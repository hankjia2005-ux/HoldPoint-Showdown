using System.Collections;
using HoldPointShowdown.AI;
using HoldPointShowdown.Combat;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.GameFlow;
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
        public IEnumerator CameraRig_AcceptsMouseAim_ZoomsOut_AndControllerCrouches()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            ThirdPersonCameraRig cameraRig = Object.FindAnyObjectByType<ThirdPersonCameraRig>();
            ThirdPersonPlayerController controller = Object.FindAnyObjectByType<ThirdPersonPlayerController>();
            CharacterController characterController = controller.GetComponent<CharacterController>();
            foreach (PrototypeEnemyAI ai in Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude))
            {
                ai.enabled = false;
            }

            Assert.That(cameraRig, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Vector2 lookBefore = cameraRig.LookAngles;
            cameraRig.ApplyLookDelta(new Vector2(50f, -20f), true);
            Assert.That(cameraRig.LookAngles.x, Is.GreaterThan(lookBefore.x));
            Assert.That(cameraRig.LookAngles.y, Is.GreaterThan(lookBefore.y));

            float distanceBefore = cameraRig.CurrentDistance;
            cameraRig.SetZoomDistance(12f);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(cameraRig.CurrentDistance, Is.GreaterThan(distanceBefore));
            Assert.That(cameraRig.TargetDistance, Is.EqualTo(12f).Within(0.01f));

            float standingHeight = characterController.height;
            controller.SetCrouching(true);
            Assert.That(controller.IsCrouching, Is.True);
            Assert.That(characterController.height, Is.LessThan(standingHeight));
            controller.SetCrouching(false);
            Assert.That(controller.IsCrouching, Is.False);
            Assert.That(characterController.height, Is.EqualTo(standingHeight).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator PhaseOneDeath_DropsLoot_AndRespawnsAtBlueBaseWithStarterOnly()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            MatchPhaseController match = Object.FindAnyObjectByType<MatchPhaseController>();
            CombatantLifecycle player = match.GetHumanPlayer();
            PrototypeInventory inventory = Object.FindAnyObjectByType<PrototypeInventory>();
            WeaponLoadout loadout = inventory.GetComponent<WeaponLoadout>();
            PrototypeHitscanWeapon weapon = inventory.GetComponent<PrototypeHitscanWeapon>();
            Damageable damageable = inventory.GetComponent<Damageable>();
            CharacterController controller = inventory.GetComponent<CharacterController>();
            Vector3 spawnPosition = player.SpawnPosition;

            foreach (PrototypeEnemyAI bot in Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude))
            {
                bot.enabled = false;
            }

            match.Rules.SetDemoValues(180f, 300f, 720f, 0.1f, 5f, 5f, 2);
            WorldItemPickup smgPickup = null;
            foreach (WorldItemPickup pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Exclude))
            {
                if (pickup.Weapon != null && pickup.Weapon.Archetype == WeaponArchetype.SubmachineGun)
                {
                    smgPickup = pickup;
                    break;
                }
            }

            Assert.That(smgPickup, Is.Not.Null);
            Assert.That(smgPickup.TryCollect(inventory), Is.True);
            Assert.That(inventory.TryAdd(SupplyKind.AmmoPack), Is.True);
            Assert.That(inventory.TryAdd(SupplyKind.Medkit), Is.True);
            int pickupsBeforeDeath = Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Exclude).Length;

            controller.enabled = false;
            inventory.transform.position = new Vector3(8f, 0.8f, 0f);
            controller.enabled = true;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(damageable.IsInvulnerable, Is.False, "离开基地后必须可以受到伤害");

            damageable.ApplyDamage(1000f, inventory.transform.position, null);

            Assert.That(damageable.IsAlive, Is.False);
            Assert.That(loadout.WeaponCount, Is.EqualTo(1), "死亡后只能保留绑定的初始手枪");
            Assert.That(inventory.AmmoPacks, Is.EqualTo(0));
            Assert.That(inventory.Medkits, Is.EqualTo(0));
            Assert.That(
                Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Exclude).Length,
                Is.GreaterThanOrEqualTo(pickupsBeforeDeath + 3),
                "获得的武器、弹药包和医疗包都应掉在死亡地点");

            yield return new WaitForSeconds(0.2f);
            yield return null;

            Assert.That(damageable.IsAlive, Is.True);
            Assert.That(damageable.CurrentHealth, Is.EqualTo(damageable.MaxHealth));
            Assert.That(Vector2.Distance(
                new Vector2(inventory.transform.position.x, inventory.transform.position.z),
                new Vector2(spawnPosition.x, spawnPosition.z)), Is.LessThan(0.15f));
            Assert.That(loadout.ActiveWeapon.Archetype, Is.EqualTo(WeaponArchetype.Pistol));
            Assert.That(weapon.MagazineAmmo, Is.EqualTo(12));
            Assert.That(weapon.ReserveAmmo, Is.EqualTo(30));
            Assert.That(damageable.CurrentArmor, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator PhaseOneBaseProtection_BlocksDamage_AndEjectsIntruders()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return new WaitForFixedUpdate();

            MatchPhaseController match = Object.FindAnyObjectByType<MatchPhaseController>();
            CombatantLifecycle player = match.GetHumanPlayer();
            BaseProtectionZone[] zones = Object.FindObjectsByType<BaseProtectionZone>(FindObjectsInactive.Exclude);
            Assert.That(zones.Length, Is.EqualTo(2));
            Assert.That(zones[0].IsProtectionActive, Is.True);
            Assert.That(zones[1].IsProtectionActive, Is.True);
            Assert.That(player.Damageable.IsInvulnerable, Is.True);

            player.Damageable.ApplyDamage(50f, player.transform.position, null);
            Assert.That(player.Damageable.CurrentHealth, Is.EqualTo(100f));

            CombatantLifecycle intruder = null;
            foreach (CombatantLifecycle combatant in match.Combatants)
            {
                PrototypeEnemyAI ai = combatant.GetComponent<PrototypeEnemyAI>();
                if (ai != null)
                {
                    ai.enabled = false;
                }

                if (intruder == null && combatant.Team == TeamId.Red)
                {
                    intruder = combatant;
                }
            }

            Assert.That(intruder, Is.Not.Null);
            CharacterController intruderController = intruder.GetComponent<CharacterController>();
            intruderController.enabled = false;
            intruder.transform.position = new Vector3(0f, 0.8f, -34f);
            intruderController.enabled = true;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(intruder.transform.position.z, Is.GreaterThan(-30f), "闯入敌方基地的角色应被推出保护区");
        }

        [UnityTest]
        public IEnumerator FinalBattle_TwoDeathsEliminatePlayers_AndResolveVictory()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            MatchPhaseController match = Object.FindAnyObjectByType<MatchPhaseController>();
            foreach (PrototypeEnemyAI ai in Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude))
            {
                ai.enabled = false;
            }

            match.Rules.SetDemoValues(1f, 2f, 20f, 0.05f, 0.05f, 0.05f, 2);
            match.StartMatch();
            yield return new WaitForSeconds(2.1f);

            Assert.That(match.CurrentPhase, Is.EqualTo(MatchPhase.FinalBattle));
            foreach (CombatantLifecycle combatant in match.Combatants)
            {
                Assert.That(combatant.FinalLivesRemaining, Is.EqualTo(2));
            }

            foreach (CombatantLifecycle combatant in match.Combatants)
            {
                if (combatant.Team != TeamId.Red)
                {
                    continue;
                }

                combatant.Damageable.SetInvulnerable(false);
                combatant.Damageable.ApplyDamage(999f, combatant.transform.position, null);
            }

            yield return new WaitForSeconds(0.12f);
            foreach (CombatantLifecycle combatant in match.Combatants)
            {
                if (combatant.Team == TeamId.Red)
                {
                    Assert.That(combatant.IsEliminated, Is.False);
                    Assert.That(combatant.FinalLivesRemaining, Is.EqualTo(1));
                    Assert.That(combatant.Damageable.IsAlive, Is.True);
                }
            }

            foreach (CombatantLifecycle combatant in match.Combatants)
            {
                if (combatant.Team != TeamId.Red)
                {
                    continue;
                }

                combatant.Damageable.SetInvulnerable(false);
                combatant.Damageable.ApplyDamage(999f, combatant.transform.position, null);
            }

            yield return null;
            yield return null;
            Assert.That(match.Result, Is.EqualTo(MatchResult.BlueVictory));
            Assert.That(match.IsRunning, Is.False);
            Assert.That(match.GetRemainingTeamMembers(TeamId.Red), Is.EqualTo(0));
        }
    }
}
