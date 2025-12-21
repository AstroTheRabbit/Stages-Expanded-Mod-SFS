using ModLoader.Helpers;
using UnityEngine;
using UnityEngine.UI;
using SFS.UI;
using SFS.World;
using SFS.Translations;
using StagesExpanded.Simulation;
using UnityEngine.SceneManagement;

namespace StagesExpanded.UI
{
    public static class StatsUI
    {
        private static GameObject holder_stats = null;
        private static GameObject separator_current = null;
        private static GameObject holder_current = null;
        private static TextAdapter title_current = null;
        private static TextAdapter text_current = null;
        private static GameObject separator_total = null;
        private static GameObject holder_total = null;
        private static TextAdapter title_total = null;
        private static TextAdapter text_total = null;

        public static void Init()
        {
            SceneHelper.OnWorldSceneLoaded += CreateUI_World;
            SceneHelper.OnBuildSceneLoaded += CreateUI_Build;
            SimulationManager.OnResultChanged += UpdateUI;
        }

        private static void CreateUI_World()
        {
            Transform holder = GameObject.Find("/--- UI ---/Main UI/Top Center Stats/Holder").transform;
            Transform separator = holder.Find("Separator");
            Transform stat = holder.Find("TWR");
            CreateUI(holder, separator, stat);
        }

        private static void CreateUI_Build()
        {
            Transform holder = GameObject.Find("/--- UI ---/Rocket Stats/TextInfoPanel").transform;
            Transform separator = holder.Find("Separator");
            Transform stat = holder.Find("PartCount");
            CreateUI(holder, separator, stat);
        }

        private static void CreateUI(Transform holder, Transform separator, Transform stat)
        {
            holder_stats = holder.gameObject;

            // TODO: Make `stat` auto-scale to fit `text` with fixed text size?
            separator_current = Object.Instantiate(separator, holder, true).gameObject;
            stat = Object.Instantiate(stat, holder, true);
            holder_current = stat.gameObject;
            title_current = stat.Find("Title").GetComponent<TextAdapter>();
            text_current = stat.Find("Text").GetComponent<TextAdapter>();

            separator_total = Object.Instantiate(separator, holder, true).gameObject;
            stat = Object.Instantiate(stat, holder, true);
            holder_total = stat.gameObject;
            title_total = stat.Find("Title").GetComponent<TextAdapter>();
            text_total = stat.Find("Text").GetComponent<TextAdapter>();
        }

        private static void UpdateUI(SimulationInput input, SimulationOutput output)
        {
            if (holder_stats?.gameObject == null)
                return;

            bool show_current = Settings.settings.ShowStat_Current;
            bool show_total = Settings.settings.ShowStat_Total;

            Debug.Log(Main.DeltaVCalculatorActive);
            if (Main.DeltaVCalculatorActive)
            {
                // * If "∆V calculator" is installed & active, only show Stages Expanded's "Current ∆V" stat in the build scene.
                show_current &= SceneManager.GetActiveScene().name == "Build_PC";
            }

            separator_current.SetActive(show_current);
            holder_current.SetActive(show_current);
            separator_total.SetActive(show_total);
            holder_total.SetActive(show_total);

            LayoutRebuilder.MarkLayoutForRebuild(holder_stats.Rect());

            if (show_current)
            {
                title_current.Text = "Current ∆V";
                if (output == null)
                    text_current.Text = "-";
                else if (SandboxSettings.main.settings.infiniteFuel)
                    text_current.Text = "∞" + Loc.main.Meter_Per_Second_Unit;
                else
                    text_current.Text = output.CurrentStageResult.DeltaV.ToVelocityString();
            }
            
            if (show_total)
            {
                title_total.Text = "Total ∆V";
                if (output == null)
                    text_total.Text = "-";
                else if (SandboxSettings.main.settings.infiniteFuel)
                    text_total.Text = "∞" + Loc.main.Meter_Per_Second_Unit;
                else
                    text_total.Text = output.TotalResults.DeltaV.ToVelocityString();
            }
        }
    }
}