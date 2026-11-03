using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HoldPointShowdown.AI;
using HoldPointShowdown.Combat;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Gameplay;
using HoldPointShowdown.Player;
using HoldPointShowdown.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace HoldPointShowdown.Tests
{
    public sealed class PlayableArenaSmokeTests
    {
        [UnityTest]
        public IEnumerator RuntimeRules_AreIsolatedFromTheProjectAsset()
        {
            GameRuleConfig sourceRules = ScriptableObject.CreateInstance<GameRuleConfig>();
            sourceRules.SetDemoValues(120f, 270f, 540f);
            GameObject flowObject = new("运行时规则隔离测试");
            flowObject.SetActive(false);
            MatchPhaseController match = flowObject.AddComponent<MatchPhaseController>();
            match.Configure(sourceRules);

            Assert.That(match.Rules, Is.Not.SameAs(sourceRules),
                "运行时必须使用规则副本，测试和自定义房间不能污染项目资产");
            match.Rules.SetDemoValues(0.1f, 0.2f, 0.4f);
            Assert.That(sourceRules.ScavengeEndsAt, Is.EqualTo(120f));
            Assert.That(sourceRules.RaidEndsAt, Is.EqualTo(270f));
            Assert.That(sourceRules.MatchEndsAt, Is.EqualTo(540f));

            Object.Destroy(flowObject);
            Object.Destroy(sourceRules);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MatchTimer_TransitionsThroughAllThreePhases_InOrder()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            MatchPhaseController match = Object.FindAnyObjectByType<MatchPhaseController>();
            foreach (PrototypeEnemyAI ai in Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude))
            {
                ai.enabled = false;
            }

            match.Rules.SetDemoValues(1f, 1.35f, 1.7f, 0.05f, 0.05f, 0.05f, 2);
            match.StartMatch();
            Assert.That(match.CurrentPhase, Is.EqualTo(MatchPhase.Scavenge));

            yield return new WaitForSeconds(1.08f);
            Assert.That(match.CurrentPhase, Is.EqualTo(MatchPhase.Raid));
            yield return new WaitForSeconds(0.36f);
            Assert.That(match.CurrentPhase, Is.EqualTo(MatchPhase.FinalBattle));
            yield return new WaitForSeconds(0.36f);
            Assert.That(match.CurrentPhase, Is.EqualTo(MatchPhase.Complete));
            Assert.That(match.Result, Is.EqualTo(MatchResult.Draw));
        }

        [UnityTest]
        public IEnumerator EscapePause_AndMatchResult_KeepCursorAvailableForUi()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            PauseMenuController pause = Object.FindAnyObjectByType<PauseMenuController>();
            MatchPhaseController match = Object.FindAnyObjectByType<MatchPhaseController>();
            Assert.That(pause, Is.Not.Null);

            pause.ProcessEscape();
            bool paused = PauseMenuController.IsPaused;
            bool releasedWhilePaused = Cursor.lockState == CursorLockMode.None && Cursor.visible;
            pause.ProcessEscape();
            Assert.That(paused, Is.True, "Esc 应打开暂停菜单");
            Assert.That(releasedWhilePaused, Is.True, "Esc 暂停时必须显示并释放鼠标");
            Assert.That(PauseMenuController.IsPaused, Is.False);

            foreach (PrototypeEnemyAI ai in Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude))
            {
                ai.enabled = false;
            }
            match.Rules.SetDemoValues(1f, 1.1f, 1.25f);
            match.StartMatch();
            yield return new WaitForSeconds(1.35f);
            yield return null;

            Assert.That(match.Result, Is.EqualTo(MatchResult.Draw));
            Assert.That(PauseMenuController.IsMatchEnded, Is.True);
            Assert.That(PauseMenuController.ShouldCaptureGameplayCursor, Is.False);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(Cursor.visible, Is.True);
            pause.ProcessEscape();
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None),
                "结算界面按 Esc 后鼠标不能被玩家控制器重新锁定");
        }

        [UnityTest]
        public IEnumerator MainMenu_HasClickableChineseEntryPoints()
        {
            SceneManager.LoadScene("MainMenu");
            yield return null;
            yield return null;

            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Length, Is.EqualTo(3));
            Assert.That(Object.FindAnyObjectByType<PrototypeSceneLabel>(), Is.Null);
            Assert.That(eventSystem.currentSelectedGameObject, Is.EqualTo(menu.PlayButton.gameObject),
                "主菜单必须默认选中普通模式，保证键盘与手柄可直接确认开局。");
        }

        [UnityTest]
        public IEnumerator ArenaTown_LoadsCompletePlayableItemCombatLoop()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            MatchPhaseController match = Object.FindAnyObjectByType<MatchPhaseController>();
            CombatantLifecycle human = match.GetHumanPlayer();
            PrototypeInventory inventory = human.GetComponent<PrototypeInventory>();
            WeaponLoadout loadout = human.GetComponent<WeaponLoadout>();
            PrototypeHitscanWeapon weapon = human.GetComponent<PrototypeHitscanWeapon>();
            PrototypeEnemyAI[] bots = Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude);
            SharedSupplyChest[] chests = Object.FindObjectsByType<SharedSupplyChest>(FindObjectsInactive.Exclude);
            WorldItemPickup[] pickups = Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Exclude);

            Assert.That(inventory, Is.Not.Null);
            Assert.That(loadout, Is.Not.Null);
            Assert.That(weapon, Is.Not.Null);
            Assert.That(bots.Length, Is.EqualTo(5));
            Assert.That(chests.Length, Is.EqualTo(2));
            Assert.That(pickups.Length, Is.GreaterThanOrEqualTo(12));
            Assert.That(Object.FindObjectsByType<LootCrate>(FindObjectsInactive.Exclude).Length,
                Is.GreaterThanOrEqualTo(20));
            GameObject grassField = GameObject.Find("Grass Field");
            Assert.That(grassField, Is.Not.Null);
            Assert.That(grassField.transform.localScale.x, Is.GreaterThanOrEqualTo(90f));
            Assert.That(grassField.transform.localScale.z, Is.GreaterThanOrEqualTo(145f));
            Assert.That(GameObject.Find("West Boundary")?.GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(Camera.main.farClipPlane, Is.GreaterThanOrEqualTo(250f));
            Assert.That(loadout.WeaponCount, Is.EqualTo(1));
            Assert.That(loadout.ActiveWeapon.Archetype, Is.EqualTo(WeaponArchetype.Pistol));
            Assert.That(weapon.Config.Range, Is.GreaterThanOrEqualTo(80f),
                "初始手枪射线长度必须覆盖城镇主街的常见交战距离");
            Assert.That(inventory.UsedSlots, Is.EqualTo(2));
            foreach (PrototypeEnemyAI bot in bots)
            {
                WeaponLoadout botLoadout = bot.GetComponent<WeaponLoadout>();
                Assert.That(botLoadout, Is.Not.Null);
                Assert.That(botLoadout.WeaponCount, Is.EqualTo(1));
                Assert.That(botLoadout.ActiveWeapon.Archetype, Is.EqualTo(WeaponArchetype.Pistol),
                    "所有 AI 必须和玩家一样只带白色基础手枪出生，高品质武器只能通过搜集获得");
                bot.enabled = false;
            }

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
            Assert.That(inventory.UsedSlots, Is.EqualTo(5));

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
                if (pickup.name.StartsWith("掉落 · ")
                    && pickup.Weapon != null
                    && pickup.Weapon.Archetype == WeaponArchetype.SubmachineGun)
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
        public IEnumerator Hitscan_ReachesAcrossArena_AndDamagesOuterLeg()
        {
            GameObject cameraObject = new("远射测试相机");
            Camera aimCamera = cameraObject.AddComponent<Camera>();
            aimCamera.enabled = false;
            cameraObject.transform.SetPositionAndRotation(
                new Vector3(1000.44f, 0.14f, 0f),
                Quaternion.LookRotation(Vector3.forward));

            GameObject shooter = new("远射测试射手");
            shooter.AddComponent<TeamMember>().Configure(TeamId.Blue);
            PrototypeHitscanWeapon weapon = shooter.AddComponent<PrototypeHitscanWeapon>();
            WeaponConfig config = ScriptableObject.CreateInstance<WeaponConfig>();
            config.SetBalancedValues(
                "test.long-range-pistol", "远射测试手枪", WeaponArchetype.Pistol, ItemRarity.Common,
                AmmoKind.Light, 20f, 4f, 1, 0f, 12, 30, 1.4f, 30, 120f, null, Color.white);
            weapon.Configure(config, aimCamera);

            GameObject target = new("百米腿部测试目标");
            target.transform.position = new Vector3(1000f, 0f, 100f);
            CharacterController targetController = target.AddComponent<CharacterController>();
            targetController.height = 2.75f;
            targetController.radius = 0.48f;
            targetController.center = new Vector3(0f, 1.35f, 0f);
            Damageable targetDamageable = target.AddComponent<Damageable>();
            targetDamageable.Configure(100f, 100f, false, 0f);
            target.AddComponent<TeamMember>().Configure(TeamId.Red);
            GameObject hitboxObject = new("角色完整受击判定体");
            hitboxObject.transform.SetParent(target.transform, false);
            CapsuleCollider damageHitbox = hitboxObject.AddComponent<CapsuleCollider>();
            damageHitbox.center = new Vector3(0f, 1.35f, 0f);
            damageHitbox.height = 2.8f;
            damageHitbox.radius = 0.72f;
            damageHitbox.isTrigger = true;

            yield return null;
            Physics.SyncTransforms();
            float healthBefore = targetDamageable.CurrentHealth;
            Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool rawHit = Physics.Raycast(
                aimRay,
                out RaycastHit rawHitInfo,
                config.Range,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);

            Assert.That(rawHit, Is.True,
                $"测试射线未触发任何碰撞。origin={aimRay.origin}, direction={aimRay.direction}, bounds={targetController.bounds}");
            Assert.That(rawHitInfo.collider.GetComponentInParent<Damageable>(), Is.SameAs(targetDamageable),
                $"测试射线被其他对象遮挡：{rawHitInfo.collider.name}");

            Assert.That(weapon.TryFire(), Is.True);
            Assert.That(targetDamageable.CurrentHealth, Is.LessThan(healthBefore),
                "100 米外瞄准方块鞋外沿时也必须命中并造成伤害");

            Object.Destroy(target);
            Object.Destroy(shooter);
            Object.Destroy(cameraObject);
            Object.Destroy(config);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LootCrates_OpenAndSpawnRealContents()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            MatchPhaseController match = Object.FindAnyObjectByType<MatchPhaseController>();
            PrototypeInventory inventory = match.GetHumanPlayer().GetComponent<PrototypeInventory>();
            LootCrate[] crates = Object.FindObjectsByType<LootCrate>(FindObjectsInactive.Exclude);
            Assert.That(crates.Length, Is.GreaterThanOrEqualTo(20));
            int pickupsBefore = Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Exclude).Length;

            Assert.That(crates[0].TryOpen(inventory), Is.True);
            Assert.That(crates[0].IsOpen, Is.True);
            Assert.That(crates[0].SpawnedItemCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Exclude).Length,
                Is.GreaterThanOrEqualTo(pickupsBefore + 2));
        }

        [UnityTest]
        public IEnumerator FinalBattle_DeploysLegendaryGoldenAirdrop()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            MatchPhaseController match = Object.FindAnyObjectByType<MatchPhaseController>();
            foreach (PrototypeEnemyAI ai in Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude))
            {
                ai.enabled = false;
            }
            match.Rules.SetDemoValues(1f, 1.1f, 10f, 0.05f, 0.05f, 0.05f, 2);
            match.StartMatch();
            yield return new WaitForSeconds(1.25f);

            GoldenAirdrop airdrop = Object.FindAnyObjectByType<GoldenAirdrop>();
            Assert.That(airdrop, Is.Not.Null);
            Assert.That(airdrop.IsDeployed, Is.True);
            bool foundLegendary = false;
            foreach (WorldItemPickup pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Exclude))
            {
                foundLegendary |= pickup.Weapon != null && pickup.Weapon.Rarity == ItemRarity.Legendary;
            }
            Assert.That(foundLegendary, Is.True);
        }

        [UnityTest]
        public IEnumerator EnemyAI_AdvancesWithoutWallhack_AndAcquiresOpponentInsideDetectionRange()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            PrototypeEnemyAI[] bots = Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude);
            Assert.That(bots.Length, Is.GreaterThanOrEqualTo(3));
            Vector3[] startingPositions = new Vector3[bots.Length];
            for (int index = 0; index < bots.Length; index++)
            {
                startingPositions[index] = bots[index].transform.position;
                Assert.That(bots[index].MoveSpeed, Is.GreaterThanOrEqualTo(6f));
                Assert.That(bots[index].BurstSize, Is.InRange(2, 4));
            }

            yield return new WaitForSeconds(1.1f);

            bool anyAdvanced = false;
            foreach (PrototypeEnemyAI bot in bots)
            {
                anyAdvanced |= Vector3.Distance(bot.transform.position, startingPositions[System.Array.IndexOf(bots, bot)]) > 1.5f;
            }
            Assert.That(anyAdvanced, Is.True, "NPC 应主动离开出生区推进，而不是原地防守");

            PrototypeEnemyAI blueBot = bots.First(bot => bot.GetComponent<TeamMember>().Team == TeamId.Blue);
            PrototypeEnemyAI redBot = bots.First(bot => bot.GetComponent<TeamMember>().Team == TeamId.Red);
            CharacterController redController = redBot.GetComponent<CharacterController>();
            redController.enabled = false;
            redBot.transform.position = blueBot.transform.position + (blueBot.transform.forward * 18f);
            redController.enabled = true;

            yield return new WaitForSeconds(0.55f);

            Assert.That(blueBot.HasTarget, Is.True,
                "敌方进入普通 AI 的 55 米侦测半径后必须被锁定");
            Assert.That(blueBot.CurrentIntent,
                Is.EqualTo(EnemyAIIntent.Pursue).Or.EqualTo(EnemyAIIntent.Engage));
        }

        [UnityTest]
        public IEnumerator StrategicHoldPoint_MustBeNeutralizedBeforeEnemyCapture()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            MatchPhaseController match = Object.FindAnyObjectByType<MatchPhaseController>();
            StrategicHoldPoint holdPoint = Object.FindAnyObjectByType<StrategicHoldPoint>();
            Assert.That(holdPoint, Is.Not.Null, "场景必须包含真正可争夺的中央据点");
            holdPoint.ConfigureTimings(0.2f, 10f);
            foreach (PrototypeEnemyAI ai in Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude))
            {
                ai.enabled = false;
            }

            match.Rules.SetDemoValues(1f, 10f, 20f, 0.05f, 0.05f, 0.05f, 2);
            match.StartMatch();
            yield return new WaitForSeconds(1.08f);
            Assert.That(match.CurrentPhase, Is.EqualTo(MatchPhase.Raid));
            Assert.That(holdPoint.IsActive, Is.True);
            MoveTeamForPointTest(match, TeamId.Blue, holdPoint.transform.position);
            MoveTeamForPointTest(match, TeamId.Red, holdPoint.transform.position + Vector3.forward * 20f);
            yield return null;
            Assert.That(holdPoint.BlueOccupants, Is.GreaterThan(0),
                "据点必须能在 RuntimeSet 重载空窗时从比赛战斗员列表识别蓝方玩家");
            yield return new WaitForSeconds(0.25f);
            Assert.That(holdPoint.ControllingTeam, Is.EqualTo(TeamId.Blue));

            MoveTeamForPointTest(match, TeamId.Blue, holdPoint.transform.position + Vector3.back * 20f);
            MoveTeamForPointTest(match, TeamId.Red, holdPoint.transform.position);
            yield return new WaitForSeconds(0.12f);
            Assert.That(holdPoint.ControllingTeam, Is.EqualTo(TeamId.None),
                "敌方推进到中线时应先让据点失控，不能让原控制方继续领取补给");
            yield return new WaitForSeconds(0.14f);
            Assert.That(holdPoint.ControllingTeam, Is.EqualTo(TeamId.Red));
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
            Assert.That(controller.MoveSpeed, Is.EqualTo(4.5f).Within(0.01f));
            Assert.That(controller.SprintSpeed, Is.EqualTo(7f).Within(0.01f));
            Assert.That(controller.SprintSpeed, Is.GreaterThan(controller.MoveSpeed));
            CapsuleCollider damageHitbox = controller.GetComponentInChildren<CapsuleCollider>();
            Assert.That(damageHitbox, Is.Not.Null, "角色必须包含独立受击判定体");
            Assert.That(damageHitbox.isTrigger, Is.True);
            Assert.That(damageHitbox.radius, Is.GreaterThanOrEqualTo(0.7f),
                "受击判定体必须覆盖方块小人的双腿和鞋子，避免腿部射击穿透");
            Vector2 lookBefore = cameraRig.LookAngles;
            cameraRig.ApplyLookDelta(new Vector2(50f, -20f), true);
            Assert.That(cameraRig.LookAngles.x, Is.GreaterThan(lookBefore.x));
            Assert.That(cameraRig.LookAngles.y, Is.GreaterThan(lookBefore.y));

            float distanceBefore = cameraRig.CurrentDistance;
            cameraRig.SetZoomDistance(12f);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(cameraRig.CurrentDistance, Is.GreaterThan(distanceBefore));
            Assert.That(cameraRig.TargetDistance, Is.EqualTo(7f).Within(0.01f),
                "第三人称缩放应受数值配置的最大相机距离约束");

            cameraRig.SetFirstPerson(true);
            yield return null;
            Assert.That(cameraRig.IsFirstPerson, Is.True);
            Assert.That(Vector3.Distance(cameraRig.transform.position, controller.transform.position), Is.LessThan(3f));
            cameraRig.TogglePerspective();
            Assert.That(cameraRig.IsFirstPerson, Is.False);

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
            PrototypeInventory inventory = player.GetComponent<PrototypeInventory>();
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
            yield return null;
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
            Assert.That(weapon.ReserveAmmo, Is.EqualTo(24));
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
            Vector3 intrusionPoint = player.SpawnPosition;
            intruder.transform.position = intrusionPoint;
            intruderController.enabled = true;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(Vector3.Distance(intruder.transform.position, intrusionPoint), Is.GreaterThan(3f),
                "闯入敌方基地的角色应被推出保护区");
        }

        [UnityTest]
        public IEnumerator RaidPhase_ProtectsOnlyThreeMeterSpawnBubble()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            MatchPhaseController match = Object.FindAnyObjectByType<MatchPhaseController>();
            foreach (PrototypeEnemyAI ai in Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude))
            {
                ai.enabled = false;
            }
            match.Rules.SetDemoValues(0.1f, 10f, 20f, 0.05f, 0.05f, 0.05f, 2);
            match.StartMatch();
            yield return new WaitForSeconds(2.1f);

            CombatantLifecycle player = match.GetHumanPlayer();
            BaseProtectionZone[] zones = Object.FindObjectsByType<BaseProtectionZone>(FindObjectsInactive.Exclude);
            Assert.That(match.CurrentPhase, Is.EqualTo(MatchPhase.Raid));
            Assert.That(zones[0].IsProtectionActive || zones[1].IsProtectionActive, Is.False);
            Assert.That(zones[0].IsSpawnProtectionActive && zones[1].IsSpawnProtectionActive, Is.True);
            player.Damageable.SetInvulnerable(false);
            player.Damageable.ApplyDamage(999f, player.transform.position, null);
            yield return new WaitForSeconds(0.1f);
            yield return null;
            Assert.That(player.Damageable.IsAlive, Is.True);
            Assert.That(player.Damageable.IsInvulnerable, Is.True);
            player.Damageable.ApplyDamage(50f, player.transform.position, null);
            Assert.That(player.Damageable.CurrentHealth, Is.EqualTo(100f));

            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position += Vector3.right * 4.2f;
            controller.enabled = true;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(player.Damageable.IsInvulnerable, Is.False);
            player.Damageable.ApplyDamage(10f, player.transform.position, null);
            Assert.That(player.Damageable.CurrentHealth, Is.LessThan(100f));
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

                CharacterController redController = combatant.GetComponent<CharacterController>();
                redController.enabled = false;
                combatant.transform.position += Vector3.forward * 4.2f;
                redController.enabled = true;
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

                CharacterController redController = combatant.GetComponent<CharacterController>();
                redController.enabled = false;
                combatant.transform.position += Vector3.forward * 4.2f;
                redController.enabled = true;
            }

            yield return new WaitForSeconds(2.05f);
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

        [UnityTest]
        public IEnumerator AcceleratedFullMatch_AICombatAndAllPhasesRemainStable()
        {
            SceneManager.LoadScene("ArenaTown");
            yield return null;
            yield return null;

            MatchPhaseController match = Object.FindAnyObjectByType<MatchPhaseController>();
            LootDirector lootDirector = Object.FindAnyObjectByType<LootDirector>();
            PrototypeEnemyAI[] bots = Object.FindObjectsByType<PrototypeEnemyAI>(FindObjectsInactive.Exclude);
            Assert.That(match, Is.Not.Null);
            Assert.That(lootDirector, Is.Not.Null);

            PrototypeEnemyAI blueBot = bots.First(bot => bot.GetComponent<TeamMember>().Team == TeamId.Blue);
            PrototypeEnemyAI redBot = bots.First(bot => bot.GetComponent<TeamMember>().Team == TeamId.Red);
            foreach (PrototypeEnemyAI bot in bots)
            {
                bot.enabled = bot == blueBot || bot == redBot;
            }

            List<MatchPhase> observedPhases = new();
            match.PhaseChanged += (_, phase) => observedPhases.Add(phase);
            match.Rules.SetDemoValues(1.2f, 2.5f, 6.5f, 0.1f, 0.15f, 0.15f, 2);
            Random.InitState(20261103);
            match.StartMatch();

            TeleportForSoak(blueBot, new Vector3(0f, 0.85f, -45f), Quaternion.identity);
            TeleportForSoak(redBot, new Vector3(0f, 0.85f, -30f), Quaternion.Euler(0f, 180f, 0f));
            yield return new WaitForFixedUpdate();

            yield return new WaitForSeconds(6.8f);

            Assert.That(observedPhases, Does.Contain(MatchPhase.Scavenge));
            Assert.That(observedPhases, Does.Contain(MatchPhase.Raid));
            Assert.That(observedPhases, Does.Contain(MatchPhase.FinalBattle));
            Assert.That(match.CurrentPhase, Is.EqualTo(MatchPhase.Complete));
            Assert.That(match.Result, Is.Not.EqualTo(MatchResult.None));

            float damageDealt = match.Combatants
                .Select(combatant => combatant.GetComponent<CombatantStats>())
                .Where(stats => stats != null)
                .Sum(stats => stats.DamageDealt);
            Assert.That(damageDealt, Is.GreaterThan(0f),
                "普通 AI 在开放交战距离内必须能产生实际伤害，不能只做移动表演");
            Assert.That(lootDirector.AvailableBudget, Is.InRange(0, lootDirector.TotalBudget));

            foreach (CombatantLifecycle combatant in match.Combatants)
            {
                Vector3 position = combatant.transform.position;
                Assert.That(float.IsNaN(position.x) || float.IsInfinity(position.x), Is.False);
                Assert.That(float.IsNaN(position.y) || float.IsInfinity(position.y), Is.False);
                Assert.That(float.IsNaN(position.z) || float.IsInfinity(position.z), Is.False);
                Assert.That(position.y, Is.GreaterThan(-5f),
                    $"{combatant.name} 不应穿过地图地面");
            }
        }

        private static void TeleportForSoak(PrototypeEnemyAI bot, Vector3 position, Quaternion rotation)
        {
            CharacterController controller = bot.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            bot.transform.SetPositionAndRotation(position, rotation);
            if (controller != null) controller.enabled = true;
        }

        private static void MoveTeamForPointTest(MatchPhaseController match, TeamId team, Vector3 destination)
        {
            int offset = 0;
            foreach (CombatantLifecycle combatant in match.Combatants)
            {
                if (combatant.Team != team)
                {
                    continue;
                }

                CharacterController controller = combatant.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                combatant.transform.position = destination + (Vector3.right * offset++);
                if (controller != null) controller.enabled = true;
            }
            Physics.SyncTransforms();
        }
    }
}
