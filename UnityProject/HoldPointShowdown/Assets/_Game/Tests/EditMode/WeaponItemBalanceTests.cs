using HoldPointShowdown.Combat;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.Gameplay;
using HoldPointShowdown.Player;
using NUnit.Framework;
using UnityEngine;

namespace HoldPointShowdown.Tests
{
    public sealed class WeaponItemBalanceTests
    {
        private readonly System.Collections.Generic.List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }
            _created.Clear();
        }

        [Test]
        public void FiveWeaponTiers_StayInsidePlayableDemoEnvelope()
        {
            WeaponConfig pistol = CreateWeapon("手枪", WeaponArchetype.Pistol, ItemRarity.Common, 20f, 4f, 1, 0.8f, 12);
            WeaponConfig smg = CreateWeapon("冲锋枪", WeaponArchetype.SubmachineGun, ItemRarity.Uncommon, 13f, 10f, 1, 2.15f, 30);
            WeaponConfig rifle = CreateWeapon("步枪", WeaponArchetype.AssaultRifle, ItemRarity.Rare, 22f, 6f, 1, 0.65f, 24);
            WeaponConfig shotgun = CreateWeapon("霰弹枪", WeaponArchetype.AutoShotgun, ItemRarity.Epic, 7f, 2.5f, 8, 6.4f, 8);
            WeaponConfig golden = CreateWeapon("金色步枪", WeaponArchetype.AssaultRifle, ItemRarity.Legendary, 25f, 6.6f, 1, 0.5f, 25);

            foreach (WeaponConfig weapon in new[] { pistol, smg, rifle, shotgun, golden })
            {
                Assert.That(CombatBalance.IsWithinDemoBalanceEnvelope(weapon), Is.True,
                    $"{weapon.DisplayName}: DPS={weapon.BodyDps:0.0}, TTK={weapon.IdealArmoredTtk:0.00}");
                Assert.That(weapon.MagazineSize, Is.GreaterThanOrEqualTo(CombatBalance.CalculateShotsToEliminate(weapon)));
            }

            Assert.That(pistol.BodyDps, Is.LessThan(smg.BodyDps));
            Assert.That(smg.BodyDps, Is.LessThanOrEqualTo(rifle.BodyDps));
            Assert.That(rifle.BodyDps, Is.LessThan(shotgun.BodyDps));
            Assert.That(shotgun.BodyDps, Is.LessThan(golden.BodyDps));
            Assert.That(pistol.IdealArmoredTtk, Is.GreaterThan(shotgun.IdealArmoredTtk));
        }

        [Test]
        public void Loadout_UsesBackpackSlots_AndEnforcesTwoWeaponLimit()
        {
            GameObject player = Track(new GameObject("玩家"));
            PrototypeHitscanWeapon weapon = player.AddComponent<PrototypeHitscanWeapon>();
            Damageable damageable = player.AddComponent<Damageable>();
            WeaponLoadout loadout = player.AddComponent<WeaponLoadout>();
            PrototypeInventory inventory = player.AddComponent<PrototypeInventory>();
            GameBalanceConfig balance = Track(ScriptableObject.CreateInstance<GameBalanceConfig>());
            balance.SetDemoValues(backpackSlots: 12, weaponSlotCost: 3, maxCarriedWeapons: 2);

            WeaponConfig pistol = CreateWeapon("手枪", WeaponArchetype.Pistol, ItemRarity.Common, 20f, 4f, 1, 0.8f, 12);
            WeaponConfig rifle = CreateWeapon("步枪", WeaponArchetype.AssaultRifle, ItemRarity.Rare, 22f, 6f, 1, 0.65f, 24);
            WeaponConfig shotgun = CreateWeapon("霰弹枪", WeaponArchetype.AutoShotgun, ItemRarity.Epic, 7f, 2.5f, 8, 6.4f, 8);

            loadout.Configure(weapon, balance.MaxCarriedWeapons, pistol);
            inventory.Configure(weapon, damageable, loadout, balance);

            Assert.That(inventory.UsedSlots, Is.EqualTo(3));
            Assert.That(inventory.TryAddWeapon(rifle), Is.True);
            Assert.That(inventory.UsedSlots, Is.EqualTo(6));
            Assert.That(inventory.TryAddWeapon(shotgun), Is.False);
            Assert.That(loadout.WeaponCount, Is.EqualTo(2));
        }

        [Test]
        public void DuplicateWeaponPickup_AddsAmmoWithoutConsumingAnotherWeaponSlot()
        {
            GameObject player = Track(new GameObject("玩家"));
            PrototypeHitscanWeapon weapon = player.AddComponent<PrototypeHitscanWeapon>();
            WeaponLoadout loadout = player.AddComponent<WeaponLoadout>();
            WeaponConfig pistol = CreateWeapon("手枪", WeaponArchetype.Pistol, ItemRarity.Common, 20f, 4f, 1, 0.8f, 12);
            loadout.Configure(weapon, 2, pistol);
            int reserveBefore = weapon.ReserveAmmo;

            Assert.That(loadout.TryAddWeapon(pistol), Is.True);
            Assert.That(loadout.WeaponCount, Is.EqualTo(1));
            Assert.That(weapon.ReserveAmmo, Is.EqualTo(reserveBefore + pistol.AmmoPerPickup));
        }

        [Test]
        public void SharedChest_StoresAndRestoresSuppliesAndActiveWeapon()
        {
            GameObject player = Track(new GameObject("仓库测试玩家"));
            PrototypeHitscanWeapon weapon = player.AddComponent<PrototypeHitscanWeapon>();
            Damageable damageable = player.AddComponent<Damageable>();
            TeamMember team = player.AddComponent<TeamMember>();
            team.Configure(TeamId.Blue);
            WeaponLoadout loadout = player.AddComponent<WeaponLoadout>();
            PrototypeInventory inventory = player.AddComponent<PrototypeInventory>();
            GameBalanceConfig balance = Track(ScriptableObject.CreateInstance<GameBalanceConfig>());
            balance.SetDemoValues();
            WeaponConfig pistol = CreateWeapon("仓库手枪", WeaponArchetype.Pistol, ItemRarity.Common, 20f, 4f, 1, 0f, 12);
            WeaponConfig rifle = CreateWeapon("仓库步枪", WeaponArchetype.AssaultRifle, ItemRarity.Rare, 22f, 6f, 1, 0f, 24);
            loadout.Configure(weapon, 2, pistol);
            inventory.Configure(weapon, damageable, loadout, balance);
            Assert.That(inventory.TryAddWeapon(rifle), Is.True);
            Assert.That(inventory.TryAdd(SupplyKind.AmmoPack), Is.True);

            GameObject chestObject = Track(new GameObject("蓝方共享仓库"));
            SharedSupplyChest chest = chestObject.AddComponent<SharedSupplyChest>();
            chest.Configure(TeamId.Blue, player.transform, team, inventory);

            int ammoBefore = chest.AmmoPacks;
            Assert.That(chest.TryDepositSupply(SupplyKind.AmmoPack), Is.True);
            Assert.That(chest.AmmoPacks, Is.EqualTo(ammoBefore + 1));
            Assert.That(chest.TryDepositActiveWeapon(), Is.True);
            Assert.That(chest.StoredWeaponCount, Is.EqualTo(1));
            Assert.That(loadout.WeaponCount, Is.EqualTo(1));
            Assert.That(chest.TryWithdrawSupply(SupplyKind.AmmoPack), Is.True);
            Assert.That(chest.TryWithdrawWeapon(), Is.True);
            Assert.That(chest.StoredWeaponCount, Is.EqualTo(0));
            Assert.That(loadout.WeaponCount, Is.EqualTo(2));
            Assert.That(loadout.ActiveWeapon, Is.SameAs(rifle));
        }

        [Test]
        public void DroppingActiveWeapon_KeepsFallbackWeapon_AndCreatesCollectableWorldItem()
        {
            GameObject player = Track(new GameObject("玩家"));
            PrototypeHitscanWeapon weapon = player.AddComponent<PrototypeHitscanWeapon>();
            WeaponLoadout loadout = player.AddComponent<WeaponLoadout>();
            WeaponConfig pistol = CreateWeapon("手枪", WeaponArchetype.Pistol, ItemRarity.Common, 20f, 4f, 1, 0.8f, 12);
            WeaponConfig rifle = CreateWeapon("步枪", WeaponArchetype.AssaultRifle, ItemRarity.Rare, 22f, 6f, 1, 0.65f, 24);
            rifle.SetBalancedValues(
                rifle.ItemId, rifle.DisplayName, rifle.Archetype, rifle.Rarity, rifle.AmmoKind,
                rifle.Damage, rifle.ShotsPerSecond, rifle.Pellets, rifle.SpreadDegrees,
                rifle.MagazineSize, rifle.StartingReserveAmmo, rifle.ReloadSeconds,
                rifle.AmmoPerPickup, rifle.Range, new GameObject("测试武器模型"), Color.white);
            Track(rifle.ModelPrefab);

            loadout.Configure(weapon, 2, pistol);
            Assert.That(loadout.TryAddWeapon(rifle), Is.True);
            Assert.That(loadout.ActiveWeapon, Is.SameAs(rifle));

            Assert.That(loadout.TryDropActive(), Is.True);
            Assert.That(loadout.WeaponCount, Is.EqualTo(1));
            Assert.That(loadout.ActiveWeapon, Is.SameAs(pistol));
            GameObject dropped = GameObject.Find("掉落 · 步枪");
            Assert.That(dropped, Is.Not.Null);
            Track(dropped);
        }

        private WeaponConfig CreateWeapon(
            string name,
            WeaponArchetype archetype,
            ItemRarity rarity,
            float damage,
            float shotsPerSecond,
            int pellets,
            float spread,
            int magazine)
        {
            WeaponConfig config = Track(ScriptableObject.CreateInstance<WeaponConfig>());
            config.SetBalancedValues(
                $"test.{name}", name, archetype, rarity, AmmoKind.Light,
                damage, shotsPerSecond, pellets, spread, magazine, magazine * 3,
                2f, 30, 80f, null, Color.white);
            return config;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
