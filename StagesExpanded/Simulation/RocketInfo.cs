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
        /// Maps a `Stage` to its respective stage `PhaseResult`.
        public Dictionary<Stage, PhaseResult> StageResults { get; }

        public static RocketInfo Generate(Rocket rocket)
        {
            // * Set-up the module mapping (engines are added by the `StageInfo` constructor when needed).
            // TODO: The resource map *could* be 'sparser', however it's probably quicker to add all resources
            // TODO: to the map in case the player is using a global-flow engine like the ion engine.
            ModuleMapping mapping = new ModuleMapping();
            foreach (ResourceModule rm in rocket.partHolder.GetModules<ResourceModule>())
            {
                mapping.GetOrAddResource(rm);
            }

            // * Generate staging info.
            Dictionary<Stage, StageInfo> stages = new Dictionary<Stage, StageInfo>();
            JointGroup joints = rocket.jointsGroup.ShallowCopy();
            foreach (Stage stage in rocket.staging.stages)
            {
                stages.Add(stage, StageInfo.Generate(stage, ref joints, mapping));
            }
            
            // * Initialize the simulation with the currently enabled engines.
            rocket.partHolder.parts
                .GetModules<EngineModule>()
                .Where(em => em.engineOn.Value)
                .Select(mapping.GetOrAddEngine)
                .ForEach(ei => ei.EngineOn = true);

            if (mapping.Engines.Count() == 0)
            {
                // * The rocket has no active or staged engines, and so calculations cannot be performed.
                return null;
            }

            // TODO: Skip calculating the 'current' stage stats if there are no currently enabled engines. 

            PhaseInfo phase = PhaseInfo.Generate(rocket.mass.GetMass(), mapping);
            List<PhaseResult> results = new List<PhaseResult>();

            // ! MAIN LOOP
            int i = 0;
            do
            {
                phase = phase.Step(mapping, out PhaseResult result);
                results.Add(result);

                Debug.Log($"Phase index: {i++}");
                Debug.Log($"  ∆V: {result.DeltaV} m/s");
                Debug.Log($"  Isp: {result.Isp} s");
                Debug.Log($"  Burn time: {result.BurnTime} s");
                Debug.Log($"  Initial mass: {result.IntialMass} kg");
                Debug.Log($"  Final mass: {result.FinalMass} kg");
                Debug.Log($"  Thrust: {result.Thrust}");
            } while (phase.MassFlow > 0.001);

            PhaseResult totalResult = PhaseResult.Merge(results, phase);
            Debug.Log("Total phase results");
            Debug.Log($"  ∆V: {totalResult.DeltaV} m/s");
            Debug.Log($"  Isp: {totalResult.Isp} s");
            Debug.Log($"  Burn time: {totalResult.BurnTime} s");
            Debug.Log($"  Initial mass: {totalResult.IntialMass} kg");
            Debug.Log($"  Final mass: {totalResult.FinalMass} kg");
            Debug.Log($"  Thrust: {totalResult.Thrust}");

            return null;
        }

        // TODO: Ensure first stage engines have their thrust altered by current throttle?
        // private static double GetThrottle(Rocket rocket)
        // {
        //     if (rocket.throttle.throttleOn && rocket.throttle.throttlePercent > 0)
        //         return rocket.throttle.throttlePercent.Value;
        //     else
        //         return 1;
        // }
    }
}