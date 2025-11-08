using System.Linq;
using System.Collections.Generic;
using SFS.Parts.Modules;
using ModLoader.IO;

namespace StagesExpanded.Simulation
{
    public class ResourceInfo
    {
        /// Set of engines currently using this `ResourceInfo`.
        public HashSet<EngineInfo> Engines { get; private set; }
        public double WetMass { get; private set; }
        public double MassFlow { get; private set; }
        public double BurnTime => WetMass / MassFlow;
        public bool Depleted => double.IsNaN(BurnTime) || BurnTime < 0.001;

        public ResourceInfo(ResourceModule rm)
        {
            Engines = new HashSet<EngineInfo>();
            WetMass = rm.ResourceAmount * rm.resourceType.resourceMass;
        }

        public void Step(double burnTime)
        {
            WetMass -= MassFlow * burnTime;
            if (Depleted)
            {
                // * This resource is removed from each engines' `Resources` hashset when the `EngineInfo.UpdateEngineOn` is called.
                WetMass = 0;
            }
        }

        public void UpdateMassFlow()
        {
            MassFlow = Engines.Where(ei => ei.EngineOn).Sum(ei => ei.MassFlowPerResource(this));
        }
    }
}