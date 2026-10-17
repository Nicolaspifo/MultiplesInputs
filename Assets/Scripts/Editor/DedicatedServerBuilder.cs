using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NetworkingLab.Editor
{
    /// <summary>Builds the Linux Dedicated Server artifacts expected by Infrastructure/Containers.</summary>
    public static class DedicatedServerBuilder
    {
        private const string NgoScene = "Assets/Scenes/NGOGameplay.unity";
        private const string NfeScene = "Assets/Scenes/NFEGameplay.unity";
        private const string NgoExecutable = "NetworkingLabNGOServer";
        private const string NfeExecutable = "NetworkingLabNFEServer";
        private const string NgoOutput = "Builds/NGO-LinuxServer";
        private const string NfeOutput = "Builds/NFE-LinuxServer";

        [MenuItem("Networking Lab/Build NGO Linux Dedicated Server")]
        public static void BuildNgo()
        {
            Build(NgoExecutable, NgoOutput, new[] { NgoScene });
        }

        [MenuItem("Networking Lab/Build NFE Linux Dedicated Server")]
        public static void BuildNfe()
        {
            Build(NfeExecutable, NfeOutput, new[] { NfeScene });
        }

        private static void Build(string executable, string outputDir, string[] scenes)
        {
            foreach (string scene in scenes)
            {
                if (!File.Exists(scene))
                {
                    throw new FileNotFoundException($"Scene not found: {scene}");
                }
            }

            Directory.CreateDirectory(outputDir);

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
            {
                throw new Exception("Could not switch to Standalone Linux64. Install Linux Build Support from Unity Hub.");
            }

            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

            string previousProductName = PlayerSettings.productName;
            string locationPathName = Path.Combine(outputDir, executable + ".x86_64");
            try
            {
                PlayerSettings.productName = executable;

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = locationPathName,
                    target = BuildTarget.StandaloneLinux64,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                if (summary.result != BuildResult.Succeeded)
                {
                    throw new Exception($"Build failed: {summary.result} ({summary.totalErrors} errors)");
                }

                Debug.Log($"[DedicatedServerBuilder] Built {locationPathName} ({summary.totalSize} bytes)");
            }
            finally
            {
                PlayerSettings.productName = previousProductName;
            }
        }
    }
}
