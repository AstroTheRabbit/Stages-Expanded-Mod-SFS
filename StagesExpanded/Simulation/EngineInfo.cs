using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SFS;
using SFS.Parts.Modules;

namespace StagesExpanded
{
    /// An engine, booster, etc used in the stats calculations.
    public class EngineInfo
    {
        /// The list of `ResourceInfo`s this engine is using.
        public HashSet<ResourceInfo> Resources { get; }
        /// Determines whether or not the engine is currently running.
        public bool EngineOn { get; private set; }
        /// The thrust vector of the engine.
        public Double2 Thrust { get; }
        /// The mass flow of the engine.
        public double MassFlow { get; }
        /// The mass flow of the engine for a specific `ResourceInfo`.
        /// ? `FlowModule.Flow.FlowNegative()`
        /// ? https://www.desmos.com/calculator/4frnkcpwyh
        public double MassFlowPerResource(ResourceInfo ri) => MassFlow * ri.WetMass / Resources.Sum(r => r.WetMass);

        public EngineInfo(EngineModule engine, ModuleMapping mapping)
        {
            Thrust = GetThrust(engine);
            MassFlow = GetMassFlow(engine);
            EngineOn = engine.engineOn.Value;
            Resources = new HashSet<ResourceInfo>();

            Stack<ResourceModule> stack = new Stack<ResourceModule>(engine.source.sources.SelectMany(s => s.sources));
            while (stack.TryPop(out ResourceModule rm))
            {
                if (mapping.TryGetResource(rm, out ResourceInfo ri))
                {
                    // * `rm` is an "actual" resource module (e.g. a fuel tank).
                    Resources.Add(ri);
                }
                else
                {
                    // * `rm` is a "parent" resource module which holds a group of "child" resource modules (which themselves could be parents).
                    foreach (ResourceModule child in rm.children)
                    {
                        stack.Push(child);
                    }
                }
            }
        }

        public void UpdateEngineOn()
        {
            EngineOn &= Resources.Any(ri => ri.WetMass > 0);
            foreach (ResourceInfo ri in Resources)
            {
                if (EngineOn)
                    ri.Engines.Add(this);
                else
                    ri.Engines.Remove(this);
            }
        }

        public void ToggleEngine()
        {
            EngineOn = !EngineOn;
            UpdateEngineOn();
        }

        public void ShutdownEngine()
        {
            EngineOn = false;
            UpdateEngineOn();
        }

        private static Double2 GetThrust(EngineModule em)
        {
            // ? Slight modification of `EngineModule.FixedUpdate()`.
            Vector2 vector = em.thrustNormal.Value * em.thrust.Value;
            if (Base.worldBase.AllowsCheats)
                return em.transform.TransformVector(vector).ToDouble2();
            else
                return (Double2) em.transform.TransformVectorUnscaled(vector);
        }

        private static double GetMassFlow(EngineModule em)
        {
            // ? `EngineModule.RecalculateMassFlow()`.
            // * The differing (incorrect) calculation for thrust here may seem strange,
            // * but it's actually a bug with the way SFS calculates mass flow!
            double thrust = em.thrust.Value * em.transform.TransformVector(em.thrustNormal.Value).magnitude;
            double isp = (double) em.ISP.Value * Base.worldBase.settings.difficulty.IspMultiplier;
            return thrust / isp;
        }
    }
}