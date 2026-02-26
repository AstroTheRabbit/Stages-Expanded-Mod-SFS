using System;
using HarmonyLib;
using SFS.World;
using SFS.Builds;
using StagesExpanded.Simulation;
using SFS.Parts.Modules;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace

namespace StagesExpanded.Patches
{
    /// Reverse patch & extension for use in `RocketInfo.Create(Rocket)`.
    [HarmonyPatch(typeof(JointGroup), "RemoveJoint")]
    public static class JointGroup_RemoveJoint
    {
        [HarmonyReversePatch]
        public static void RemoveJoint(this JointGroup instance, PartJoint joint) => throw new Exception("Reverse Patch Error");
    }

    [HarmonyPatch(typeof(BuildState), nameof(BuildState.Clear))]
    public static class BuildState_Clear
    {
        public static void Postfix()
        {
            // * Used to determine whether the window UI should be "reset".
            BuildInput.GridCleared = true;
        }
    }

    [HarmonyPatch(typeof(RcsModule), "FixedUpdate")]
    public static class RcsModule_FixedUpdate
    {
        public static bool Prefix()
        {
            // * `RcsModule.FixedUpdate` tries to access `RcsModule.Rocket` in the build scene
            // * since Stages Expanded generates `FlowModule` flows for use in the simulation.
            return SceneManager.GetActiveScene().name != "Build_PC";
        }
    }
}