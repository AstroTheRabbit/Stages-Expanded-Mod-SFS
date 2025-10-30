using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using SFS.World;
using SFS.Parts.Modules;
using System.Collections;

namespace StagesExpanded.Patches
{
    /// Reverse patch & extension for use in `RocketInfo.Create(Rocket)`.
    [HarmonyPatch(typeof(JointGroup), "RemoveJoint")]
    public static class JointGroup_RemoveJoint
    {
        [HarmonyReversePatch]
        public static void RemoveJoint(this JointGroup instance, PartJoint joint) => throw new Exception("Reverse Patch Error");
    }

    /// Reverse patch & extension for use in `RocketInfo.Create(Rocket)`.
    [HarmonyPatch(typeof(DetachModule), "GetJointsToDetach")]
    public static class DetachModule_GetJointsToDetach
    {
        // * `GetJointsToDetach()` returns a `List<DetachModule.DetachData>`, however `DetachModule.DetachData` is a private struct.
        // * Instead, we wrap the reverse patch in a second method which returns the `joint` from each `DetachData`.
        [HarmonyReversePatch]
        static object GetJointsToDetach(this DetachModule instance) => throw new Exception("Reverse Patch Error");
        public static IEnumerable<PartJoint> GetJoints(this DetachModule instance) => ((IEnumerable) GetJointsToDetach(instance))
            .Cast<object>()
            .Select(o => new Traverse(o).Field<PartJoint>("joint").Value);
    }
}