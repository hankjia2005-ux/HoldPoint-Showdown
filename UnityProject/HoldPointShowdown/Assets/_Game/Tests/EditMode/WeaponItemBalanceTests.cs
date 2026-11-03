using HoldPointShowdown.Combat;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.Gameplay;
using HoldPointShowdown.Player;
using NUnit.Framework;
using UnityEditor;
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
            WeaponConfig pistol = CreateWeapon("手枪", WeaponArchetype.Pistol, ItemRarity.Common, 24f, 3f, 1, 0.8f, 12, WeaponFireMode.SemiAutomatic);
            WeaponConfig smg = CreateWeapon("冲锋枪", WeaponArchetype.SubmachineGun, ItemRarity.Uncommon, 12f, 8f, 1, 1.6f, 30, WeaponFireMode.Automatic);
            WeaponConfig rifle = CreateWeapon("步枪", WeaponArchetype.AssaultRifle, ItemRarity.Rare, 22f, 4.2f, 1, 0.5f, 20, WeaponFireMode.SemiAutomatic);
            WeaponConfig shotgun = CreateWeapon("霰弹枪", WeaponArchetype.AutoShotgun, ItemRarity.Epic, 11f, 1.3f, 8, 4f, 6, WeaponFireMode.SemiAutomatic);
            WeaponConfig golden = CreateWeapon("金色步枪", WeaponArchetype.AssaultRifle, ItemRarity.Legendary, 24f, 4.8f, 1, 0.45f, 20, WeaponFireMode.Automatic);

            foreach (WeaponConfig weapon in new[] { pistol, smg, rifle, shotgun, golden })
            {
                Assert.That(CombatBalance.IsWithinDemoBalanceEnvelope(weapon), Is.True,
                    $"{weapon.DisplayName}: DPS={weapon.BodyDps:0.0}, TTK={weapon.IdealArmoredTtk:0.00}");
                Assert.That(weapon.MagazineSize, Is.GreaterThanOrEqualTo(CombatBalance.CalculateShotsToEliminate(weapon)));
            }

            Assert.That(pistol.FireMode, Is.EqualTo(WeaponFireMode.SemiAutomatic));
            Assert.That(smg.FireMode, Is.EqualTo(WeaponFireMode.Automatic));
            Assert.That(rifle.FireMode, Is.EqualTo(WeaponFireMode.SemiAutomatic));
            Assert.That(shotgun.IdealArmoredTtk, Is.LessThan(pistol.IdealArmoredTtk));
            Assert.That(golden.IdealArmoredTtk, Is.LessThan(rifle.IdealArmoredTtk));
        }

        [TestCase("Assets/_Game/Art/Items/Prefabs/Weapon_Pistol.prefab")]
        [TestCase("Assets/_Game/Art/Items/Prefabs/Weapon_SMG.prefab")]
        [TestCase("Assets/_Game/Art/Items/Prefabs/Weapon_Rifle.prefab")]
        [TestCase("Assets/_Game/Art/Items/Prefabs/Weapon_AutoShotgun.prefab")]
        [TestCase("Assets/_Game/Art/Items/Prefabs/Weapon_GoldenRifle.prefab")]
        public void WeaponArt_UsesAtMostFourMaterialBatches(string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            Assert.That(prefab, Is.Not.Null, $"武器美术资产不存在：{assetPath}");
            int rendererCount = prefab.GetComponentsInChildren<Renderer>(true).Length;
            Assert.That(rendererCount, Is.GreaterThan(0));
            Assert.That(rendererCount, Is.LessThanOrEqualTo(4),
                $"{prefab.name} 应按材质合并，避免一把枪产生几十次绘制");
            Assert.That(prefab.transform.Find("枪口"), Is.Not.Null,
                $"{prefab.name} 必须保留枪口挂点，供火焰、音频和弹道共用");
        }

        [Test]
        public void WeaponArt_RemainsMobileFriendly()
        {
            string[] assetPaths =
            {
                "Assets/_Game/Art/Items/Prefabs/Weapon_Pistol.prefab",
                "Assets/_Game/Art/Items/Prefabs/Weapon_SMG.prefab",
                "Assets/_Game/Art/Items/Prefabs/Weapon_Rifle.prefab",
                "Assets/_Game/Art/Items/Prefabs/Weapon_AutoShotgun.prefab",
                "Assets/_Game/Art/Items/Prefabs/Weapon_GoldenRifle.prefab"
            };

            foreach (string assetPath in assetPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                int rendererCount = prefab.GetComponentsInChildren<Renderer>(true).Length;
                Assert.That(rendererCount, Is.LessThanOrEqualTo(4),
                    $"{prefab.name} 的独立网格过多，会增加移动端批次和维护成本");
            }
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

            Assert.That(inventory.UsedSlots, Is.EqualTo(2));
            Assert.That(inventory.TryAddWeapon(rifle), Is.True);
            Assert.That(inventory.UsedSlots, Is.EqualTo(5));
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
            int magazine,
            WeaponFireMode fireMode = WeaponFireMode.SemiAutomatic)
        {
            WeaponConfig config = Track(ScriptableObject.CreateInstance<WeaponConfig>());
            config.SetBalancedValues(
                $"test.{name}", name, archetype, rarity, AmmoKind.Light,
                damage, shotsPerSecond, pellets, spread, magazine, magazine * 3,
                2f, magazine * 2, 80f, null, Color.white, fireMode,
                reserveAmmoCap: magazine * 5);
            return config;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
