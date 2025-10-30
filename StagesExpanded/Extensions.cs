using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using HarmonyLib;
using SFS.Parts;
using SFS.World;

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
            List<PartJoint> joints = new List<PartJoint>(original.joints);
            List<Part> parts = new List<Part>(original.parts);
            return new JointGroup(joints, parts);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> GetModules<T>(this IEnumerable<Part> parts)
        {
            return parts.SelectMany(p => p.GetModules<T>());
        }
    }
}