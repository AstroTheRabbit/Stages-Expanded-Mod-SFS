using System;
using System.Linq;
using System.Collections.Generic;
using UITools;
using UnityEngine;
using SFS.UI;
using SFS.World;
using SFS.UI.ModGUI;
using ModLoader.Helpers;
using StagesExpanded.Simulation;
using Object = UnityEngine.Object;
using LayoutType = SFS.UI.ModGUI.Type;
using static StagesExpanded.ReadoutNames;
using UnityEngine.UI;

namespace StagesExpanded
{
    public static class UI
    {
        private static StageReadout currentStageReadout = null;
        /// Key is `Stage.stageId`.
        private static Dictionary<int, StageReadout> stageReadouts = new Dictionary<int, StageReadout>();

        private static readonly int windowID = Builder.GetRandomID();
        private static GameObject holder;
        private static ClosableWindow window;

        public static void Init()
        {
            SceneHelper.OnWorldSceneLoaded += CreateUI;
            SceneHelper.OnWorldSceneUnloaded += DestroyUI;
            SimulationManager.OnResultChanged += UpdateUI;
        }

        public static void CreateUI()
        {
            DestroyUI();
            if (Settings.settings.ActiveReadoutCount() == 0)
                return;
            
            holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "Stages Expanded - Window Holder");
            window = UIToolsBuilder.CreateClosableWindow
            (
                holder.transform,
                windowID,
                Settings.settings.WindowWidth,
                Settings.settings.WindowHeight,
                draggable: true,
                savePosition: true,
                titleText: "Stages Expanded"
            );
            window.CreateLayoutGroup(LayoutType.Vertical, spacing: 5);
            window.EnableScrolling(LayoutType.Vertical);
            window.RegisterPermanentSaving(Main.main.ModNameID);

            window.Minimized = Settings.settings.WindowMinimized;
            window.OnMinimizedChangedEvent += () => Settings.settings.WindowMinimized = window.Minimized;
        }

        static void UpdateUI(Rocket rocket, RocketInfo info)
        {
            if (holder == null)
                return;

            // ! TESTING ONLY
            // TODO: Need to optimize the updating of the inner windows (I'm currently just destroying and re-creating them).
            // TODO: Also need to make the minimized state of the inner windows persistent.
            currentStageReadout?.Destroy();
            foreach (StageReadout readout in stageReadouts.Values)
            {
                readout?.Destroy();
            }
            stageReadouts.Clear();

            if (info == null)
            {
                window.Active = false;
                return;
            }
            window.Active = true;

            if (window.Minimized)
                return;

            currentStageReadout = new StageReadout("Current Stage", info.CurrentStageResult, window);
            foreach (Stage stage in rocket.staging.stages)
            {
                StageReadout readout = new StageReadout($"Stage {stage.stageId}", info.StageResults[stage], window);
                stageReadouts.Add(stage.stageId, readout);
            }
        }

        public static void DestroyUI()
        {
            if (holder != null)
                Object.Destroy(holder);
            stageReadouts.Clear();
        }
    }

    public class StageReadout
    {
        private ClosableWindow window;
        private Label label_DeltaV = null;
        private Label label_BurnTime = null;
        private Label label_Thrust = null;
        private Label label_Acceleration = null;
        private Label label_GForce = null;
        private Label label_Isp = null;
        private Label label_InitialMass = null;
        private Label label_FinalMass = null;

        public StageReadout(string stage, PhaseResult result, Window holder)
        {
            int label_spacing = 5;
            int window_padding = 5;
            int window_width = Settings.settings.WindowWidth - (2 * window_padding);
            int label_height = 30;
            int label_width = window_width - (2 * window_padding);
            int window_height = (label_height + label_spacing) * Settings.settings.ActiveReadoutCount() + label_spacing + (2 * window_padding) + 50;

            window = UIToolsBuilder.CreateClosableWindow
            (
                holder,
                Builder.GetRandomID(),
                window_width,
                window_height,
                draggable: false,
                savePosition: false,
                titleText: stage
            );
            // * Stops the inner window from "intercepting" scroll inputs which should be going to the main outer window.
            Object.Destroy(window.rectTransform.GetComponent<ButtonPC>());
            // * Corrects the positions of the inner windows if this inner window is minimized or maximized.
            window.OnMinimizedChangedEvent += () =>
            {
                // holder.ChildrenHolder.GetComponent<VerticalLayoutGroup>().SetLayoutVertical();
                LayoutRebuilder.MarkLayoutForRebuild(holder.ChildrenHolder.Rect());
                holder.ChildrenHolder.GetComponent<ScrollElement>().Move(Vector2.zero);
            };
            window.CreateLayoutGroup
            (
                LayoutType.Vertical,
                TextAnchor.MiddleLeft,
                5,
                new RectOffset(window_padding, window_padding, window_padding, window_padding)
            );
            CreateLabels(label_width, label_height);
            UpdateLabels(result);
        }

        public void Destroy()
        {
            if (window != null)
                Object.Destroy(window.gameObject);
        }

        IEnumerable<(string name, MemberRef<Label> label)> Labels()
        {
            if (Settings.settings.ShowReadout_DeltaV      ) yield return (Name_DeltaV      , MemberRef<Label>.FromField(this, nameof(label_DeltaV      )));
            if (Settings.settings.ShowReadout_BurnTime    ) yield return (Name_BurnTime    , MemberRef<Label>.FromField(this, nameof(label_BurnTime    )));
            if (Settings.settings.ShowReadout_Thrust      ) yield return (Name_Thrust      , MemberRef<Label>.FromField(this, nameof(label_Thrust      )));
            if (Settings.settings.ShowReadout_Acceleration) yield return (Name_Acceleration, MemberRef<Label>.FromField(this, nameof(label_Acceleration)));
            if (Settings.settings.ShowReadout_GForce      ) yield return (Name_GForce      , MemberRef<Label>.FromField(this, nameof(label_GForce      )));
            if (Settings.settings.ShowReadout_Isp         ) yield return (Name_Isp         , MemberRef<Label>.FromField(this, nameof(label_Isp         )));
            if (Settings.settings.ShowReadout_InitialMass ) yield return (Name_InitialMass , MemberRef<Label>.FromField(this, nameof(label_InitialMass )));
            if (Settings.settings.ShowReadout_FinalMass   ) yield return (Name_FinalMass   , MemberRef<Label>.FromField(this, nameof(label_FinalMass   )));
        }

        void CreateLabels(int width, int height)
        {
            foreach ((string name, MemberRef<Label> labelRef) in Labels())
            {
                Label label = Builder.CreateLabel(window, width, height);
                label.TextAlignment = TMPro.TextAlignmentOptions.TopLeft;
                labelRef.Set(label);
            }
        }

        public void UpdateLabels(PhaseResult result)
        {
            var iter = Enumerable.Zip
            (
                Labels(),
                result.Results(),
                (l, r) =>
                {
                    if (l.name != r.name)
                        throw new Exception($"Stages Expanded - Label name '{l.name}' does not match readout name '{r.name}'!");
                    return (l.label.Get(), l.name, r.result);
                }
            );
            foreach ((Label label, string name, double value) in iter)
            {
                label.Text = value.ToReadoutString(name);
            }
        }
    }
}