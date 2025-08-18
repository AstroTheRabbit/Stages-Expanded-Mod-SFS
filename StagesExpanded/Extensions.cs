using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StagesExpanded
{
    public static class Extensions
    {
        public static Vector2 Sum(this IEnumerable<Vector2> iter)
        {
            return iter.Aggregate((a, b) => a + b);
        }
    }
}