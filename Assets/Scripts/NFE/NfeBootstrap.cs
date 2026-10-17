using System;
using Unity.NetCode;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Unity.Entities;

namespace NetworkingLab.NFE
{
    /// <summary>Creates NFE worlds only for the NFE scene or an explicit -nfe launch.</summary>
    [Preserve]
    public sealed class NfeBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            UnityEngine.Application.runInBackground = true;

            string sceneName = SceneManager.GetActiveScene().name;
            bool explicitNfe = Array.Exists(Environment.GetCommandLineArgs(), arg =>
                string.Equals(arg, "-nfe", StringComparison.OrdinalIgnoreCase));

            if (!explicitNfe && !sceneName.StartsWith("NFE", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            AutoConnectPort = 0;
            bool clientOnly = Array.Exists(Environment.GetCommandLineArgs(), arg =>
                string.Equals(arg, "-client", StringComparison.OrdinalIgnoreCase));

            if (clientOnly)
            {
                CreateClientWorld("ClientWorld");
            }
            else if (UnityEngine.Application.isBatchMode)
            {
                CreateServerWorld("ServerWorld");
            }
            else
            {
                CreateServerWorld("ServerWorld");
                CreateClientWorld("ClientWorld");
            }

            // Configure tick rate for smoother Editor testing
            var tickRate = new Unity.NetCode.ClientServerTickRate
            {
                SimulationTickRate = 60,
                NetworkTickRate = 60,
                MaxSimulationStepsPerFrame = 4,
                MaxSimulationStepBatchSize = 2
            };
            tickRate.ResolveDefaults();
            foreach (var w in Unity.Entities.World.All)
            {
                if ((w.Name == "ServerWorld" || w.Name == "ClientWorld") && w.IsCreated)
                {
                    var query = w.EntityManager.CreateEntityQuery(typeof(Unity.NetCode.ClientServerTickRate));
                    if (query.CalculateEntityCount() == 0)
                    {
                        w.EntityManager.CreateSingleton(tickRate);
                    }
                    query.Dispose();
                }
            }

            return true;
        }
    }
}
