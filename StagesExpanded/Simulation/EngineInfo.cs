using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SFS;
using SFS.Parts.Modules;

namespace StagesExpanded.Simulation
{
    public enum EngineStatus
    {
        EngineOn,
        EngineOff,
        BoosterOn,
        BoosterOff,
    }

    public static class EngineStatusImpl
    {
        public static EngineStatus GetStatus(this EngineModule em)
        {
            if (em.engineOn.Value)
                return EngineStatus.EngineOn;
            else
                return EngineStatus.EngineOff;
        }

        public static EngineStatus GetStatus(this BoosterModule bm)
        {
            if (bm.enabled || bm.boosterPrimed.Value)
                return EngineStatus.BoosterOn;
            else
                return EngineStatus.BoosterOff;
        }

        public static EngineStatus Update(this EngineStatus status, bool resourcesAvailable)
        {
            switch (status)
            {
                case EngineStatus.EngineOn:
                    if (resourcesAvailable)
                        return EngineStatus.EngineOn;
                    else
                        return EngineStatus.EngineOff;
                case EngineStatus.EngineOff:
                        return EngineStatus.EngineOff;
                case EngineStatus.BoosterOn:
                    if (resourcesAvailable)
                        return EngineStatus.BoosterOn;
                    else
                        return EngineStatus.BoosterOff;
                case EngineStatus.BoosterOff:
                    return EngineStatus.BoosterOff;
                default:
                    throw new Exception("Stages Expanded - Invalid `EngineStatus`!");
            }
        }

        public static EngineStatus Toggle(this EngineStatus status)
        {
            switch (status)
            {
                case EngineStatus.EngineOn:
                    return EngineStatus.EngineOff;
                case EngineStatus.EngineOff:
                    return EngineStatus.EngineOn;
                case EngineStatus.BoosterOn:
                case EngineStatus.BoosterOff:
                    return EngineStatus.BoosterOn;
                default:
                    throw new Exception("Stages Expanded - Invalid `EngineStatus`!");
            }
        }

        public static EngineStatus Shutdown(this EngineStatus status)
        {
            switch (status)
            {
                case EngineStatus.EngineOn:
                case EngineStatus.EngineOff:
                    return EngineStatus.EngineOff;
                case EngineStatus.BoosterOn:
                case EngineStatus.BoosterOff:
                    return EngineStatus.BoosterOff;
                default:
                    throw new Exception("Stages Expanded - Invalid `EngineStatus`!");
            }
        }

        public static bool Running(this EngineStatus status)
        {
            return status == EngineStatus.EngineOn || status == EngineStatus.BoosterOn;
        }
    }

    /// An engine, booster, etc used in the stats calculations.
    public class EngineInfo
    {
        /// The list of `ResourceInfo`s this engine is using/will use.
        public HashSet<ResourceInfo> Resources { get; }
        /// The engine's current status.
        public EngineStatus Status { get; set; }
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
            Status = engine.GetStatus();
            if (Status.Running())
            {
                Thrust *= mapping.Throttle;
                MassFlow *= mapping.Throttle;
            }
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

        public EngineInfo(BoosterModule booster, ResourceInfo resource)
        {
            Thrust = GetThrust(booster);
            MassFlow = GetMassFlow(booster);
            Status = booster.GetStatus();
            Resources = new HashSet<ResourceInfo>() { resource };
            resource.Engines.Add(this);
        }

        public void UpdateEngine()
        {
            Resources.RemoveWhere(ri => ri.Depleted);
            Status = Status.Update(Resources.Count > 0);
            foreach (ResourceInfo ri in Resources)
            {
                if (Status.Running())
                    ri.Engines.Add(this);
                else
                    ri.Engines.Remove(this);
            }
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

        private static Double2 GetThrust(BoosterModule bm)
        {
            // ? Slight modification of `BoosterModule.FixedUpdate()`.
            Vector2 vector = bm.thrustVector.Value;
            if (Base.worldBase.AllowsCheats)
                return bm.transform.TransformVector(vector).ToDouble2();
            else
                return (Double2) bm.transform.TransformVectorUnscaled(vector);
        }

        private static double GetMassFlow(BoosterModule bm)
        {
            // * To paraphrase Altaïr: "stretching a booster increases its thrust but not its mass flow".
            // ? https://github.com/Kaskouy/SFS-DeltaV-calculator/blob/main/DeltaV_Simulator.cs#L120
            double isp = bm.ISP.Value * Base.worldBase.settings.difficulty.IspMultiplier;
            double thrust = bm.transform.TransformVectorUnscaled(bm.thrustVector.Value).magnitude;
            return thrust / isp;
        }
    }
}