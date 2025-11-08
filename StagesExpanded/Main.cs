using System.Collections.Generic;
using HarmonyLib;
using UITools;
using SFS.UI;
using SFS.IO;
using ModLoader;
using StagesExpanded.UI;
using StagesExpanded.Simulation;
using SFS.World;
using UnityEngine;

namespace StagesExpanded
{
    public class Main : Mod// !, IUpdatable
    {
        public static Main main;
        public override string ModNameID => "stagesexpanded";
        public override string DisplayName => "Stages Expanded";
        public override string Author => "Astro The Rabbit";
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string ModVersion => "1.0";
        public override string Description => "Displays ∆V, burn time, and other stats for your rockets' stages.";

        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.5" } };
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>() { { "https://github.com/AstroTheRabbit/Stages-Expanded-Mod-SFS/releases/latest/download/StagesExpanded.dll", new FolderPath(ModFolder).ExtendToFile("StagesExpanded.dll") } };

        public override void Early_Load()
        {
            new Harmony(ModNameID).PatchAll();
            main = this;
        }

        public override void Load()
        {
            const string ID_DELTA_V_CALCULATOR = "DELTA_V_CALCULATOR";
            string Text_Menu()
            {
                return "Stages Expanded completely replaces the functionality of Altaïr's ΔV calculator.\n"
                     + "Stages Expanded will now disable ∆V calculator and re-launch the game.\n"
                     + "You can also fully un-install ∆V calculator if you want.";
            }
            string Text_Continue()
            {
                return "Continue";
            }

            if (ModsSettings.main.settings.modsActive.TryGetValue(ID_DELTA_V_CALCULATOR, out bool active) && active)
            {
                ButtonBuilder button = ButtonBuilder.CreateButton(null, Text_Continue, Relaunch, SFS.Input.CloseMode.None);
                MenuGenerator.ShowChoices(Text_Menu, button);
            }
            else
            {
                Settings.Init(ModFolder);
                StatsUI.Init();
                WindowUI.Init();
                SimulationManager.Init();

                // ! TESTING ONLY
                ModLoader.IO.Console.commands.Add
                (
                    (string input) =>
                    {
                        if (input != "calc")
                            return false;
                        
                        if (PlayerController.main.player.Value is Rocket rocket)
                        {
                            RocketInfo info = RocketInfo.Generate(rocket);
                            info.CurrentStageResult.DebugPrint("Current Stage");
                            foreach (Stage stage in rocket.staging.stages)
                            {
                                info.StageResults[stage].DebugPrint($"Stage {stage.stageId}");
                            }
                        }

                        return true;
                    }
                );
            }

            void Relaunch()
            {
                ModsSettings.main.settings.modsActive[ID_DELTA_V_CALCULATOR] = false;
                ModsSettings.main.SaveAll();
                ApplicationUtility.Relaunch();
            }
        }
    }
}
