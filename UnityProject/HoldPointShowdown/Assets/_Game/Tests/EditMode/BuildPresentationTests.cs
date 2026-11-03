using NUnit.Framework;
using UnityEditor;
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
    }
}
