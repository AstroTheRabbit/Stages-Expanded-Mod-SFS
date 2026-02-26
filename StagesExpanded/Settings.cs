using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UITools;
using SFS.IO;
using SFS.UI.ModGUI;
using StagesExpanded.UI;
using LayoutType = SFS.UI.ModGUI.Type;
using static StagesExpanded.ReadoutNames;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace StagesExpanded
{
    public class Settings : ModSettings<SettingsData>
    {
        public static Settings main;
        protected override FilePath SettingsFile => new FolderPath(Entrypoint.Main.ModFolder).ExtendToFile("settings.txt");
        private static Color DefaultInputColor => new Color(0.008f, 0.090f, 0.180f, 0.941f);

        public static void Init()
        {
            main = new Settings();
            main.Initialize();
            AddUI();
        }

        private static void AddUI()
        {
            ConfigurationMenu.Add
            (
                "Stages Expanded",
                
                new (string, Func<Transform, GameObject>)[]
                {
                    ("General", CreateGeneralUI),
                    ("Readouts", CreateReadoutsUI),
                }
            );
        }

        private static GameObject CreateGeneralUI(Transform parent)
        {
            Vector2Int size = ConfigurationMenu.ContentSize;
            Box box = Builder.CreateBox(parent, size.x, size.y);
            box.CreateLayoutGroup(LayoutType.Vertical, TextAnchor.UpperLeft, padding: new RectOffset(15, 15, 15, 15));

            void CreateIntInput(string name, MemberRef<int> setting)
            {
                InputWithLabel input = Builder.CreateInputWithLabel
                (
                    box,
                    size.x - 30,
                    40,
                    labelText: name,
                    inputText: setting.Get().ToString()
                );
                input.textInput.OnChange += value =>
                {
                    if (int.TryParse(value, out int result) && result > 0)
                    {
                        input.textInput.FieldColor = DefaultInputColor;
                        setting.Set(result);
                    }
                    else
                    {
                        input.textInput.FieldColor = Color.red;
                    }
                };
            }

            void CreateFloatInput(string name, MemberRef<float> setting)
            {
                InputWithLabel input = Builder.CreateInputWithLabel
                (
                    box,
                    size.x - 30,
                    40,
                    labelText: name,
                    inputText: setting.Get().ToString(CultureInfo.InvariantCulture)
                );
                input.textInput.OnChange += value =>
                {
                    if (float.TryParse(value, out float result) && result > 0.01)
                    {
                        input.textInput.FieldColor = DefaultInputColor;
                        setting.Set(result);
                    }
                    else
                    {
                        input.textInput.FieldColor = Color.red;
                    }
                };
            }

            void CreateToggle(string name, MemberRef<bool> setting)
            {
                Builder.CreateToggleWithLabel
                (
                    box,
                    size.x - 30,
                    40,
                    setting.Get,
                    () => setting.Set(!setting.Get()),
                    labelText: name
                );
            }

            MemberRef<int> windowWidthRef = MemberRef<int>.FromProperty(settings, nameof(SettingsData.WindowWidth), WindowUI.CreateUI);
            MemberRef<int> windowHeightRef = MemberRef<int>.FromProperty(settings, nameof(SettingsData.WindowHeight), WindowUI.CreateUI);
            MemberRef<float> windowScaleRef = MemberRef<float>.FromProperty(settings, nameof(SettingsData.WindowScale), WindowUI.CreateUI);
            MemberRef<int> simulationFrequencyRef = MemberRef<int>.FromProperty(settings, nameof(SettingsData.SimulationFrequency));
            MemberRef<bool> minimizeEmptyStagesRef = MemberRef<bool>.FromProperty(settings, nameof(SettingsData.MinimizeEmptyStages), WindowUI.CreateUI);
            MemberRef<bool> statsCurrentRef = MemberRef<bool>.FromProperty(settings, nameof(SettingsData.ShowStat_Current));
            MemberRef<bool> statsTotalRef = MemberRef<bool>.FromProperty(settings, nameof(SettingsData.ShowStat_Total));

            CreateIntInput("Window Width", windowWidthRef);
            CreateIntInput("Window Height", windowHeightRef);
            CreateFloatInput("Window Scale", windowScaleRef);
            CreateIntInput("Simulation Frequency", simulationFrequencyRef);

            CreateToggle("Minimize Empty Stages", minimizeEmptyStagesRef);

            Builder.CreateSeparator(box, size.x - 30);
            Builder.CreateLabel(box, size.x - 30, 40, text: "Stats Options");
            CreateToggle("Show Current ∆V", statsCurrentRef);
            CreateToggle("Show Total ∆V", statsTotalRef);
            
            return box.gameObject;
        }

        private static GameObject CreateReadoutsUI(Transform parent)
        {
            Vector2Int size = ConfigurationMenu.ContentSize;
            Box box = Builder.CreateBox(parent, size.x, size.y);
            box.CreateLayoutGroup(LayoutType.Vertical, TextAnchor.UpperLeft, padding: new RectOffset(15, 15, 15, 15));

            foreach ((string name, MemberRef<bool> setting) in settings.ReadoutSettings(WindowUI.CreateUI))
            {
                Builder.CreateToggleWithLabel
                (
                    box,
                    size.x - 30,
                    40,
                    () => setting.Get(),
                    () => setting.Set(!setting.Get()),
                    labelText: name
                );
            }

            return box.gameObject;
        }

        protected override void RegisterOnVariableChange(Action onChange)
        {
            Application.quitting += onChange;
        }
    }

    public class SettingsData
    {
        public int WindowWidth { get; set; } = 320;
        public int WindowHeight { get; set; } = 620;
        public float WindowScale { get; set; } = 1;
        public bool WindowMinimized { get; set; } = false;

        public bool ShowStat_Current { get; set; } = false;
        public bool ShowStat_Total { get; set; } = true;

        /// How often the simulation run, in milliseconds.
        public int SimulationFrequency { get; set; } = 1000;
        public bool MinimizeEmptyStages { get; set; } = false;

        public bool ShowReadout_DeltaV { get; set; } = true;
        public bool ShowReadout_BurnTime { get; set; } = true;
        public bool ShowReadout_Thrust { get; set; } = false;
        public bool ShowReadout_Acceleration { get; set; } = true;
        public bool ShowReadout_GForce { get; set; } = false;
        public bool ShowReadout_Isp { get; set; } = true;
        public bool ShowReadout_InitialMass { get; set; } = true;
        public bool ShowReadout_FinalMass { get; set; } = true;

        public IEnumerable<(string name, MemberRef<bool> setting)> ReadoutSettings(Action onChange = null)
        {
            yield return (Name_DeltaV,       MemberRef<bool>.FromProperty(this, nameof(ShowReadout_DeltaV),       onChange));
            yield return (Name_BurnTime,     MemberRef<bool>.FromProperty(this, nameof(ShowReadout_BurnTime),     onChange));
            yield return (Name_Thrust,       MemberRef<bool>.FromProperty(this, nameof(ShowReadout_Thrust),       onChange));
            yield return (Name_Acceleration, MemberRef<bool>.FromProperty(this, nameof(ShowReadout_Acceleration), onChange));
            yield return (Name_GForce,       MemberRef<bool>.FromProperty(this, nameof(ShowReadout_GForce),       onChange));
            yield return (Name_Isp,          MemberRef<bool>.FromProperty(this, nameof(ShowReadout_Isp),          onChange));
            yield return (Name_InitialMass,  MemberRef<bool>.FromProperty(this, nameof(ShowReadout_InitialMass),  onChange));
            yield return (Name_FinalMass,    MemberRef<bool>.FromProperty(this, nameof(ShowReadout_FinalMass),    onChange));
        }

        public int ActiveReadoutCount()
        {
            return ReadoutSettings().Count(((string, MemberRef<bool>) t) => t.Item2.Get());
        }
    }
}