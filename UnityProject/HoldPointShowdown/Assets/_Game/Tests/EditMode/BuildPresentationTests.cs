using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace HoldPointShowdown.Tests
{
    public sealed class BuildPresentationTests
    {
        [Test]
        public void WindowsBuild_SupportsFullscreenAndResizableWindow()
        {
            Assert.That(PlayerSettings.runInBackground, Is.True);
            Assert.That(PlayerSettings.resizableWindow, Is.True);
            Assert.That(PlayerSettings.fullScreenMode, Is.EqualTo(FullScreenMode.FullScreenWindow));
            Assert.That(PlayerSettings.defaultScreenWidth, Is.GreaterThanOrEqualTo(1600));
            Assert.That(PlayerSettings.defaultScreenHeight, Is.GreaterThanOrEqualTo(900));
        }

        [Test]
        public void AndroidRelease_UsesExplicitProductionBaseline()
        {
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), Is.EqualTo("com.hankjia.holdpoint"));
            // Unity 6000.6 clamps legacy API 24 to its supported Android floor (API 26).
            Assert.That((int)PlayerSettings.Android.minSdkVersion, Is.EqualTo(26));
            Assert.That((int)PlayerSettings.Android.targetSdkVersion, Is.EqualTo(34));
            Assert.That(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android), Is.EqualTo(ScriptingImplementation.IL2CPP));
            Assert.That(PlayerSettings.Android.minifyRelease, Is.True);
        }

        [Test]
        public void Build_StartsAtClickableMainMenu()
        {
            Assert.That(EditorBuildSettings.scenes.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(EditorBuildSettings.scenes[0].enabled, Is.True);
            Assert.That(EditorBuildSettings.scenes[0].path, Does.EndWith("MainMenu.unity"));
        }
    }
}
