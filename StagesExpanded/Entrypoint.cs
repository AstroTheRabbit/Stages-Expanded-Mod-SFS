using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UITools;
using SFS.IO;
using ModLoader;
using StagesExpanded.UI;
using StagesExpanded.Simulation;

namespace StagesExpanded
{
    [UsedImplicitly]
    public class Entrypoint : Mod, IUpdatable
    {
        public static Entrypoint Main { get; private set; }
        public override string ModNameID => "stagesexpanded";
        public override string DisplayName => "Stages Expanded";
        public override string Author => "Astro The Rabbit";
        public override string MinimumGameVersionNecessary => "1.6.0.14";
        public override string ModVersion => "1.4";
        public override string Description => "Displays ∆V, burn time, and other stats for your rockets' stages.";

        public static bool DeltaVCalculatorActive { get; private set; }
        
        public override Dictionary<string, string> Dependencies => new Dictionary<string, string>
        {
            { "UITools", "1.1.5" },
        };
        
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>
        {
            {
                "https://github.com/AstroTheRabbit/Stages-Expanded-Mod-SFS/releases/latest/download/StagesExpanded.dll",
                new FolderPath(ModFolder).ExtendToFile("StagesExpanded.dll")
            },
        };

        public override void Early_Load()
        {
            new Harmony(ModNameID).PatchAll();
            Main = this;
        }

        public override void Load()
        {
            DeltaVCalculatorActive = Loader.main.GetLoadedMods().Any(m => m.ModNameID == "DELTA_V_CALCULATOR");
            Settings.Init();
            StatsUI.Init();
            WindowUI.Init();
            SimulationManager.Init();
        }
    }
}
