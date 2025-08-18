using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SFS;
using SFS.Parts.Modules;

namespace StagesExpanded
{
    public class EngineGroup
    {
        public List<EngineModule> engines;

        IEnumerable<EngineModule> CurrentEngines(bool activeStage)
        {
            if (activeStage)
                return engines.Where(em => em.engineOn.Value);
            else
                return engines;
        }

        /// The total thrust of the flow group's engines.
        /// This can actually be less than expected if the engines are not all facing the same direction.
        public float Thrust(bool activeStage) => CurrentEngines(activeStage).Select(GetThrust).Sum().magnitude;
        /// The total mass flow of the flow group's engines.
        public float MassFlow(bool activeStage) => CurrentEngines(activeStage).Select(GetMassFlow).Sum();
        /// The total specific impulse of the flow group's engines.
        // ? https://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines
        public float Isp(bool activeStage) => Thrust(activeStage) / MassFlow(activeStage);

        static Vector2 GetThrust(EngineModule em)
        {
            // ? Slight modification of `EngineModule.FixedUpdate()`.
            Vector2 vector = em.thrustNormal.Value * em.thrust.Value * 9.8f;
            if (Base.worldBase.AllowsCheats)
                return em.transform.TransformVector(vector);
            else
                return em.transform.TransformVectorUnscaled(vector);
        }

        static float GetISP(EngineModule em)
        {
            return em.ISP.Value * (float) Base.worldBase.settings.difficulty.IspMultiplier;
        }

        static float GetMassFlow(EngineModule em)
        {
            // ? `EngineModule.RecalculateMassFlow()`.
            // * The differing (incorrect) calculation for thrust here may seem strange,
            // * but it's actually a bug with the way SFS calculates mass flow!
            // TODO: This needs to be tested properly, since I'm still not sure if the mass flow of the ∆V calculation
            // TODO: should always be thrust / isp, or if it should use the *actual* mass flow equation used in-game.
            // TODO: It may still need to be multiplied by 9.8 like in `GetThrust()`, but idk at this point...
            float thrust = em.thrust.Value * em.transform.TransformVector(em.thrustNormal.Value).magnitude;
            return thrust / GetISP(em);
        }
    }
}