using System.Linq;
using System.Collections.Generic;
using SFS.World;
using SFS.Parts;
using SFS.Parts.Modules;
using StagesExpanded.Patches;

namespace StagesExpanded
{
    /// Contains info about how the rocket changes (resources, engines, etc) when a stage is triggered.
    public class StageInfo
    {
        public double RemovedMass { get; private set; }
        public List<ResourceInfo> RemovedResources { get; private set; }
        public List<EngineInfo> RemovedEngines { get; private set; }
        public List<EngineInfo> ToggledEngines { get; private set; }

        public static StageInfo Generate(Stage stage, ref JointGroup joints, ModuleMapping mapping)
        {
            JointGroup jointGroup = joints;
            double splitMass = 0;
            // * "Activate" detach & split modules.
            foreach (DetachModule dm in stage.parts.GetModules<DetachModule>())
            {
                // ? `DetachModule.Detach()`
                foreach (PartJoint j in dm.GetJoints())
                {
                    jointGroup.RemoveJoint(j);
                }
            }
            foreach (SplitModule sm in stage.parts.GetModules<SplitModule>())
            {
                // ? `SplitModule.Split()`
                if (sm.fairing)
                {
                    foreach (SplitModule fairing in jointGroup.GetConnectedFairings(sm.FieldRef<Part>("part"), sm))
                    {
                        Deploy(fairing);
                    }
                }
                else
                {
                    Deploy(sm);
                }

            }
            void Deploy(SplitModule sm)
            {
                // ? `SplitModule.Deploy()`
                Part part = sm.FieldRef<Part>("part");
                jointGroup.RemovePartAndItsJoints(part);
                // TODO: idk how to easily re-add the joints of the split module's fragments without directly creating parts.
                // TODO: However for most 'normal' rockets this shouldn't mess with the calculations *too* much.
                splitMass += part.mass.Value;
            }

            // * Recreate the joint group, calculate removed mass.
            jointGroup.RecreateGroups(out List<JointGroup> newGroups);
            newGroups.Sort(SortJointGroups);
            joints = newGroups[0];
            HashSet<Part> removedParts = newGroups.Skip(1).SelectMany(jg => jg.parts).ToHashSet();

            double removedMass = removedParts.Sum(p => p.mass.Value) - splitMass;
            List<EngineInfo> removedEngines = removedParts
                .GetModules<EngineModule>()
                .Select(mapping.GetOrAddEngine)
                .ToList();
            List<EngineInfo> activatedEngines = stage.parts
                .Where(p => !removedParts.Contains(p))
                .GetModules<EngineModule>()
                .Select(mapping.GetOrAddEngine)
                .ToList();
            List<ResourceInfo> removedResources = removedParts
                .GetModules<ResourceModule>()
                .Select(mapping.GetOrAddResource)
                .ToList();
            return new StageInfo()
            {
                RemovedMass = removedMass,
                RemovedResources = removedResources,
                RemovedEngines = removedEngines,
                ToggledEngines = activatedEngines,
            };
        }

        private static int SortJointGroups(JointGroup a, JointGroup b)
        {
            // ? Pseudo-recreation of `Rocket.SetPlayerToBestControllable()`.
            bool a_hasControl = a.parts.GetModules<ControlModule>().Any(cm => cm.hasControl.Value);
            bool b_hasControl = b.parts.GetModules<ControlModule>().Any(cm => cm.hasControl.Value);
            if (a_hasControl && b_hasControl)
            {
                Dictionary<ResourceType, (double amount, double capacity)> resources = new Dictionary<ResourceType, (double, double)>();
                double a_score = GetScore(a);
                double b_score = GetScore(b);
                return b_score > a_score ? 1 : -1;

                double GetScore(JointGroup group)
                {
                    resources.Clear();
                    foreach (ResourceModule rm in group.parts.GetModules<ResourceModule>())
                    {
                        if (resources.TryGetValue(rm.resourceType, out var tuple))
                            resources[rm.resourceType] = (tuple.amount + rm.ResourceAmount, tuple.capacity + rm.TotalResourceCapacity);
                        else
                            resources.Add(rm.resourceType, (rm.ResourceAmount, rm.TotalResourceCapacity));
                    }
                    double score = 0;
                    foreach ((double amount, double capacity) in resources.Values)
                    {
                        score += amount / capacity / resources.Count;
                    }
                    return score;
                }
            }
            return (b_hasControl ? 1 : 0) - (a_hasControl ? 1 : 0);
        }
    }
}