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
        private static TextAdapter stat_text = null;

        public static void Init()
        {
            SceneHelper.OnWorldSceneLoaded += CreateUI;
            SimulationManager.OnResultChanged += UpdateUI;
        }

        private static void CreateUI()
        {
            Transform holder = GameObject.Find("/--- UI ---/Main UI/Top Center Stats/Holder").transform;
            Transform separator = holder.Find("Separator");
            Transform stat = holder.Find("TWR");

            Object.Instantiate(separator, holder, true);
            stat = Object.Instantiate(stat, holder, true);

            stat.Find("Title").GetComponent<TextAdapter>().Text = "∆V";
            stat_text = stat.Find("Text").GetComponent<TextAdapter>();

            // TODO: Make `stat` auto-scale to fit `stat_text` with fixed text size?
        }

        private static void UpdateUI(Rocket rocket, RocketInfo info)
        {
            if (info == null)
                stat_text.Text = "-";
            else if (SandboxSettings.main.settings.infiniteFuel)
                stat_text.Text = "∞" + Loc.main.Meter_Per_Second_Unit;
            else
                stat_text.Text = info.TotalResults.DeltaV.ToVelocityString();
        }
    }
}