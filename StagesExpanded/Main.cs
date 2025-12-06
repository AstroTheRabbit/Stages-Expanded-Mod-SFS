using System.Collections.Generic;
using HarmonyLib;
using UITools;
using SFS.UI;
using SFS.IO;
using ModLoader;
using StagesExpanded.UI;
using StagesExpanded.Simulation;

namespace StagesExpanded
{
    public class Main : Mod, IUpdatable
    {
        public static Main main;
        public override string ModNameID => "stagesexpanded";
        public override string DisplayName => "Stages Expanded";
        public override string Author => "Astro The Rabbit";
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string ModVersion => "1.2";
        public override string Description => "Displays ∆V, burn time, and other stats for your rockets' stages.";

        public static bool DeltaVCalculatorActive { get; private set; }
        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.5" } };
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>()
        {
            {
                "https://github.com/AstroTheRabbit/Stages-Expanded-Mod-SFS/releases/latest/download/StagesExpanded.dll",
                new FolderPath(ModFolder).ExtendToFile("StagesExpanded.dll")
            }
        };

        public override void Early_Load()
        {
            new Harmony(ModNameID).PatchAll();
            main = this;
        }

        public override void Load()
        {
            DeltaVCalculatorActive = ModsSettings.main.settings.modsActive.TryGetValue("DELTA_V_CALCULATOR", out bool active) && active;
            Settings.Init(ModFolder);
            StatsUI.Init();
            WindowUI.Init();
            SimulationManager.Init();
        }
    }
}
