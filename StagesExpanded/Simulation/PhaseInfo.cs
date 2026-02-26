using System;
using System.Linq;
using System.Collections.Generic;

namespace StagesExpanded.Simulation
{
    /// A "phase" is a segment of a stage during which thrust, specific impulse, and mass flow remain constant.
    // ? https://space.stackexchange.com/a/25167
    public class PhaseInfo
    {
        private HashSet<EngineInfo> Engines { get; set; }
        private HashSet<ResourceInfo> Resources { get; set; }
        public double TotalMass { get; private set; }
        public double FinalMass => TotalMass - MassFlow * BurnTime;

        /// The total thrust caused by this phase's engines.
        public Double2 Thrust => Engines.Select(ei => ei.Thrust).Sum();
        /// The total mass flow caused by this phase's engines.
        public double MassFlow => Resources.Select(ri => ri.MassFlow).Sum();
        /// The *effective* specific impulse of this phase's engines.
        // ? https://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines
        public double Isp => Thrust.magnitude / MassFlow;
        /// The minimum burn time before a resource used by this phase is depleted.
        public double BurnTime => Depleted ? 0 : Resources.Min(ri => ri.BurnTime);
        /// The available ∆V of this phase.
        // ? https://en.wikipedia.org/wiki/Tsiolkovsky_rocket_equation
        public double DeltaV => 9.8 * Isp * Math.Log(TotalMass / FinalMass);
        /// Returns `true` if all the resources used by this phase have been depleted.
        public bool Depleted => Resources.All(ri => ri.Depleted);

        /// Generates a new `PhaseInfo` from the engines which are currently enabled.
        public static PhaseInfo Generate(double totalMass, ModuleMapping mapping)
        {
            foreach (EngineInfo ei in mapping.Engines)
            {
                ei.UpdateEngine();
            }
            HashSet<EngineInfo> engines = mapping.Engines.Where(ei => ei.Status.Running()).ToHashSet();
            HashSet<ResourceInfo> resources = engines.SelectMany(ei => ei.Resources).ToHashSet();
            foreach (ResourceInfo ri in resources)
            {
                ri.UpdateMassFlow();
            }
            return new PhaseInfo
            {
                Engines = engines,
                Resources = resources,
                TotalMass = totalMass,
            };
        }

        public PhaseInfo Step(ModuleMapping mapping, out PhaseResult result)
        {
            result = PhaseResult.FromPhaseInfo(this);
            foreach (ResourceInfo ri in Resources)
            {
                ri.Step(result.BurnTime);
            }
            return Generate(result.FinalMass, mapping);
        }

        public bool ShouldApplyStage(StageInfo stage)
        {
            // * If the phase is not using any of the resources that are removed by this stage, and there are no running engines
            // * that are removed by this stage, then it is safe to assume that the player will activate this stage.

            if (stage.IsCosmetic && !Depleted)
                return false;
            else if (stage.RemovedResources.Any(ri => Resources.Contains(ri)))
                return false;
            else if (stage.RemovedEngines.Any(ei => Engines.Contains(ei)))
                return false;
            else
                return true;
        }

        public PhaseInfo ApplyStage(StageInfo stage, ModuleMapping mapping, out PhaseResult result)
        {
            double initialMass = TotalMass;
            double finalMass = TotalMass - stage.RemovedMass;
            result = PhaseResult.EmptyResult(initialMass, finalMass);

            foreach (ResourceInfo ri in stage.RemovedResources)
            {
                ri.Engines.Clear();
            }
            foreach (EngineInfo ei in stage.ToggledEngines)
            {
                ei.Status = ei.Status.Toggle();
            }
            foreach (EngineInfo ei in stage.RemovedEngines)
            {
                ei.Status = ei.Status.Shutdown();
            }
            return Generate(finalMass, mapping);
        }
    }
}