using System.Collections;
using HoldPointShowdown.AI;
using HoldPointShowdown.Combat;
using HoldPointShowdown.Data;
using HoldPointShowdown.Gameplay;
using HoldPointShowdown.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HoldPointShowdown.Tests
{
    public sealed class PlayableArenaSmokeTests
    {
        [UnityTest]
        public IEnumerator ArenaTown_LoadsCompletePlayableItemCombatLoop()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            PrototypeInventory inventory = Object.FindFirstObjectByType<PrototypeInventory>();
            WeaponLoadout loadout = Object.FindFirstObjectByType<WeaponLoadout>();
            PrototypeHitscanWeapon weapon = Object.FindFirstObjectByType<PrototypeHitscanWeapon>();
            PrototypeEnemyAI[] bots = Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsSortMode.None);
            SharedSupplyChest[] chests = Object.FindObjectsByType<SharedSupplyChest>(FindObjectsSortMode.None);
            WorldItemPickup[] pickups = Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None);

            Assert.That(inventory, Is.Not.Null);
            Assert.That(loadout, Is.Not.Null);
            Assert.That(weapon, Is.Not.Null);
            Assert.That(bots.Length, Is.EqualTo(3));
            Assert.That(chests.Length, Is.EqualTo(2));
            Assert.That(pickups.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(loadout.WeaponCount, Is.EqualTo(1));
            Assert.That(loadout.ActiveWeapon.Archetype, Is.EqualTo(WeaponArchetype.Pistol));
            Assert.That(inventory.UsedSlots, Is.EqualTo(3));

            WorldItemPickup smgPickup = null;
            foreach (WorldItemPickup pickup in pickups)
            {
                if (pickup.Weapon != null && pickup.Weapon.Archetype == WeaponArchetype.SubmachineGun)
                {
                    smgPickup = pickup;
                    break;
                }
            }

            Assert.That(smgPickup, Is.Not.Null);
            Assert.That(smgPickup.TryCollect(inventory), Is.True);
            Assert.That(loadout.WeaponCount, Is.EqualTo(2));
            Assert.That(loadout.ActiveWeapon.Archetype, Is.EqualTo(WeaponArchetype.SubmachineGun));
            Assert.That(inventory.UsedSlots, Is.EqualTo(6));

            Assert.That(loadout.TryDropActive(), Is.True);
            Assert.That(loadout.WeaponCount, Is.EqualTo(1));
            Assert.That(loadout.ActiveWeapon.Archetype, Is.EqualTo(WeaponArchetype.Pistol));
            Assert.That(Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None).Length,
                Is.GreaterThan(pickups.Length));

            int ammoBefore = weapon.MagazineAmmo;
            Assert.That(weapon.TryFire(), Is.True);
            Assert.That(weapon.MagazineAmmo, Is.EqualTo(ammoBefore - 1));
        }
    }
}
