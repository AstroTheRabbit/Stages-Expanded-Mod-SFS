using System;
using System.Linq;
using System.Collections.Generic;
using UITools;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SFS.UI;
using SFS.UI.ModGUI;
using ModLoader.Helpers;
using StagesExpanded.Simulation;
using Object = UnityEngine.Object;
using LayoutType = SFS.UI.ModGUI.Type;
using static StagesExpanded.ReadoutNames;

namespace StagesExpanded.UI
{
    public static class WindowUI
    {
        private static readonly int windowID = Builder.GetRandomID();
        private static GameObject holder;
        private static ClosableWindow window;
        private static ScrollElement scroll;

        private static SimulationInput previousInput = null;
        private static readonly Queue<StageUI> pool = new Queue<StageUI>();
        private static readonly Dictionary<int, StageUI.State> states = new Dictionary<int, StageUI.State>();

        public static void Init()
        {
            SceneHelper.OnWorldSceneLoaded += CreateUI;
            SceneHelper.OnWorldSceneUnloaded += DestroyUI;
            SceneHelper.OnBuildSceneLoaded += CreateUI;
            SceneHelper.OnBuildSceneUnloaded += DestroyUI;
            SimulationManager.OnResultChanged += UpdateUI;
        }

        public static void CreateUI()
        {
            DestroyUI();
            string name = SceneManager.GetActiveScene().name;
            if (Settings.settings.ActiveReadoutCount() == 0 || name != "World_PC" && name != "Build_PC")
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
            scroll = window.ChildrenHolder.GetComponent<ScrollElement>();
            
            float scale = Settings.settings.WindowScale;
            window.rectTransform.localScale = new Vector3(scale, scale, 1);
        }

        static void UpdateUI(SimulationInput input, SimulationOutput output)
        {
            if (window?.gameObject == null)
                return;

            if (input == null || output == null)
            {
                ClearStates();
                window.Active = false;
                return;
            }
            else
            {
                window.Active = true;
            }

            if (input.ResetWindowUI(previousInput))
            {
                ClearStates();
                scroll.ResetPosition();
            }
            previousInput = input;

            int required = output.StageResults.Count + 1;
            while (pool.Count > required)
            {
                pool.Dequeue().Destroy();
            }
            while (pool.Count < required)
            {
                pool.Enqueue(new StageUI(window, scroll));
            }
            scroll.Move(Vector2.zero);

            var iter = pool.Zip
            (
                output.AllResults(),
                (ui, tuple) => (ui, tuple.id, tuple.result)
            );
            foreach ((StageUI ui, int id, PhaseResult result) in iter)
            {
                if (!states.TryGetValue(id, out StageUI.State state))
                {
                    bool minimized = Settings.settings.MinimizeEmptyStages && result.IsEmpty;
                    state = new StageUI.State(minimized);
                    states.Add(id, state);
                }
                ui.Update(state, result, id);
            }
        }

        public static void DestroyUI()
        {
            if (holder != null)
                Object.Destroy(holder);
            ClearStates();
            pool.Clear();
        }

        static void ClearStates()
        {
            states.Clear();
        }
    }

    public class StageUI
    {
        public class State
        {
            public bool Minimized { get; set; }
            public State(bool minimized = false)
            {
                Minimized = minimized;
            }
        }

        private readonly ClosableWindow window;
        private readonly Label label_DeltaV = null;
        private readonly Label label_BurnTime = null;
        private readonly Label label_Thrust = null;
        private readonly Label label_Acceleration = null;
        private readonly Label label_GForce = null;
        private readonly Label label_Isp = null;
        private readonly Label label_InitialMass = null;
        private readonly Label label_FinalMass = null;
        private State currentState = null;

        public StageUI(Window holder, ScrollElement scroll)
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
                savePosition: false
            );
            window.CreateLayoutGroup
            (
                LayoutType.Vertical,
                TextAnchor.MiddleLeft,
                5,
                new RectOffset(window_padding, window_padding, window_padding, window_padding)
            );

            // * Stops the inner window from "intercepting" scroll inputs which should be going to the main outer window.
            Object.Destroy(window.rectTransform.GetComponent<ButtonPC>());
            // * Corrects the positions of the inner windows if this inner window is minimized or maximized.
            window.OnMinimizedChangedEvent += () =>
            {
                if (currentState != null)
                    currentState.Minimized = window.Minimized;
                LayoutRebuilder.MarkLayoutForRebuild(holder.ChildrenHolder.Rect());
                scroll.Move(Vector2.zero);

            };

            foreach ((_, MemberRef<Label> labelRef) in Labels())
            {
                Label label = Builder.CreateLabel(window, label_width, label_height);
                label.TextAlignment = TMPro.TextAlignmentOptions.TopLeft;
                labelRef.Set(label);
            }
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

        public void Destroy()
        {
            if (window != null)
                Object.Destroy(window.gameObject);
        }

        public void Update(State state, PhaseResult result, int stageId)
        {
            if (window?.gameObject == null)
                return;
            
            currentState = state;
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
            window.Title = stageId == 0 ? "Current Stage" : $"Stage {stageId}";
            window.Minimized = state.Minimized;
        }
    }
}