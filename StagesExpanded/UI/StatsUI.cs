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
        private static TextAdapter text_stat = null;

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
            text_stat = stat.Find("Text").GetComponent<TextAdapter>();
        }

        private static void UpdateUI(Rocket rocket, RocketInfo info)
        {
            if (info == null)
                text_stat.Text = "-";
            else if (SandboxSettings.main.settings.infiniteFuel)
                text_stat.Text = "∞" + Loc.main.Meter_Per_Second_Unit;
            else
                text_stat.Text = info.TotalResults.DeltaV.ToVelocityString();
        }
    }
}