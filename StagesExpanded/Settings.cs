using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UITools;
using SFS.IO;
using SFS.UI.ModGUI;
using StagesExpanded.UI;
using LayoutType = SFS.UI.ModGUI.Type;
using static StagesExpanded.ReadoutNames;

namespace StagesExpanded
{
    public class Settings : ModSettings<SettingsData>
    {
        public static Settings main;
        private static FilePath settingsFile;
        protected override FilePath SettingsFile => settingsFile;

        public static void Init(string modFolder)
        {
            main = new Settings();
            settingsFile = new FolderPath(modFolder).ExtendToFile("settings.txt");
            main.Initialize();
            main.AddUI();
        }

        void AddUI()
        {
            ConfigurationMenu.Add
            (
                "Stages Expanded",
                new (string, Func<Transform, GameObject>)[]
                {
                    ("Readouts", (Transform transform) => CreateReadoutUI(transform, ConfigurationMenu.ContentSize)),
                }
            );
        }

        GameObject CreateReadoutUI(Transform parent, Vector2Int size)
        {
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
        public int WindowHeight { get; set; } = 600;
        /// Should the window start minimized?
        public bool WindowMinimized { get; set; } = false;

        /// How often the simulation run, in milliseconds.
        public int SimulationFrequency { get; set; } = 100;

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