using System.Collections.Generic;
using SFS.World;
using SFS.Parts.Modules;
using System.Linq;

namespace StagesExpanded.Simulation
{
    /// Maps vanilla part modules (resources, engines, etc) to their simulation equivalents.
    public class ModuleMapping
    {
        private readonly Dictionary<ResourceModule, ResourceInfo> resources = new Dictionary<ResourceModule, ResourceInfo>();
        private readonly Dictionary<EngineModule, EngineInfo> engines = new Dictionary<EngineModule, EngineInfo>();
        private readonly Dictionary<BoosterModule, EngineInfo> boosters = new Dictionary<BoosterModule, EngineInfo>();

        public ModuleMapping(SimulationInput input, out JointGroup joints)
        {
            Throttle = input.GetThrottle();
            joints = input.GetJointGroup();

            foreach (ResourceModule rm in joints.parts.GetModules<ResourceModule>())
            {
                GetOrAddResource(rm);
            }
        }

        public double Throttle { get; }
        public IEnumerable<ResourceInfo> Resources => resources.Values;
        public IEnumerable<EngineInfo> Engines => Enumerable.Concat(engines.Values, boosters.Values);

        /// Adds a resource to the mapping, or returns the current one if it already exists.
        public ResourceInfo GetOrAddResource(ResourceModule rm)
        {
            if (!resources.TryGetValue(rm, out ResourceInfo ri))
            {
                ri = new ResourceInfo(rm);
                resources.Add(rm, ri);
            }
            return ri;
        }

        /// Adds an engine to the mapping, or returns the current one if it already exists.
        public EngineInfo GetOrAddEngine(EngineModule em)
        {
            if (!engines.TryGetValue(em, out EngineInfo ei))
            {
                ei = new EngineInfo(em, this);
                engines.Add(em, ei);
            }
            return ei;
        }

        /// Adds a booster to the mapping, or returns the current one if it already exists.
        public EngineInfo GetOrAddBooster(BoosterModule bm)
        {
            if (!boosters.TryGetValue(bm, out EngineInfo ei))
            {
                ResourceInfo ri = new ResourceInfo(bm);
                ei = new EngineInfo(bm, ri);
                boosters.Add(bm, ei);
            }
            return ei;
        }

        public bool TryGetResource(ResourceModule rm, out ResourceInfo ri)
        {
            return resources.TryGetValue(rm, out ri);
        }
    }
}