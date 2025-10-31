using HarmonyLib;
using UnityEngine;
using ModLoader;
using ModLoader.Helpers;
using UITools;
using SFS.IO;
using System.Collections.Generic;
using ModLoader.IO;
using SFS.World;
using SFS.UI.ModGUI;

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
            // ! TODO: Testing purposes only!
            Console.commands.Add
            (
                input =>
                {
                    if (input != "calc")
                        return false;

                    Rocket rocket = PlayerController.main.player.Value as Rocket;
                    RocketInfo info = RocketInfo.Generate(rocket);

                    return true;
                }
            );
            SceneHelper.OnWorldSceneLoaded += TestUpdater.Create;
        }

        class TestUpdater : MonoBehaviour
        {
            Label label_deltaV;
            Label label_isp;
            Label label_burnTime;
            Label label_initialMass;
            Label label_finalMass;
            Label label_thrust;

            int counter = 0;

            public static void Create()
            {
                int id = Builder.GetRandomID();
                GameObject holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "test");
                Window window = Builder.CreateWindow(holder.transform, id, 500, 500, draggable: true, savePosition: false, titleText: "∆V Test");
                window.CreateLayoutGroup(Type.Vertical, TextAnchor.MiddleLeft);
                holder.AddComponent<TestUpdater>().Init(window);
            }

            void Init(Window window)
            {
                label_deltaV = Builder.CreateLabel(window, 450, 40);
                label_isp = Builder.CreateLabel(window, 450, 40);
                label_burnTime = Builder.CreateLabel(window, 450, 40);
                label_initialMass = Builder.CreateLabel(window, 450, 40);
                label_finalMass = Builder.CreateLabel(window, 450, 40);
                label_thrust = Builder.CreateLabel(window, 450, 40);
            }

            void Update()
            {
                if (counter++ > 30 && PlayerController.main.player.Value is Rocket rocket && RocketInfo.Generate(rocket) is RocketInfo ri)
                {
                    counter = 0;
                    label_deltaV.Text = "∆V: " + ri.CurrentStageResult.DeltaV.ToVelocityString();
                    label_isp.Text = "Isp: " + ri.CurrentStageResult.Isp + "s";
                    label_burnTime.Text = "Burn Time: " + ri.CurrentStageResult.BurnTime + "s";
                    label_initialMass.Text = "Initial Mass: " + ri.CurrentStageResult.IntialMass.ToString();
                    label_finalMass.Text = "Final Mass: " + ri.CurrentStageResult.FinalMass.ToString();
                    label_thrust.Text = "Thrust: " + ri.CurrentStageResult.Thrust.ToString();
                }
            }
        }
    }
}
