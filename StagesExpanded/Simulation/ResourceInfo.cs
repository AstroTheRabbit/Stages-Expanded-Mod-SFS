using System.Linq;
using System.Collections.Generic;
using SFS.Parts.Modules;

namespace StagesExpanded.Simulation
{
    public class ResourceInfo
    {
        /// Set of engines using this `ResourceInfo`.
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
                WetMass = 0;
                // MassFlow = 0;
                foreach (EngineInfo ei in Engines)
                {
                    ei.Resources.Remove(this);
                }
                Engines.Clear();
            }
        }

        public void UpdateMassFlow()
        {
            MassFlow = Engines.Where(ei => ei.EngineOn).Sum(ei => ei.MassFlowPerResource(this));
        }
    }
}