using HoldPointShowdown.Combat;
using HoldPointShowdown.Gameplay;
using HoldPointShowdown.Player;
using HoldPointShowdown.Presentation;
using HoldPointShowdown.UI;
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
        public void ApplyDamage_ArmorAbsorbsQuarterAndHealthStillShowsThreeQuarters()
        {
            Damageable damageable = _gameObject.AddComponent<Damageable>();
            damageable.Configure(100f, 50f, false, 50f);
            float reportedDamage = 0f;
            damageable.Damaged += (_, amount, _, _) => reportedDamage = amount;

            damageable.ApplyDamage(60f, Vector3.zero, null);

            Assert.That(damageable.CurrentArmor, Is.EqualTo(35f));
            Assert.That(damageable.CurrentHealth, Is.EqualTo(55f));
            Assert.That(reportedDamage, Is.EqualTo(45f));
        }

        [Test]
        public void RestoreHealthAndArmor_ClampAtConfiguredMaximums()
        {
            Damageable damageable = _gameObject.AddComponent<Damageable>();
            damageable.Configure(100f, 40f, false, 40f);
            damageable.ApplyDamage(70f, Vector3.zero, null);

            float armorRestored = damageable.RestoreArmor(100f);
            float healthRestored = damageable.RestoreHealth(100f);

            Assert.That(armorRestored, Is.EqualTo(17.5f));
            Assert.That(healthRestored, Is.EqualTo(52.5f));
            Assert.That(damageable.CurrentArmor, Is.EqualTo(40f));
            Assert.That(damageable.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void SpatialAudio_UsesEightReusableThreeDimensionalSources()
        {
            AudioFeedback feedback = _gameObject.AddComponent<AudioFeedback>();
            feedback.Initialize();

            Assert.That(feedback.SourceCount, Is.EqualTo(8));
            AudioSource[] sources = _gameObject.GetComponentsInChildren<AudioSource>(true);
            Assert.That(sources.Length, Is.EqualTo(8));
            foreach (AudioSource source in sources)
            {
                Assert.That(source.spatialBlend, Is.EqualTo(1f));
                Assert.That(source.maxDistance, Is.EqualTo(60f));
            }
        }

        [Test]
        public void DamageIndicator_MapsWorldDirectionRelativeToCameraYaw()
        {
            Vector3 origin = Vector3.zero;
            Assert.That(DamageIndicator.AngleToDirection(
                DamageIndicator.CalculateRelativeAngle(0f, origin, Vector3.forward)), Is.EqualTo(0));
            Assert.That(DamageIndicator.AngleToDirection(
                DamageIndicator.CalculateRelativeAngle(0f, origin, Vector3.right)), Is.EqualTo(1));
            Assert.That(DamageIndicator.AngleToDirection(
                DamageIndicator.CalculateRelativeAngle(0f, origin, Vector3.back)), Is.EqualTo(2));
            Assert.That(DamageIndicator.AngleToDirection(
                DamageIndicator.CalculateRelativeAngle(0f, origin, Vector3.left)), Is.EqualTo(3));
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
