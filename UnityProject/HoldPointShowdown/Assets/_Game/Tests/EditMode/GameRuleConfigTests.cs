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
        [TestCase(119.99f, MatchPhase.Scavenge)]
        [TestCase(120f, MatchPhase.Raid)]
        [TestCase(239.99f, MatchPhase.Raid)]
        [TestCase(240f, MatchPhase.FinalBattle)]
        [TestCase(539.99f, MatchPhase.FinalBattle)]
        [TestCase(540f, MatchPhase.Complete)]
        public void ResolvePhase_UsesExpectedBoundaries(float elapsedSeconds, MatchPhase expected)
        {
            Assert.That(_rules.ResolvePhase(elapsedSeconds), Is.EqualTo(expected));
        }

        [Test]
        public void RespawnRules_UseEachPhaseDelay_AndTwoFinalLives()
        {
            _rules.SetDemoValues(180f, 300f, 720f, 0.5f, 4f, 5f, 2);

            Assert.That(_rules.GetRespawnDelay(MatchPhase.Scavenge), Is.EqualTo(0.5f));
            Assert.That(_rules.GetRespawnDelay(MatchPhase.Raid), Is.EqualTo(4f));
            Assert.That(_rules.GetRespawnDelay(MatchPhase.FinalBattle), Is.EqualTo(5f));
            Assert.That(_rules.FinalBattleLives, Is.EqualTo(2));
        }

        [TestCase(2, 90f, 210f, 480f, 19, 29)]
        [TestCase(3, 120f, 240f, 540f, 29, 43)]
        [TestCase(4, 150f, 270f, 630f, 38, 58)]
        [TestCase(5, 180f, 300f, 720f, 48, 72)]
        public void MatchPresets_ScaleTimelineAndLootWithTeamSize(
            int teamSize,
            float scavenge,
            float raid,
            float total,
            int points,
            int budget)
        {
            _rules.SetRecommendedPresets(teamSize);

            Assert.That(_rules.ScavengeEndsAt, Is.EqualTo(scavenge));
            Assert.That(_rules.RaidEndsAt, Is.EqualTo(raid));
            Assert.That(_rules.MatchEndsAt, Is.EqualTo(total));
            Assert.That(_rules.LootPointCount, Is.EqualTo(points));
            Assert.That(_rules.LootBudget, Is.EqualTo(budget));
        }

        [Test]
        public void Controls_KeepSForBackward_AndUseLeftControlForCrouch()
        {
            Assert.That(ThirdPersonPlayerController.CrouchKeyboardBinding, Is.EqualTo("<Keyboard>/leftCtrl"));
            Assert.That(ThirdPersonPlayerController.SprintKeyboardBinding, Is.EqualTo("<Keyboard>/leftShift"));
            Assert.That(ThirdPersonCameraRig.PerspectiveKeyboardBinding, Is.EqualTo("<Keyboard>/v"));
            Assert.That(ThirdPersonPlayerController.BackwardDoubleTapWindow, Is.InRange(0.25f, 0.4f));
        }
    }
}
