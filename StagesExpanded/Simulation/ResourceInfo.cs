using System;
using System.Linq;
using System.Collections.Generic;
using SFS.Parts.Modules;
using UnityEngine;

namespace StagesExpanded
{
    public class ResourceInfo
    {
        /// Set of engines using this `ResourceInfo`.
        public HashSet<EngineInfo> Engines { get; private set; }
        public double WetMass { get; private set; }
        public double MassFlow => Engines.Where(ei => ei.EngineOn).Sum(ei => ei.MassFlowPerResource(this));
        public double BurnTime => WetMass / MassFlow;
        public bool Depleted => double.IsNaN(BurnTime) || BurnTime < 0.001;

        public ResourceInfo(ResourceModule rm)
        {
            Engines = new HashSet<EngineInfo>();
            WetMass = rm.ResourceAmount * rm.resourceType.resourceMass;
        }

        public void Step(double burnTime, ModuleMapping mapping)
        {
            WetMass -= MassFlow * burnTime;
            // TODO: This might be incorrect/buggy for modded resource types without mass e.g. electricity.
            if (Depleted)
            {
                WetMass = 0;
                mapping.DepletedResources.Add(this);
                foreach (EngineInfo ei in Engines)
                {
                    ei.Resources.Remove(this);
                }
                Engines.Clear();
            }
        }
    }
}