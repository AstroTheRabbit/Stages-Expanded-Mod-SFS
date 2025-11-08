using System.Linq;
using System.Collections.Generic;
using static StagesExpanded.ReadoutNames;
using ModLoader.IO;

namespace StagesExpanded.Simulation
{
    /// The result of `PhaseInfo` calculations.
    public class PhaseResult
    {
        public double Thrust { get; private set; } = double.NaN;
        public double Acceleration { get; private set; } = double.NaN;
        public double GForce => Acceleration / 9.8;
        public double BurnTime { get; private set; } = double.NaN;
        public double Isp { get; private set; } = double.NaN;
        public double DeltaV { get; private set; } = double.NaN;
        public double InitialMass { get; private set; } = double.NaN;
        public double FinalMass { get; private set; } = double.NaN;

        public static PhaseResult EmptyResult(double initialMass, double finalMass)
        {
            return new PhaseResult()
            {
                Thrust = 0,
                Acceleration = 0,
                BurnTime = 0,
                Isp = 0,
                DeltaV = 0,
                InitialMass = initialMass,
                FinalMass = finalMass,
            };
        }

        public static PhaseResult FromPhaseInfo(PhaseInfo pi)
        {
            double thrust = pi.Thrust.magnitude;
            return new PhaseResult()
            {
                Thrust = thrust,
                Acceleration = 9.8 * thrust / pi.TotalMass,
                BurnTime = pi.BurnTime,
                Isp = pi.Isp,
                DeltaV = pi.DeltaV,
                InitialMass = pi.TotalMass,
                FinalMass = pi.FinalMass,
            };

            // double GetSpaceCenterGravity()
            // {
            //     SpaceCenterData spaceCenter = Base.planetLoader.spaceCenter;
            //     return spaceCenter.address.GetPlanet().GetGravity(spaceCenter.LaunchPadLocation.position.magnitude);
            // }
        }

        /// Combines the results of `phases` into a single stage `PhaseResult`. Returns `null` if `phases` is empty.
        public static PhaseResult Merge(IEnumerable<PhaseResult> phases)
        {
            if (phases.Count() == 0)
                return null;
            
            PhaseResult first = phases.First();
            PhaseResult last = phases.Last();
            return new PhaseResult()
            {
                Thrust = first.Thrust,
                Acceleration = first.Acceleration,
                Isp = first.Isp,
                BurnTime = phases.Sum(pr => pr.BurnTime),
                DeltaV = phases.Sum(pr => pr.DeltaV),
                InitialMass = first.InitialMass,
                FinalMass = last.FinalMass,
            };
        }

        public IEnumerable<(string name, double result)> Results()
        {
            if (Settings.settings.ShowReadout_DeltaV      ) yield return (Name_DeltaV      , DeltaV      );
            if (Settings.settings.ShowReadout_BurnTime    ) yield return (Name_BurnTime    , BurnTime    );
            if (Settings.settings.ShowReadout_Thrust      ) yield return (Name_Thrust      , Thrust      );
            if (Settings.settings.ShowReadout_Acceleration) yield return (Name_Acceleration, Acceleration);
            if (Settings.settings.ShowReadout_GForce      ) yield return (Name_GForce      , GForce      );
            if (Settings.settings.ShowReadout_Isp         ) yield return (Name_Isp         , Isp         );
            if (Settings.settings.ShowReadout_InitialMass ) yield return (Name_InitialMass , InitialMass );
            if (Settings.settings.ShowReadout_FinalMass   ) yield return (Name_FinalMass   , FinalMass   );
        }

        public void DebugPrint(string phaseName)
        {
            Console.main.WriteText(phaseName + ":");
            foreach ((string name, double result) in Results())
            {
                Console.main.WriteText($"  {name}: {result}");
            }
        }
    }
}