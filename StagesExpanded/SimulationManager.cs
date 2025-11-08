using System;
using System.Threading;
using ModLoader.Helpers;
using SFS.Input;
using SFS.World;
using UnityEngine;

namespace StagesExpanded.Simulation
{
    public static class SimulationManager
    {
        private static Rocket rocket = null;
        private static Thread simulationThread = null;
        private static bool simulationRunning = false;
        private static readonly object simulationLock = new object();
        private static readonly AutoResetEvent simulationResetEvent = new AutoResetEvent(false);
        private static SynchronizationContext unityContext = null;

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
                rocket = player as Rocket;
            }
            simulationResetEvent.Set();
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
            simulationResetEvent.Set();
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
                // * Don't run the simulation whilst in a menu that pauses the game.
                if (ScreenManager.main.CurrentScreen.PauseWhileOpen)
                    continue;
                
                try
                {
                    RocketInfo copy_result = null;
                    if (!SandboxSettings.main.settings.infiniteFuel)
                    {
                        Rocket copy_rocket;
                        lock (simulationLock)
                        {
                            copy_rocket = rocket;
                        }
                        if (copy_rocket != null && copy_rocket.hasControl)
                        {
                            copy_result = RocketInfo.Generate(copy_rocket);
                        }
                    }
                    lock (simulationLock)
                    {
                        PostResultChanged(rocket, copy_result);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Stages Expanded - Simulation thread error: {e}");
                }
                simulationResetEvent.WaitOne(Settings.settings.SimulationFrequency);
            }
        }
    }
}