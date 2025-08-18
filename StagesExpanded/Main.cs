using HarmonyLib;
using UnityEngine;
using ModLoader;
using ModLoader.Helpers;
using UITools;
using SFS.IO;
using System.Collections.Generic;

namespace StagesExpanded
{
    public class Main : Mod
    {
        public static Main main;
        public override string ModNameID => "stagesexpanded";
        public override string DisplayName => "Stages Expanded";
        public override string Author => "Astro The Rabbit";
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string ModVersion => "1.0";
        public override string Description => "Displays ∆V, TWR, and other stats for your rockets' stages.";

        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.5" } };
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>() { { "https://github.com/AstroTheRabbit/Stages-Expanded-Mod-SFS/releases/latest/download/StagesExpanded.dll", new FolderPath(ModFolder).ExtendToFile("StagesExpanded.dll") } };

        public override void Early_Load()
        {
            new Harmony(ModNameID).PatchAll();
            main = this;
        }

        public override void Load()
        {
            Debug.Log("Hiya!");
        }
    }
}
