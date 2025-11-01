using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SFS.World;
using SFS.Parts;
using SFS.Parts.Modules;
using StagesExpanded.Patches;

namespace StagesExpanded
{
    /// Contains info about how the rocket changes (resources, engines, etc) when a stage is triggered.
    public class StageInfo
    {
        public List<ResourceInfo> RemovedResources { get; private set; }
        public List<EngineInfo> RemovedEngines { get; private set; }
        public List<EngineInfo> ToggledEngines { get; private set; }
        public double RemovedDryMass { get; private set; }
        public double RemovedMass => RemovedDryMass + RemovedResources.Sum(ri => ri.WetMass);

        /// A "cosmetic" stage is one which at most only removes mass (e.g. fairing separation or solar panel deployment).
        /// These should only be applied once the current phase is fully depleted, so that the calculator doesn't accidentally
        /// over-report the amount of ∆V available in case the player doesn't activate a cosmetic stage immediately.
        public bool IsCosmetic => RemovedResources.Count == 0 && RemovedEngines.Count == 0 && ToggledEngines.Count == 0;

        public static StageInfo Generate(Stage stage, ref JointGroup joints, ModuleMapping mapping)
        {
            JointGroup jointGroup = joints;
            Dictionary<Part, List<Part>> splitModuleJoints = new Dictionary<Part, List<Part>>();
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
                    var list = jointGroup.GetConnectedFairings(sm.FieldRef<Part>("part"), sm);
                    foreach (SplitModule fairing in list)
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
                // TODO: Properly detecting whether or not the fragments of a `SplitModule` remain attached to the rocket
                // TODO: without creating new parts and/or modifying the `Rocket` seems to be incredibly difficult.
                // TODO: Currently I am just assuming that most players will use parts "as they are intended to be used",
                // TODO: and that every non-fairing `SplitModule` has a fragment that stays connected to its adjoined parts.
                Part part = sm.FieldRef<Part>("part");
                foreach (PartJoint joint in jointGroup.dictionary[part].ToArray())
                {
                    if (!sm.fairing)
                    {
                        Part other = joint.GetOtherPart(part);
                        if (splitModuleJoints.TryGetValue(other, out List<Part> parts))
                            parts.Add(part);
                        else
                            splitModuleJoints.Add(other, new List<Part>() { part });
                    }
                    jointGroup.RemoveJoint(joint);
                }
            }

            // * Recreate the joint group; calculate removed mass.
            jointGroup.RecreateGroups(out List<JointGroup> newGroups);
            newGroups.Sort(SortJointGroups);
            joints = newGroups[0];
            HashSet<Part> removedParts = newGroups.Skip(1).SelectMany(jg => jg.parts).ToHashSet();

            double splitMass = 0;
            HashSet<Part> connectedSplitParts = new HashSet<Part>();
            foreach (KeyValuePair<Part, List<Part>> kvp in splitModuleJoints)
            {
                if (!removedParts.Contains(kvp.Key))
                {
                    foreach (Part splitPart in kvp.Value)
                    {
                        if (!connectedSplitParts.Contains(splitPart))
                        {
                            splitMass += splitPart.mass.Value;
                            connectedSplitParts.Add(splitPart);
                        }
                    }
                }
            }

            List<EngineInfo> removedEngines = removedParts
                .GetModules<EngineModule>()
                .Select(mapping.GetOrAddEngine)
                .ToList();
            List<EngineInfo> toggledEngines = stage.parts
                .Where(p => !removedParts.Contains(p))
                .GetModules<EngineModule>()
                .Select(mapping.GetOrAddEngine)
                .ToList();
            List<ResourceInfo> removedResources = removedParts
                .GetModules<ResourceModule>()
                .Select(mapping.GetOrAddResource)
                .ToList();
            double removedDryMass = removedParts.Sum(p => p.mass.Value) - splitMass - removedResources.Sum(ri => ri.WetMass);
            return new StageInfo()
            {
                RemovedResources = removedResources,
                RemovedEngines = removedEngines,
                ToggledEngines = toggledEngines,
                RemovedDryMass = removedDryMass,
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