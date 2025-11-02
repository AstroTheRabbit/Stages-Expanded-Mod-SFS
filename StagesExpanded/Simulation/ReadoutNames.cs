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
                    return result + value.ToTimestampString(true, true);
                case Name_Thrust:
                    return result + value.ToMassString(1);
                case Name_Acceleration:
                    return result + value.ToString(2, false) + "m/s²";
                case Name_GForce:
                    return result + value.ToString(2, true) + "g";
                case Name_Isp:
                    return result + value.ToString(1, false) + "s";
                case Name_InitialMass:
                    return result + value.ToMassString(2);
                case Name_FinalMass:
                    return result + value.ToMassString(2);
                default:
                    throw new ArgumentException($"Stages Expanded - '{name}' is not a valid readout name!");
            }
        }

        static string ToMassString(this double value, int decimals)
        {
            return value.ToString(decimals, true) + Loc.main.Mass_Unit;
        }
    }
}