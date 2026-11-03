using System;
using System.IO;
using System.Linq;
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
                scenes = GetEnabledScenes(),
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CleanBuildCache
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Windows prototype build: {summary.result}, {summary.totalSize} bytes, {summary.totalTime}.");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows prototype build failed: {summary.result}");
            }
        }

        [MenuItem("HoldPoint Showdown/Build/Android Release APK")]
        public static void BuildAndroidRelease()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new InvalidOperationException(
                    "当前 Unity Editor 未安装 Android Build Support、SDK/NDK 或 OpenJDK。请先按 Docs/Development/ANDROID_RELEASE_BUILD.md 安装模块。");
            }

            string keystorePath = Environment.GetEnvironmentVariable("HPS_KEYSTORE_PATH");
            string keystorePassword = Environment.GetEnvironmentVariable("HPS_KEYSTORE_PASSWORD");
            string alias = Environment.GetEnvironmentVariable("HPS_KEY_ALIAS");
            string aliasPassword = Environment.GetEnvironmentVariable("HPS_KEY_PASSWORD");
            if (string.IsNullOrWhiteSpace(keystorePath) || !File.Exists(keystorePath)
                || string.IsNullOrWhiteSpace(keystorePassword)
                || string.IsNullOrWhiteSpace(alias)
                || string.IsNullOrWhiteSpace(aliasPassword))
            {
                throw new InvalidOperationException(
                    "Release 签名未配置。请在本机设置 HPS_KEYSTORE_PATH、HPS_KEYSTORE_PASSWORD、HPS_KEY_ALIAS、HPS_KEY_PASSWORD；不要把密钥或密码提交到 Git。");
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(keystorePath);
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = aliasPassword;

            string repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            string outputDirectory = Path.Combine(repositoryRoot, "Builds", "Android");
            string outputPath = Path.Combine(outputDirectory, "HoldPointShowdown-release.apk");
            Directory.CreateDirectory(outputDirectory);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.CleanBuildCache
            });
            BuildSummary summary = report.summary;
            Debug.Log($"Android release build: {summary.result}, {summary.totalSize} bytes, {summary.totalTime}.");
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Android release build failed: {summary.result}");
            }
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }
    }
}
