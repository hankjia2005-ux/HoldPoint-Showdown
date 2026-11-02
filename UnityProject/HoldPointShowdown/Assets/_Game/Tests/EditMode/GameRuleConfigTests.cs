using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
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
    }
}
