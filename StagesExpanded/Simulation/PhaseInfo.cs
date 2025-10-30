using System;
using System.Linq;
using System.Collections.Generic;
using SFS;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;

namespace StagesExpanded
{
    /// A "phase" is a segment of a stage during which thrust, specific impulse, and mass flow remain constant.
    // ? https://space.stackexchange.com/a/25167
    public class PhaseInfo
    {
        /// Generates a new `PhaseInfo` from the engines which are currently enabled.
        public static PhaseInfo Generate(double totalMass, ModuleMapping mapping)
        {
            HashSet<EngineInfo> engines = mapping.Engines.Where(ei => ei.EngineOn).ToHashSet();
            HashSet<ResourceInfo> resources = engines.SelectMany(ei => ei.Resources).ToHashSet();
            return new PhaseInfo()
            {
                Engines = engines,
                Resources = resources,
                TotalMass = totalMass,
            };
        }

        private HashSet<EngineInfo> Engines { get; set; }
        private HashSet<ResourceInfo> Resources { get; set; }
        public double TotalMass { get; private set; }
        public double WetMass => Resources.Select(ri => ri.WetMass).Sum();
        public double DryMass => TotalMass - WetMass;

        /// The total thrust caused by this phase's engines.
        public Double2 Thrust => Engines.Select(ei => ei.Thrust).Sum();
        /// The total mass flow caused by this phase's engines.
        public double MassFlow => Resources.Select(ri => ri.MassFlow).Sum();
        /// The *effective* specific impulse of this phase's engines.
        // ? https://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines
        public double Isp => Thrust.magnitude / MassFlow;
        /// The available ∆V of this phase.
        // ? https://en.wikipedia.org/wiki/Tsiolkovsky_rocket_equation
        public double DeltaV => 9.8 * Isp * Math.Log(TotalMass / DryMass);
        /// The minimum burn time before a resource used by this phase is depleted.
        public double BurnTime => Resources.Where(ri => !ri.Depleted).Min(ri => ri.BurnTime);

        public PhaseInfo Step(ModuleMapping mapping, out PhaseResult result)
        {
            Debug.Log($"Resource count: {Resources.Count}");
            result = PhaseResult.FromPhaseInfo(this);
            foreach (ResourceInfo ri in Resources.Where(ri => !ri.Depleted))
            {
                ri.Step(result.BurnTime, mapping);
            }
            return Generate(result.FinalMass, mapping);
        }
    }

    /// The result of `PhaseInfo` calculations.
    public class PhaseResult
    {
        public double Thrust { get; private set; } = double.NaN;
        public double Acceleration { get; private set; } = double.NaN;
        public double GForce => Acceleration / 9.8;
        public double BurnTime { get; private set; } = double.NaN;
        public double Isp { get; private set; } = double.NaN;
        public double DeltaV { get; private set; } = double.NaN;
        public double IntialMass { get; private set; } = double.NaN;
        public double FinalMass { get; private set; } = double.NaN;

        public static PhaseResult FromPhaseInfo(PhaseInfo pi)
        {
            double thrust = pi.Thrust.magnitude;
            double burnTime = pi.BurnTime;

            return new PhaseResult()
            {
                Thrust = thrust,
                Acceleration = thrust / pi.TotalMass,
                BurnTime = burnTime,
                Isp = pi.Isp,
                DeltaV = pi.DeltaV,
                IntialMass = pi.TotalMass,
                FinalMass = pi.TotalMass - (burnTime * pi.MassFlow),
            };

            // double GetSpaceCenterGravity()
            // {
            //     SpaceCenterData spaceCenter = Base.planetLoader.spaceCenter;
            //     return spaceCenter.address.GetPlanet().GetGravity(spaceCenter.LaunchPadLocation.position.magnitude);
            // }
        }

        /// Combines the results of `phases` into a single stage `PhaseResult`.
        public static PhaseResult Merge(List<PhaseResult> phases, PhaseInfo finalPhase)
        {
            if (phases.Count == 0)
                throw new Exception("PhaseResult.Merge(): `results` is empty!");
            
            PhaseResult first = phases[0];
            return new PhaseResult()
            {
                Thrust = first.Thrust,
                Acceleration = first.Acceleration,
                Isp = first.Isp,
                BurnTime = phases.Sum(pr => pr.BurnTime),
                DeltaV = phases.Sum(pr => pr.DeltaV),
                IntialMass = first.IntialMass,
                FinalMass = finalPhase.TotalMass,
            };
        }
    }
}