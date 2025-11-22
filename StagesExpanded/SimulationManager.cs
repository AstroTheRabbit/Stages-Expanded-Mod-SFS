using System;
using System.Threading;
using UnityEngine;
using SFS.Input;
using SFS.World;
using ModLoader.Helpers;

namespace StagesExpanded.Simulation
{
    public static class SimulationManager
    {
        private static SimulationInput simulationInput = null;
        private static Thread simulationThread = null;
        private static bool simulationRunning = false;
        private static readonly object simulationLock = new object();
        private static readonly AutoResetEvent simulationResetEvent = new AutoResetEvent(false);
        private static SynchronizationContext unityContext = null;

        public static event Action<SimulationInput, SimulationOutput> OnResultChanged;

        public static void Init()
        {
            unityContext = SynchronizationContext.Current;

            SceneHelper.OnBuildSceneLoaded += () =>
            {
                lock (simulationLock)
                {
                    simulationInput = new BuildInput();
                }
                StartThread();
            };
            SceneHelper.OnBuildSceneUnloaded += () =>
            {
                StopThread();
            };
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
                if (player is Rocket rocket)
                    simulationInput = new WorldInput(rocket);
                else
                    simulationInput = null;
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

        private static void PostResultChanged(SimulationInput input, SimulationOutput info)
        {
            unityContext.Post(_ => OnResultChanged(input, info), null);
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
                    SimulationInput copy_input;
                    lock (simulationLock)
                    {
                        copy_input = simulationInput;
                    }

                    SimulationOutput copy_output = null;
                    if (copy_input != null && copy_input.RunSimulation())
                        copy_output = SimulationOutput.Generate(copy_input);

                    lock (simulationLock)
                    {
                        PostResultChanged(copy_input, copy_output);
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