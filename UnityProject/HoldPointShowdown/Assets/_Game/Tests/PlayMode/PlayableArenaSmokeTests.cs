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

            PrototypeInventory inventory = Object.FindAnyObjectByType<PrototypeInventory>();
            WeaponLoadout loadout = Object.FindAnyObjectByType<WeaponLoadout>();
            PrototypeHitscanWeapon weapon = Object.FindAnyObjectByType<PrototypeHitscanWeapon>();
            PrototypeEnemyAI[] bots = Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude);
            SharedSupplyChest[] chests = Object.FindObjectsByType<SharedSupplyChest>(FindObjectsInactive.Exclude);
            WorldItemPickup[] pickups = Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Exclude);

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

            Assert.That(weapon.TryFire(), Is.True);
            int droppedMagazine = weapon.MagazineAmmo;
            int droppedReserve = weapon.ReserveAmmo;
            Assert.That(loadout.TryDropActive(), Is.True);
            Assert.That(loadout.WeaponCount, Is.EqualTo(1));
            Assert.That(loadout.ActiveWeapon.Archetype, Is.EqualTo(WeaponArchetype.Pistol));
            Assert.That(Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Exclude).Length,
                Is.GreaterThan(pickups.Length));

            WorldItemPickup droppedPickup = null;
            foreach (WorldItemPickup pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Exclude))
            {
                if (pickup.name.StartsWith("掉落 · "))
                {
                    droppedPickup = pickup;
                    break;
                }
            }

            Assert.That(droppedPickup, Is.Not.Null);
            Assert.That(droppedPickup.TryCollect(inventory), Is.False, "刚丢下的武器不应被自己立刻捡回");
            yield return new WaitForSeconds(0.9f);
            Assert.That(loadout.WeaponCount, Is.EqualTo(1), "玩家停在原地时，掉落武器不应自动回吸");
            Assert.That(droppedPickup.TryCollect(inventory), Is.True);
            Assert.That(loadout.WeaponCount, Is.EqualTo(2));
            Assert.That(weapon.MagazineAmmo, Is.EqualTo(droppedMagazine));
            Assert.That(weapon.ReserveAmmo, Is.EqualTo(droppedReserve));

            int ammoBefore = weapon.MagazineAmmo;
            Assert.That(weapon.TryFire(), Is.True);
            Assert.That(weapon.MagazineAmmo, Is.EqualTo(ammoBefore - 1));
        }

        [UnityTest]
        public IEnumerator PlayerDeath_RespawnsAtBlueBase()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            PrototypeInventory inventory = Object.FindAnyObjectByType<PrototypeInventory>();
            Damageable damageable = inventory.GetComponent<Damageable>();
            CharacterController controller = inventory.GetComponent<CharacterController>();
            Vector3 spawnPosition = inventory.transform.position;

            foreach (PrototypeEnemyAI bot in Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude))
            {
                bot.enabled = false;
            }

            controller.enabled = false;
            inventory.transform.position = spawnPosition + new Vector3(8f, 0f, 8f);
            controller.enabled = true;
            damageable.ApplyDamage(1000f, inventory.transform.position, null);

            Assert.That(damageable.IsAlive, Is.False);
            yield return new WaitForSeconds(2.2f);
            yield return null;

            Assert.That(damageable.IsAlive, Is.True);
            Assert.That(damageable.CurrentHealth, Is.EqualTo(damageable.MaxHealth));
            Assert.That(Vector3.Distance(inventory.transform.position, spawnPosition), Is.LessThan(0.15f));
        }
    }
}
