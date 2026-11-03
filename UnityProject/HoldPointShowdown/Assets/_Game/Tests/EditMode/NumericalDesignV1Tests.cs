using HoldPointShowdown.Data;
using NUnit.Framework;
using UnityEditor;

namespace HoldPointShowdown.Tests
{
    public sealed class NumericalDesignV1Tests
    {
        [Test]
        public void GlobalCombatConstants_MatchApprovedSpreadsheet()
        {
            Assert.That(CombatBalance.StandardHealth, Is.EqualTo(100f));
            Assert.That(CombatBalance.StandardArmor, Is.EqualTo(40f));
            Assert.That(CombatBalance.StandardEffectiveHealth, Is.EqualTo(140f));
            Assert.That(CombatBalance.HeadMultiplier, Is.EqualTo(1.5f));
            Assert.That(CombatBalance.LimbMultiplier, Is.EqualTo(0.8f));
        }

        [TestCase("Assets/_Game/Data/PrototypePistol.asset", 24f, 3f, 1, 12, 24, 48, 1.6667f)]
        [TestCase("Assets/_Game/Data/Weapons/GreenSmg.asset", 12f, 8f, 1, 30, 60, 90, 1.375f)]
        [TestCase("Assets/_Game/Data/Weapons/BlueRifle.asset", 22f, 4.2f, 1, 20, 40, 60, 1.4286f)]
        [TestCase("Assets/_Game/Data/Weapons/PurpleAutoShotgun.asset", 11f, 1.3f, 8, 6, 12, 24, 0.7692f)]
        public void WeaponAssets_MatchApprovedSpreadsheet(
            string path,
            float damage,
            float rate,
            int pellets,
            int magazine,
            int reserve,
            int reserveCap,
            float expectedTtk)
        {
            WeaponConfig weapon = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);

            Assert.That(weapon, Is.Not.Null);
            Assert.That(weapon.Damage, Is.EqualTo(damage));
            Assert.That(weapon.ShotsPerSecond, Is.EqualTo(rate));
            Assert.That(weapon.Pellets, Is.EqualTo(pellets));
            Assert.That(weapon.MagazineSize, Is.EqualTo(magazine));
            Assert.That(weapon.StartingReserveAmmo, Is.EqualTo(reserve));
            Assert.That(weapon.ReserveAmmoCap, Is.EqualTo(reserveCap));
            Assert.That(weapon.IdealArmoredTtk, Is.EqualTo(expectedTtk).Within(0.002f));
            Assert.That(weapon.EvaluateFalloff(weapon.Range + 100f), Is.EqualTo(0.35f).Within(0.001f));
        }

        [Test]
        public void BalanceAsset_MakesSharedChestAndBackpackMeaningful()
        {
            GameBalanceConfig balance = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(
                "Assets/_Game/Data/GameBalanceConfig.asset");

            Assert.That(balance, Is.Not.Null);
            Assert.That(balance.PlayerMaxArmor, Is.EqualTo(40f));
            Assert.That(balance.ArmorDamageRatio, Is.EqualTo(1f));
            Assert.That(balance.BackpackSlots, Is.EqualTo(12));
            Assert.That(balance.WeaponSlotCost, Is.EqualTo(3));
            Assert.That(balance.PistolSlotCost, Is.EqualTo(2));
            Assert.That(balance.ArmorPlateRestore, Is.EqualTo(20f));
            Assert.That(balance.ChestCapAmmo, Is.EqualTo(8));
            Assert.That(balance.ChestCapMedkit, Is.EqualTo(4));
            Assert.That(balance.ChestCapArmor, Is.EqualTo(4));
            Assert.That(balance.ChestCapWeapon, Is.EqualTo(4));
        }

        [Test]
        public void LootBudget_ReleasesExactlyOneMatchBudget()
        {
            LootBudgetConfig loot = AssetDatabase.LoadAssetAtPath<LootBudgetConfig>(
                "Assets/_Game/Data/LootBudgetConfig.asset");
            GameRuleConfig rules = AssetDatabase.LoadAssetAtPath<GameRuleConfig>(
                "Assets/_Game/Data/GameRuleConfig.asset");

            Assert.That(loot, Is.Not.Null);
            Assert.That(rules, Is.Not.Null);
            Assert.That(loot.InitialShare + loot.PhaseOneShare + loot.PhaseTwoShare + loot.PhaseThreeShare,
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(rules.ActiveTeamSize, Is.EqualTo(3));
            Assert.That(rules.LootBudget, Is.EqualTo(43));
            Assert.That(rules.LootPointCount, Is.EqualTo(29));
        }

        [TestCase("Assets/_Game/Data/AIProfile_Easy.asset", 0.35f, 35f, 0f)]
        [TestCase("Assets/_Game/Data/AIProfile_Normal.asset", 0.55f, 55f, 0.2f)]
        [TestCase("Assets/_Game/Data/AIProfile_Hard.asset", 0.75f, 75f, 0.4f)]
        public void AiProfiles_UseFairAccuracyInsteadOfGuaranteedHits(
            string path,
            float hitChance,
            float detectRadius,
            float raidChance)
        {
            AIProfile profile = AssetDatabase.LoadAssetAtPath<AIProfile>(path);

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.HitChanceAt10m, Is.EqualTo(hitChance));
            Assert.That(profile.DetectRadius, Is.EqualTo(detectRadius));
            Assert.That(profile.RaidBaseChance, Is.EqualTo(raidChance));
            Assert.That(profile.HitChanceAt10m, Is.LessThan(1f));
        }
    }
}
