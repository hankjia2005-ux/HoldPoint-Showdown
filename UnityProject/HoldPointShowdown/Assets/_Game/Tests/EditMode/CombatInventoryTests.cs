using HoldPointShowdown.Combat;
using HoldPointShowdown.Gameplay;
using HoldPointShowdown.Player;
using NUnit.Framework;
using UnityEngine;

namespace HoldPointShowdown.Tests
{
    public sealed class CombatInventoryTests
    {
        private GameObject _gameObject;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("测试对象");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void ApplyDamage_ConsumesArmorBeforeHealth_AndRaisesFeedbackEvent()
        {
            Damageable damageable = _gameObject.AddComponent<Damageable>();
            damageable.Configure(100f, 50f, false, 50f);
            float reportedDamage = 0f;
            damageable.Damaged += (_, amount, _, _) => reportedDamage = amount;

            damageable.ApplyDamage(60f, Vector3.zero, null);

            Assert.That(damageable.CurrentArmor, Is.EqualTo(0f));
            Assert.That(damageable.CurrentHealth, Is.EqualTo(90f));
            Assert.That(reportedDamage, Is.EqualTo(60f));
        }

        [Test]
        public void RestoreHealthAndArmor_ClampAtConfiguredMaximums()
        {
            Damageable damageable = _gameObject.AddComponent<Damageable>();
            damageable.Configure(100f, 40f, false, 40f);
            damageable.ApplyDamage(70f, Vector3.zero, null);

            float armorRestored = damageable.RestoreArmor(100f);
            float healthRestored = damageable.RestoreHealth(100f);

            Assert.That(armorRestored, Is.EqualTo(40f));
            Assert.That(healthRestored, Is.EqualTo(30f));
            Assert.That(damageable.CurrentArmor, Is.EqualTo(40f));
            Assert.That(damageable.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void Damageable_StartsWithoutArmor_AndHonorsBaseProtection()
        {
            Damageable damageable = _gameObject.AddComponent<Damageable>();
            damageable.Configure(100f, 50f, false, 0f);

            Assert.That(damageable.CurrentArmor, Is.EqualTo(0f));
            damageable.SetInvulnerable(true);
            damageable.ApplyDamage(999f, Vector3.zero, null);
            Assert.That(damageable.CurrentHealth, Is.EqualTo(100f));

            damageable.SetInvulnerable(false);
            damageable.ApplyDamage(25f, Vector3.zero, null);
            Assert.That(damageable.CurrentHealth, Is.EqualTo(75f));
        }

        [Test]
        public void Inventory_EnforcesCapacityAndSupportsChestStyleTransfers()
        {
            PrototypeInventory inventory = _gameObject.AddComponent<PrototypeInventory>();
            inventory.Configure(null, null, 2);

            Assert.That(inventory.TryAdd(SupplyKind.AmmoPack), Is.True);
            Assert.That(inventory.TryAdd(SupplyKind.Medkit), Is.True);
            Assert.That(inventory.TryAdd(SupplyKind.ArmorPlate), Is.False);
            Assert.That(inventory.UsedSlots, Is.EqualTo(2));

            Assert.That(inventory.TryRemove(SupplyKind.AmmoPack), Is.True);
            Assert.That(inventory.TryAdd(SupplyKind.ArmorPlate), Is.True);
            Assert.That(inventory.AmmoPacks, Is.EqualTo(0));
            Assert.That(inventory.ArmorPlates, Is.EqualTo(1));
        }
    }
}
