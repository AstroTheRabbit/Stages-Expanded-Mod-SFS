using ModLoader.Helpers;
using UnityEngine;
using SFS.UI;
using SFS.World;
using SFS.Translations;
using StagesExpanded.Simulation;

namespace StagesExpanded.UI
{
    public static class StatsUI
    {
        private static TextAdapter stat_title = null;
        private static TextAdapter stat_text = null;

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

            Object.Instantiate(separator, holder, true);
            stat = Object.Instantiate(stat, holder, true);

            stat_title = stat.Find("Title").GetComponent<TextAdapter>();
            stat_text = stat.Find("Text").GetComponent<TextAdapter>();

            // TODO: Make `stat` auto-scale to fit `stat_text` with fixed text size?
        }

        private static void CreateUI_Build()
        {
            Transform holder = GameObject.Find("/--- UI ---/Rocket Stats/TextInfoPanel").transform;
            Transform separator = holder.Find("Separator");
            Transform stat = holder.Find("PartCount");

            Object.Instantiate(separator, holder, true);
            stat = Object.Instantiate(stat, holder, true);

            stat_title = stat.Find("Title").GetComponent<TextAdapter>();
            stat_text = stat.Find("Text").GetComponent<TextAdapter>();
        }

        private static void UpdateUI(SimulationInput input, SimulationOutput output)
        {
            if (stat_text?.gameObject == null)
                return;
            
            stat_title.Text = "∆V";
            if (output == null)
                stat_text.Text = "-";
            else if (SandboxSettings.main.settings.infiniteFuel)
                stat_text.Text = "∞" + Loc.main.Meter_Per_Second_Unit;
            else
                stat_text.Text = output.TotalResults.DeltaV.ToVelocityString();
        }
    }
}