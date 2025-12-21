using System.Linq;
using System.Collections.Generic;
using SFS.World;
using SFS.Parts.Modules;

namespace StagesExpanded.Simulation
{
    public class SimulationOutput
    {
        /// The staging used to generate this output.
        public List<Stage> Stages { get; }
        /// The combined result of every stage.
        public PhaseResult TotalResults { get; }
        /// The calculation results of the current rocket state.
        public PhaseResult CurrentStageResult { get; }
        /// Maps a `Stage` to its respective stage `PhaseResult`.
        public Dictionary<Stage, PhaseResult> StageResults { get; }

        private SimulationOutput(List<Stage> stages, PhaseResult currentStageResult, Dictionary<Stage, PhaseResult> stageResults)
        {
            IEnumerable<PhaseResult> Enumerator()
            {
                yield return currentStageResult;
                foreach (PhaseResult pr in stageResults.Values)
                {
                    yield return pr;
                }
            }
            Stages = stages;
            TotalResults = PhaseResult.Merge(Enumerator());
            CurrentStageResult = currentStageResult;
            StageResults = stageResults;
        }

        public static SimulationOutput Generate(SimulationInput input)
        {
            ModuleMapping mapping = new ModuleMapping(input, out JointGroup joints);
            double mass = joints.parts.Sum(p => p.mass.Value);
            List<Stage> stages = input.GetStages(joints);

            // * Initialize the simulation with the currently enabled engines & boosters.
            joints.parts
                .GetModules<EngineModule>()
                .Where(em => em.engineOn.Value)
                .Select(mapping.GetOrAddEngine)
                .ForEach(ei => ei.UpdateEngine());
            joints.parts
                .GetModules<BoosterModule>()
                .Where(bm => bm.enabled)
                .Select(mapping.GetOrAddBooster)
                .ForEach(ei => ei.UpdateEngine());

            // * Generate staging info.
            Dictionary<Stage, StageInfo> stageMap = new Dictionary<Stage, StageInfo>();
            foreach (Stage stage in stages)
            {
                stageMap.Add(stage, StageInfo.Generate(stage, ref joints, mapping));
            }

            Queue<Stage> stageQueue = new Queue<Stage>(stages);
            Stage previousStage = null;

            PhaseInfo phase = PhaseInfo.Generate(mass, mapping);
            PhaseResult emptyResult = PhaseResult.EmptyResult(phase.TotalMass, phase.TotalMass);
            List<PhaseResult> phaseResults = new List<PhaseResult>();

            PhaseResult currentStageResult = PhaseResult.EmptyResult(phase.TotalMass, phase.TotalMass);
            Dictionary<Stage, PhaseResult> stageResults = new Dictionary<Stage, PhaseResult>(stageMap.Count);

            void AddResults()
            {
                PhaseResult result = PhaseResult.Merge(phaseResults) ?? emptyResult;                
                phaseResults.Clear();
                if (previousStage == null)
                    currentStageResult = result;
                else
                    stageResults.Add(previousStage, result);
            }

            while (stageQueue.TryPeek(out Stage currentStage))
            {
                StageInfo currentInfo = stageMap[currentStage];
                if (phase.ShouldApplyStage(currentInfo))
                {
                    AddResults();
                    // * `stages.Dequeue() == currentStage`
                    previousStage = stageQueue.Dequeue();
                    phase = phase.ApplyStage(currentInfo, mapping, out emptyResult);
                }
                else
                {
                    phase = phase.Step(mapping, out PhaseResult result);
                    phaseResults.Add(result);
                }
            }
            while (!phase.Depleted)
            {
                phase = phase.Step(mapping, out PhaseResult result);
                phaseResults.Add(result);
            }
            AddResults();

            return new SimulationOutput(stages, currentStageResult, stageResults);
        }

        public IEnumerable<(int id, PhaseResult result)> AllResults()
        {
            yield return (0, CurrentStageResult);
            foreach (Stage stage in Stages)
            {
                yield return (stage.stageId, StageResults[stage]);
            }
        }
    }
}