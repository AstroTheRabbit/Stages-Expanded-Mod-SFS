using System.Linq;
using System.Collections.Generic;
using SFS.World;
using SFS.Parts.Modules;

namespace StagesExpanded.Simulation
{
    public class RocketInfo
    {
        /// The calculation results of the current rocket state.
        public PhaseResult CurrentStageResult { get; } 
        public PhaseResult TotalResults { get; }
        /// Maps a `Stage` to its respective stage `PhaseResult`.
        public Dictionary<Stage, PhaseResult> StageResults { get; }

        private RocketInfo(PhaseResult currentStageResult, Dictionary<Stage, PhaseResult> stageResults)
        {
            IEnumerable<PhaseResult> Enumerator()
            {
                yield return currentStageResult;
                foreach (PhaseResult pr in stageResults.Values)
                {
                    yield return pr;
                }
            }
            CurrentStageResult = currentStageResult;
            StageResults = stageResults;
            TotalResults = PhaseResult.Merge(Enumerator());
        }

        public static RocketInfo Generate(Rocket rocket)
        {
            ModuleMapping mapping = new ModuleMapping(rocket);

            // * Generate staging info.
            Dictionary<Stage, StageInfo> stageMap = new Dictionary<Stage, StageInfo>();
            JointGroup joints = rocket.jointsGroup.ShallowCopy();
            foreach (Stage stage in rocket.staging.stages)
            {
                stageMap.Add(stage, StageInfo.Generate(stage, ref joints, mapping));
            }
            
            // * Initialize the simulation with the currently enabled engines.
            rocket.partHolder.parts
                .GetModules<EngineModule>()
                .Where(em => em.engineOn.Value)
                .Select(mapping.GetOrAddEngine)
                .ForEach(ei => ei.UpdateEngine());

            Queue<Stage> stages = new Queue<Stage>(rocket.staging.stages);
            Stage previousStage = null;

            PhaseInfo phase = PhaseInfo.Generate(rocket.mass.GetMass(), mapping);
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

            while (stages.TryPeek(out Stage currentStage))
            {
                StageInfo currentInfo = stageMap[currentStage];
                if (phase.ShouldApplyStage(currentInfo))
                {
                    AddResults();
                    // * `stages.Dequeue() == currentStage`
                    previousStage = stages.Dequeue();
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

            return new RocketInfo(currentStageResult, stageResults);
        }
    }
}