using System.Collections.Generic;
using SFS.Parts.Modules;

namespace StagesExpanded.Simulation
{
    /// Maps vanilla part modules (resources, engines, etc) to their simulation equivalents.
    public class ModuleMapping
    {
        private readonly Dictionary<ResourceModule, ResourceInfo> resources = new Dictionary<ResourceModule, ResourceInfo>();
        private readonly Dictionary<EngineModule, EngineInfo> engines = new Dictionary<EngineModule, EngineInfo>();
        // TODO: `BoosterModule` support.

        public IEnumerable<ResourceInfo> Resources => resources.Values;
        public IEnumerable<EngineInfo> Engines => engines.Values;

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

        public bool TryGetResource(ResourceModule rm, out ResourceInfo ri)
        {
            return resources.TryGetValue(rm, out ri);
        }
    }
}