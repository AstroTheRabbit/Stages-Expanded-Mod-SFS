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
        private bool engineOn = false;
        /// Determines whether or not the engine is currently running.
        public bool EngineOn
        {
            get
            {
                Debug.Log($"engine with thrust of {Thrust.magnitude / 9.8} has {Resources.Count} sources available");
                return engineOn &= Resources.Count > 0;
            }
            set
            {
                engineOn = value;
            }
        }
        /// The thrust vector of the engine.
        public Double2 Thrust { get; }
        /// The mass flow of the engine.
        public double MassFlow { get; }
        /// The mass flow of the engine for a specific `ResourceInfo`.
        // ? `FlowModule.Flow.FlowNegative()`
        // ? https://www.desmos.com/calculator/4frnkcpwyh
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
            foreach (ResourceInfo ri in Resources)
            {
                ri.Engines.Add(this);
            }
        }

        private static Double2 GetThrust(EngineModule em)
        {
            // ? Slight modification of `EngineModule.FixedUpdate()`.
            Vector2 vector = em.thrustNormal.Value * em.thrust.Value * 9.8f;
            if (Base.worldBase.AllowsCheats)
                return em.transform.TransformVector(vector).ToDouble2();
            else
                return (Double2) em.transform.TransformVectorUnscaled(vector);
        }

        private static double GetISP(EngineModule em)
        {
            return (double) em.ISP.Value * Base.worldBase.settings.difficulty.IspMultiplier;
        }

        private static double GetMassFlow(EngineModule em)
        {
            // ? `EngineModule.RecalculateMassFlow()`.
            // * The differing (incorrect) calculation for thrust here may seem strange,
            // * but it's actually a bug with the way SFS calculates mass flow!
            // TODO: This needs to be tested properly, since I'm still not sure if the mass flow of the ∆V calculation
            // TODO: should always be thrust / isp, or if it should use the *actual* mass flow equation used in-game.
            double thrust = 9.8 * em.thrust.Value * em.transform.TransformVector(em.thrustNormal.Value).magnitude;
            return thrust / GetISP(em);
        }
    }
}