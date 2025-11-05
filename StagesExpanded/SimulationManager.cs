using System;
using System.Threading;
using ModLoader.Helpers;
using SFS.World;
using UnityEngine;

namespace StagesExpanded.Simulation
{
    public static class SimulationManager
    {
        private static Thread simulationThread = null;
        private static bool simulationRunning = false;
        private static readonly object simulationLock = new object();
        private static SynchronizationContext unityContext;

        private static Rocket rocket;
        private static RocketInfo result;

        public static event Action<Rocket, RocketInfo> OnResultChanged;

        public static void Init()
        {
            unityContext = SynchronizationContext.Current;

            SceneHelper.OnWorldSceneLoaded += () =>
            {
                PlayerController.main.player.OnChange += OnPlayerChange;
                StartThread();
            };
            SceneHelper.OnWorldSceneUnloaded += () =>
            {
                StopThread();
                PlayerController.main.player.OnChange -= OnPlayerChange;
            };
        }

        private static void OnPlayerChange(Player player)
        {
            lock (simulationLock)
            {
                PostResultChanged
                (
                    rocket = player as Rocket,
                    result = null
                );
            }
        }

        private static void StartThread()
        {
            if (simulationRunning)
                return;
            simulationRunning = true;
            simulationThread = new Thread(SimulationLoop)
            {
                Name = "Stages Expanded - Simulation Thread",
                IsBackground = true,
            };
            simulationThread.Start();
        }

        private static void StopThread()
        {
            simulationRunning = false;
            simulationThread?.Join();
            simulationThread = null;
        }

        private static void PostResultChanged(Rocket rocket, RocketInfo info)
        {
            unityContext.Post(_ => OnResultChanged(rocket, info), null);
        }

        private static void SimulationLoop()
        {
            while (simulationRunning)
            {
                try
                {
                    Rocket copy_rocket;
                    lock (simulationLock)
                    {
                        copy_rocket = rocket;
                    }

                    RocketInfo copy_result = null;
                    if (copy_rocket != null && copy_rocket.hasControl)
                    {
                        copy_result = RocketInfo.Generate(copy_rocket);
                    }

                    lock (simulationLock)
                    {
                        PostResultChanged(rocket, result = copy_result);
                    }

                }
                catch (Exception e)
                {
                    Debug.LogError($"Stages Expanded - Simulation thread error: {e}");
                }
                Thread.Sleep(Settings.settings.SimulationFrequency);
            }
        }
    }
}