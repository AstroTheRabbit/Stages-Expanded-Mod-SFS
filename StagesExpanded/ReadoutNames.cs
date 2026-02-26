using System;
using SFS.Translations;

namespace StagesExpanded
{
    public static class ReadoutNames
    {
        public const string Name_DeltaV = "∆V";
        public const string Name_BurnTime = "Burn Time";
        public const string Name_Thrust = "Thrust";
        public const string Name_Acceleration = "Acceleration";
        public const string Name_GForce = "G-Force";
        public const string Name_Isp = "Isp";
        public const string Name_InitialMass = "Initial Mass";
        public const string Name_FinalMass = "Final Mass";

        public static string ToReadoutString(this double value, string name)
        {
            string result = name + ": ";
            switch (name)
            {
                case Name_DeltaV:
                    return result + value.ToVelocityString();
                case Name_BurnTime:
                    return result + value.ToBurnTimeString();
                case Name_Thrust:
                    return result + value.ToMassString(1);
                case Name_Acceleration:
                    return result + value.ToString(2, false) + "m/s²";
                case Name_GForce:
                    return result + value.ToString(2, true) + "g";
                case Name_Isp:
                    return result + value.ToString(1, false) + "s";
                case Name_InitialMass:
                case Name_FinalMass:
                    return result + value.ToMassString(2);
                default:
                    throw new ArgumentException($"Stages Expanded - '{name}' is not a valid readout name!");
            }
        }

        private static string ToBurnTimeString(this double value)
        {
            TimeSpan span = TimeSpan.FromSeconds(value);
            string result = "";
            if (span.Days > 0)
                result += Loc.main.Day_Short.Inject(span.Days.ToString(), "value");
            if (result != "" || span.Hours > 0)
                result += Loc.main.Hour_Short.Inject(span.Hours.ToString(), "value");
            if (result != "" || span.Minutes > 0)
                result += Loc.main.Minute_Short.Inject(span.Minutes.ToString(), "value");
            if (result == "" || span.TotalSeconds < 10)
                result += Loc.main.Second_Short.Inject(span.TotalSeconds.ToString(1, true), "value");
            else
                result += Loc.main.Second_Short.Inject(span.Seconds.ToString(), "value");
            return result;
        }

        private static string ToMassString(this double value, int decimals)
        {
            return value.ToString(decimals, true) + Loc.main.Mass_Unit;
        }
    }
}