using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HoldPointShowdown.Editor
{
    public static class BuildAutomation
    {
        [MenuItem("HoldPoint Showdown/Build/Windows Prototype")]
        public static void BuildWindowsPrototype()
        {
            string repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            string outputDirectory = Path.Combine(repositoryRoot, "Builds", "Windows");
            string outputPath = Path.Combine(outputDirectory, "HoldPointShowdown.exe");
            Directory.CreateDirectory(outputDirectory);

            BuildPlayerOptions options = new()
            {
                scenes = new[] { "Assets/_Game/Scenes/TestGym.unity" },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Windows prototype build: {summary.result}, {summary.totalSize} bytes, {summary.totalTime}.");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows prototype build failed: {summary.result}");
            }
        }
    }
}
