using System.Collections.Generic;

namespace StagesExpanded
{
    /// A "phase" is a segment of a stage during which thrust, specific impulse, and mass flow remain constant.
    // ? https://space.stackexchange.com/a/25167
    public class PhaseInfo
    {
        public List<EngineGroup> engineGroups;
    }
}