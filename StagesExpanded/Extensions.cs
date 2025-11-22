using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using SFS.World;
using SFS.Parts;
using SFS.Parts.Modules;

namespace StagesExpanded
{
    public static class Extensions
    {
        public static ref F FieldRef<F>(this object instance, string field)
        {
            return ref AccessTools.FieldRefAccess<F>(instance.GetType(), field).Invoke(instance);
        }

        public static bool TryPop<T>(this Stack<T> stack, out T item)
        {
            if (stack.Count > 0)
            {
                item = stack.Pop();
                return true;
            }
            else
            {
                item = default;
                return false;
            }
        }

        public static bool TryPeek<T>(this Queue<T> queue, out T item)
        {
            if (queue.Count > 0)
            {
                item = queue.Peek();
                return true;
            }
            else
            {
                item = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double2 ToDouble2(this Vector3 v)
        {
            return new Double2(v.x, v.y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double2 Sum(this IEnumerable<Double2> iterator)
        {
            return iterator.Aggregate((a, b) => a + b);
        }

        /// Makes a shallow copy of a `JointGroup`.
        public static JointGroup ShallowCopy(this JointGroup original)
        {
            // * It's fine to copy the `PartJoint` 'references' instead of duplicating them, since
            // * the mod doesn't directly modifiy the parts/anchors of each part joint anyway.
            return new JointGroup
            (
                original.joints.ToList(),
                original.parts.ToList()
            );
        }

        public static List<Stage> Copy(this List<Stage> original)
        {
            return original
                .Select(s => new Stage(s.stageId, s.parts.ToList()))
                .ToList();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> GetModules<T>(this IEnumerable<Part> parts)
        {
            return parts.SelectMany(p => p.GetModules<T>());
        }

        /// ? Simulation-friendly version of `DetachModule.GetJointsToDetach`, with some of the maths removed to prevent unnecessary computing.
        public static IEnumerable<PartJoint> GetJointsToDetach_Simulation(this DetachModule module, JointGroup joints)
        {
            Part part = module.transform.GetComponentInParentTree<Part>();
            Line2[] surfaces = module.separationSurface.surfaces.SelectMany((Surfaces x) => x.GetSurfacesWorld()).ToArray();
            foreach (PartJoint connectedJoint in joints.GetConnectedJoints(part))
            {
                Line2[] otherSurfaces = connectedJoint.GetOtherPart(part).GetAttachmentSurfacesWorld();
                foreach (Line2 a in surfaces)
                {
                    foreach (Line2 b in otherSurfaces)
                    {
                        if (SurfaceUtility.SurfacesConnect(a, b, out _, out _))
                        {
                            yield return connectedJoint;
                            goto SurfacesConnect;
                        }
                    }
                }
                SurfacesConnect: continue;
            }
        }

        /// ? Simulation-friendly version of `FlowModule.Flow.GetSources`.
        public static ResourceModule[] GetSources_Simulation(this FlowModule.Flow flow, JointGroup joints, Part part)
        {
            if (flow.sourceSearchMode == FlowModule.SourceMode.Global)
            {
                // * `FlowModule.Flow.GetGlobally` uses the rocket's `Resources.globalGroups`,
                // * however the code below has the same effect in the simulation.
                return joints.parts
                    .GetModules<ResourceModule>()
                    .Where(rm => flow.resourceType == rm.resourceType)
                    .ToArray();
            }
            if (flow.sourceSearchMode == FlowModule.SourceMode.Surfaces)
            {
                List<ResourceModule> result = new List<ResourceModule>();
                HashSet<Part> checkedParts = new HashSet<Part>();
                Stack<Part> connectedParts = new Stack<Part>
                (
                    joints
                        .GetConnectedJoints(part)
                        .Select(pj => pj.GetOtherPart(part))
                        .Where(p => SurfaceUtility.SurfacesConnect(p, flow.surface, out _, out _))
                );
                while (connectedParts.TryPop(out part))
                {
                    if (checkedParts.Contains(part))
                        continue;
                    checkedParts.Add(part);
                    
                    bool foundResource = false;
                    foreach (ResourceModule rm in part.GetModules<ResourceModule>())
                    {
                        if (rm.resourceType == flow.resourceType)
                        {
                            result.Add(rm);
                            foundResource = true;
                        }
                    }
                    if (foundResource)
                    {
                        foreach (PartJoint joint in joints.GetConnectedJoints(part))
                        {
                            connectedParts.Push(joint.GetOtherPart(part));
                        }
                    }
                }
                return result.ToArray();
            }
            if (flow.sourceSearchMode == FlowModule.SourceMode.Local)
            {
                return new Traverse(flow)
                    .Method("GetLocally")
                    .GetValue<ResourceModule[]>(part);
            }
            throw new Exception("Stages Expanded - Invalid `Flow.sourceSearchMode`!");
        }
    }
}