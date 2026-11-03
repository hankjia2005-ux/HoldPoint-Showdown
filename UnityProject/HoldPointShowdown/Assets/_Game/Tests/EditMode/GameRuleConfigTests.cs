using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.Player;
using NUnit.Framework;
using UnityEngine;

namespace HoldPointShowdown.Tests
{
    public sealed class GameRuleConfigTests
    {
        private GameRuleConfig _rules;

        [SetUp]
        public void SetUp()
        {
            _rules = ScriptableObject.CreateInstance<GameRuleConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rules);
        }

        [TestCase(0f, MatchPhase.Scavenge)]
        [TestCase(179.99f, MatchPhase.Scavenge)]
        [TestCase(180f, MatchPhase.Raid)]
        [TestCase(299.99f, MatchPhase.Raid)]
        [TestCase(300f, MatchPhase.FinalBattle)]
        [TestCase(719.99f, MatchPhase.FinalBattle)]
        [TestCase(720f, MatchPhase.Complete)]
        public void ResolvePhase_UsesExpectedBoundaries(float elapsedSeconds, MatchPhase expected)
        {
            Assert.That(_rules.ResolvePhase(elapsedSeconds), Is.EqualTo(expected));
        }

        [Test]
        public void RespawnRules_UseEachPhaseDelay_AndTwoFinalLives()
        {
            _rules.SetDemoValues(180f, 300f, 720f, 0.75f, 5f, 5f, 2);

            Assert.That(_rules.GetRespawnDelay(MatchPhase.Scavenge), Is.EqualTo(0.75f));
            Assert.That(_rules.GetRespawnDelay(MatchPhase.Raid), Is.EqualTo(5f));
            Assert.That(_rules.GetRespawnDelay(MatchPhase.FinalBattle), Is.EqualTo(5f));
            Assert.That(_rules.FinalBattleLives, Is.EqualTo(2));
        }

        [Test]
        public void Controls_KeepSForBackward_AndUseLeftControlForCrouch()
        {
            Assert.That(ThirdPersonPlayerController.CrouchKeyboardBinding, Is.EqualTo("<Keyboard>/leftCtrl"));
            Assert.That(ThirdPersonPlayerController.SprintKeyboardBinding, Is.EqualTo("<Keyboard>/leftShift"));
        }
    }
}
