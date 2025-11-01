using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SFS.World;
using SFS.Parts.Modules;
using System;

namespace StagesExpanded
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
            // * Set-up the module mapping (engines are added by the `StageInfo` constructor when needed).
            ModuleMapping mapping = new ModuleMapping();
            foreach (ResourceModule rm in rocket.partHolder.GetModules<ResourceModule>())
            {
                mapping.GetOrAddResource(rm);
            }

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
                .ForEach(ei => ei.UpdateEngineOn());

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
                    previousStage = stages.Dequeue(); // `stages.Dequeue() == currentStage`
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

        // TODO: Ensure first stage engines have their thrust altered by current throttle.
        // private static double GetThrottle(Rocket rocket)
        // {
        //     if (rocket.throttle.throttleOn && rocket.throttle.throttlePercent > 0)
        //         return rocket.throttle.throttlePercent.Value;
        //     else
        //         return 1;
        // }
    }
}