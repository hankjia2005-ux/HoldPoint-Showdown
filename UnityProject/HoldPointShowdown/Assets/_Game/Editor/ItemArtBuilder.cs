using System.Collections.Generic;
using System.IO;
using HoldPointShowdown.Data;
using HoldPointShowdown.Presentation;
using HoldPointShowdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace HoldPointShowdown.Editor
{
    public static class ItemArtBuilder
    {
        public const string PistolAssetPath = "Assets/_Game/Data/PrototypePistol.asset";
        public const string SmgAssetPath = "Assets/_Game/Data/Weapons/GreenSmg.asset";
        public const string RifleAssetPath = "Assets/_Game/Data/Weapons/BlueRifle.asset";
        public const string ShotgunAssetPath = "Assets/_Game/Data/Weapons/PurpleAutoShotgun.asset";
        public const string GoldenRifleAssetPath = "Assets/_Game/Data/Weapons/GoldenAirdropRifle.asset";
        public const string BalanceAssetPath = "Assets/_Game/Data/GameBalanceConfig.asset";
        public const string CombatantSetAssetPath = "Assets/_Game/Data/Runtime/CombatantRuntimeSet.asset";
        public const string CameraChannelAssetPath = "Assets/_Game/Data/Runtime/CameraDirectiveChannel.asset";
        public const string MovementAssetPath = "Assets/_Game/Data/MovementProfile.asset";
        public const string LootBudgetAssetPath = "Assets/_Game/Data/LootBudgetConfig.asset";
        public const string AiEasyAssetPath = "Assets/_Game/Data/AIProfile_Easy.asset";
        public const string AiNormalAssetPath = "Assets/_Game/Data/AIProfile_Normal.asset";
        public const string AiHardAssetPath = "Assets/_Game/Data/AIProfile_Hard.asset";

        public const string PistolPrefabPath = "Assets/_Game/Art/Items/Prefabs/Weapon_Pistol.prefab";
        public const string SmgPrefabPath = "Assets/_Game/Art/Items/Prefabs/Weapon_SMG.prefab";
        public const string RiflePrefabPath = "Assets/_Game/Art/Items/Prefabs/Weapon_Rifle.prefab";
        public const string ShotgunPrefabPath = "Assets/_Game/Art/Items/Prefabs/Weapon_AutoShotgun.prefab";
        public const string GoldenRiflePrefabPath = "Assets/_Game/Art/Items/Prefabs/Weapon_GoldenRifle.prefab";
        public const string AmmoPrefabPath = "Assets/_Game/Art/Items/Prefabs/Pickup_Ammo.prefab";
        public const string MedkitPrefabPath = "Assets/_Game/Art/Items/Prefabs/Pickup_Medkit.prefab";
        public const string ArmorPrefabPath = "Assets/_Game/Art/Items/Prefabs/Pickup_Armor.prefab";

        private const string ItemRoot = "Assets/_Game/Art/Items";
        private const string MaterialRoot = ItemRoot + "/Materials";
        private const string PrefabRoot = ItemRoot + "/Prefabs";
        private const string MeshRoot = ItemRoot + "/Meshes";
        private const string WeaponDataRoot = "Assets/_Game/Data/Weapons";

        [MenuItem("HoldPoint Showdown/Art/Rebuild Items And Weapons")]
        public static void BuildAssets()
        {
            EnsureFolder(ItemRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(PrefabRoot);
            EnsureFolder(MeshRoot);
            EnsureFolder(WeaponDataRoot);
            EnsureFolder("Assets/_Game/Data/Runtime");

            Material gunMetal = GetOrCreateMaterial(MaterialRoot + "/GunMetal.mat", new Color(0.075f, 0.09f, 0.13f), 0.55f, 0.65f);
            Material darkPolymer = GetOrCreateMaterial(MaterialRoot + "/DarkPolymer.mat", new Color(0.035f, 0.055f, 0.08f), 0.2f, 0.05f);
            Material common = GetOrCreateMaterial(MaterialRoot + "/Rarity_Common.mat", new Color(0.88f, 0.92f, 0.96f), 0.25f, 0.1f);
            Material uncommon = GetOrCreateMaterial(MaterialRoot + "/Rarity_Uncommon.mat", new Color(0.22f, 0.86f, 0.42f), 0.3f, 0.15f);
            Material rare = GetOrCreateMaterial(MaterialRoot + "/Rarity_Rare.mat", new Color(0.18f, 0.55f, 1f), 0.35f, 0.2f);
            Material epic = GetOrCreateMaterial(MaterialRoot + "/Rarity_Epic.mat", new Color(0.66f, 0.28f, 1f), 0.4f, 0.25f);
            Material legendary = GetOrCreateMaterial(MaterialRoot + "/Rarity_Legendary.mat", new Color(1f, 0.7f, 0.08f), 0.72f, 0.65f);
            Material white = GetOrCreateMaterial(MaterialRoot + "/MedicalWhite.mat", new Color(0.95f, 0.98f, 1f), 0.25f, 0f);
            Material coral = GetOrCreateMaterial(MaterialRoot + "/MedicalCoral.mat", new Color(1f, 0.26f, 0.22f), 0.25f, 0f);
            Material ammoGold = GetOrCreateMaterial(MaterialRoot + "/AmmoGold.mat", new Color(1f, 0.66f, 0.12f), 0.55f, 0.45f);
            Material armorCyan = GetOrCreateMaterial(MaterialRoot + "/ArmorCyan.mat", new Color(0.16f, 0.78f, 0.92f), 0.45f, 0.3f);
            Material glow = GetOrCreateEmissiveMaterial(MaterialRoot + "/ItemGlow.mat", new Color(0.92f, 0.98f, 1f), new Color(1.4f, 1.7f, 2.2f));

            GameObject pistolModel = BuildPistol(gunMetal, darkPolymer, common, glow);
            GameObject smgModel = BuildSmg(gunMetal, darkPolymer, uncommon, glow);
            GameObject rifleModel = BuildRifle(gunMetal, darkPolymer, rare, glow);
            GameObject shotgunModel = BuildShotgun(gunMetal, darkPolymer, epic, glow);
            GameObject goldenRifleModel = BuildGoldenRifle(rare, legendary);
            BuildAmmoPickup(darkPolymer, ammoGold, glow);
            BuildMedkitPickup(white, coral, glow);
            BuildArmorPickup(darkPolymer, armorCyan, glow);

            WeaponConfig pistol = GetOrCreateAsset<WeaponConfig>(PistolAssetPath);
            pistol.SetBalancedValues("weapon.pistol.common", "白色基础手枪", WeaponArchetype.Pistol, ItemRarity.Common,
                AmmoKind.Light, 24f, 3f, 1, 0.8f, 12, 24, 1.6f, 24, 84f, pistolModel,
                new Color(0.88f, 0.92f, 0.96f), WeaponFireMode.SemiAutomatic, 3,
                48, 15f, 45f, 70f, 0.35f, 2.4f, 12f, 0.9f, 0.3f, 1.5f, 1f);

            WeaponConfig smg = GetOrCreateAsset<WeaponConfig>(SmgAssetPath);
            smg.SetBalancedValues("weapon.smg.uncommon", "绿色疾风冲锋枪", WeaponArchetype.SubmachineGun, ItemRarity.Uncommon,
                AmmoKind.Light, 12f, 8f, 1, 1.6f, 30, 60, 2.1f, 60, 96f, smgModel,
                new Color(0.22f, 0.86f, 0.42f), WeaponFireMode.Automatic, 3,
                90, 20f, 55f, 80f, 0.45f, 4.8f, 10f, 0.6f, 0.55f, 0.8f, 1f);

            WeaponConfig rifle = GetOrCreateAsset<WeaponConfig>(RifleAssetPath);
            rifle.SetBalancedValues("weapon.rifle.rare", "蓝色先锋步枪", WeaponArchetype.AssaultRifle, ItemRarity.Rare,
                AmmoKind.Rifle, 22f, 4.2f, 1, 0.5f, 20, 40, 2.3f, 40, 180f, rifleModel,
                new Color(0.18f, 0.55f, 1f), WeaponFireMode.SemiAutomatic, 3,
                60, 45f, 110f, 150f, 0.55f, 3.2f, 14f, 1.4f, 0.35f, 2f, 0.6f);

            WeaponConfig shotgun = GetOrCreateAsset<WeaponConfig>(ShotgunAssetPath);
            shotgun.SetBalancedValues("weapon.shotgun.epic", "紫色风暴连发霰弹枪", WeaponArchetype.AutoShotgun, ItemRarity.Epic,
                AmmoKind.Shell, 11f, 1.3f, 8, 4f, 6, 12, 2.6f, 12, 34f, shotgunModel,
                new Color(0.66f, 0.28f, 1f), WeaponFireMode.SemiAutomatic, 3,
                24, 8f, 18f, 28f, 1.2f, 8f, 8f, 2.2f, 1f, 2.5f, 1.2f);

            WeaponConfig goldenRifle = GetOrCreateAsset<WeaponConfig>(GoldenRifleAssetPath);
            goldenRifle.SetBalancedValues("weapon.rifle.legendary", "金色破晓步枪", WeaponArchetype.AssaultRifle, ItemRarity.Legendary,
                AmmoKind.Rifle, 24f, 4.8f, 1, 0.45f, 20, 40, 2.2f, 40, 180f, goldenRifleModel,
                new Color(1f, 0.7f, 0.08f), WeaponFireMode.Automatic, 3,
                60, 45f, 110f, 150f, 0.5f, 3f, 14f, 1.2f, 0.3f, 1.8f, 0.55f);

            GameBalanceConfig balance = GetOrCreateAsset<GameBalanceConfig>(BalanceAssetPath);
            balance.SetDemoValues();
            CombatantRuntimeSet combatants = GetOrCreateAsset<CombatantRuntimeSet>(CombatantSetAssetPath);
            CameraDirectiveChannel cameraChannel = GetOrCreateAsset<CameraDirectiveChannel>(CameraChannelAssetPath);
            MovementProfile movement = GetOrCreateAsset<MovementProfile>(MovementAssetPath);
            movement.SetRecommendedValues();
            LootBudgetConfig lootBudget = GetOrCreateAsset<LootBudgetConfig>(LootBudgetAssetPath);
            lootBudget.SetRecommendedValues();
            AIProfile easyAi = GetOrCreateAsset<AIProfile>(AiEasyAssetPath);
            easyAi.SetValues(0.9f, 9f, 0.35f, 35f, 15f, 0.45f, 3f, 0.7f, false, 0f);
            AIProfile normalAi = GetOrCreateAsset<AIProfile>(AiNormalAssetPath);
            normalAi.SetValues(0.55f, 4.5f, 0.55f, 55f, 30f, 0.3f, 2.4f, 1f, true, 0.2f);
            AIProfile hardAi = GetOrCreateAsset<AIProfile>(AiHardAssetPath);
            hardAi.SetValues(0.3f, 2f, 0.75f, 75f, 45f, 0.2f, 1.8f, 1.15f, true, 0.4f);

            foreach (Object asset in new Object[]
                     {
                         pistol, smg, rifle, shotgun, goldenRifle, balance, combatants, cameraChannel,
                         movement, lootBudget, easyAi, normalAi, hardAi
                     })
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("道具与武器资产已重建：5 档武器、弹药、医疗包、护甲与数值配置。");
        }

        private static GameObject BuildPistol(Material metal, Material polymer, Material rarity, Material glow)
        {
            GameObject root = CreateWeaponRoot("白色基础手枪");
            List<Renderer> renderers = new();
            CreatePart("白色套筒", root.transform, new Vector3(0f, 0.06f, 0.38f), new Vector3(0.27f, 0.2f, 0.88f), rarity, renderers);
            CreatePart("套筒顶部切面", root.transform, new Vector3(0f, 0.175f, 0.35f), new Vector3(0.19f, 0.045f, 0.66f), metal, renderers);
            CreatePart("枪管外露", root.transform, new Vector3(0f, 0.075f, 0.84f), new Vector3(0.13f, 0.105f, 0.17f), metal, renderers);
            CreateCylinder("枪口环", root.transform, new Vector3(0f, 0.075f, 0.94f), new Vector3(0.095f, 0.055f, 0.095f), metal, renderers, new Vector3(90f, 0f, 0f));
            CreateCylinder("枪口内膛", root.transform, new Vector3(0f, 0.075f, 1.002f), new Vector3(0.052f, 0.016f, 0.052f), polymer, renderers, new Vector3(90f, 0f, 0f));
            CreatePart("下机匣", root.transform, new Vector3(0f, -0.09f, 0.3f), new Vector3(0.24f, 0.13f, 0.52f), polymer, renderers);
            CreatePart("扳机护圈前", root.transform, new Vector3(0f, -0.24f, 0.34f), new Vector3(0.045f, 0.2f, 0.06f), metal, renderers, new Vector3(18f, 0f, 0f));
            CreatePart("扳机护圈后", root.transform, new Vector3(0f, -0.25f, 0.14f), new Vector3(0.045f, 0.2f, 0.06f), metal, renderers, new Vector3(-18f, 0f, 0f));
            CreatePart("扳机护圈底", root.transform, new Vector3(0f, -0.34f, 0.24f), new Vector3(0.045f, 0.05f, 0.22f), metal, renderers);
            CreatePart("扳机", root.transform, new Vector3(0f, -0.23f, 0.25f), new Vector3(0.055f, 0.15f, 0.04f), glow, renderers, new Vector3(-18f, 0f, 0f));
            CreatePart("倾斜握把", root.transform, new Vector3(0f, -0.38f, 0.02f), new Vector3(0.23f, 0.48f, 0.24f), polymer, renderers, new Vector3(-14f, 0f, 0f));
            for (int index = 0; index < 4; index++)
            {
                CreatePart($"握把防滑纹 {index + 1}", root.transform,
                    new Vector3(0f, -0.23f - (index * 0.085f), 0.145f - (index * 0.02f)),
                    new Vector3(0.245f, 0.025f, 0.045f), rarity, renderers, new Vector3(-14f, 0f, 0f));
            }
            CreatePart("弹匣底板", root.transform, new Vector3(0f, -0.63f, -0.04f), new Vector3(0.28f, 0.075f, 0.29f), rarity, renderers, new Vector3(-14f, 0f, 0f));
            CreatePart("后照门", root.transform, new Vector3(0f, 0.23f, 0.05f), new Vector3(0.15f, 0.07f, 0.07f), metal, renderers);
            CreatePart("前准星", root.transform, new Vector3(0f, 0.235f, 0.79f), new Vector3(0.055f, 0.08f, 0.055f), glow, renderers);
            CreatePart("保险钮", root.transform, new Vector3(0.145f, 0.04f, 0.12f), new Vector3(0.055f, 0.055f, 0.12f), glow, renderers);
            Transform muzzle = CreatePivot("枪口", root.transform, new Vector3(0f, 0.075f, 1.02f));
            root.AddComponent<WeaponModelBinding>().Configure(muzzle);
            return SavePrefab(root, PistolPrefabPath);
        }

        private static GameObject BuildSmg(Material metal, Material polymer, Material rarity, Material glow)
        {
            GameObject root = CreateWeaponRoot("绿色疾风冲锋枪");
            List<Renderer> renderers = new();
            CreatePart("绿色机匣", root.transform, new Vector3(0f, 0.03f, 0.34f), new Vector3(0.35f, 0.31f, 0.86f), rarity, renderers);
            CreatePart("下机匣", root.transform, new Vector3(0f, -0.14f, 0.25f), new Vector3(0.31f, 0.15f, 0.5f), polymer, renderers);
            CreatePart("顶部导轨", root.transform, new Vector3(0f, 0.225f, 0.42f), new Vector3(0.21f, 0.055f, 0.92f), metal, renderers);
            AddRailTeeth("顶部导轨齿", root.transform, 0.02f, 0.27f, 0.06f, 7, 0.14f, metal, renderers);
            CreatePart("枪托连接杆", root.transform, new Vector3(0f, 0.05f, -0.34f), new Vector3(0.12f, 0.12f, 0.62f), metal, renderers);
            CreatePart("伸缩托上梁", root.transform, new Vector3(0f, 0.15f, -0.68f), new Vector3(0.25f, 0.1f, 0.4f), polymer, renderers, new Vector3(-8f, 0f, 0f));
            CreatePart("伸缩托下梁", root.transform, new Vector3(0f, -0.08f, -0.68f), new Vector3(0.25f, 0.1f, 0.4f), polymer, renderers, new Vector3(8f, 0f, 0f));
            CreatePart("枪托肩垫", root.transform, new Vector3(0f, 0.02f, -0.93f), new Vector3(0.31f, 0.48f, 0.14f), polymer, renderers);
            CreatePart("握把", root.transform, new Vector3(0f, -0.38f, -0.02f), new Vector3(0.22f, 0.46f, 0.24f), polymer, renderers, new Vector3(-12f, 0f, 0f));
            CreatePart("弧形弹匣上段", root.transform, new Vector3(0f, -0.38f, 0.39f), new Vector3(0.23f, 0.42f, 0.22f), metal, renderers, new Vector3(-5f, 0f, 0f));
            CreatePart("弧形弹匣下段", root.transform, new Vector3(0f, -0.68f, 0.32f), new Vector3(0.22f, 0.28f, 0.2f), metal, renderers, new Vector3(-14f, 0f, 0f));
            CreatePart("绿色护木", root.transform, new Vector3(0f, 0.03f, 0.91f), new Vector3(0.36f, 0.27f, 0.56f), rarity, renderers);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int index = 0; index < 3; index++)
                {
                    CreatePart($"护木散热孔 {side} {index + 1}", root.transform,
                        new Vector3(side * 0.19f, 0.05f, 0.74f + (index * 0.16f)),
                        new Vector3(0.025f, 0.09f, 0.1f), polymer, renderers);
                }
            }
            CreateCylinder("短枪管", root.transform, new Vector3(0f, 0.03f, 1.35f), new Vector3(0.075f, 0.3f, 0.075f), metal, renderers, new Vector3(90f, 0f, 0f));
            CreateCylinder("抑制器", root.transform, new Vector3(0f, 0.03f, 1.67f), new Vector3(0.13f, 0.34f, 0.13f), rarity, renderers, new Vector3(90f, 0f, 0f));
            CreateCylinder("抑制器前盖", root.transform, new Vector3(0f, 0.03f, 2.015f), new Vector3(0.105f, 0.035f, 0.105f), metal, renderers, new Vector3(90f, 0f, 0f));
            AddScope("战术瞄具", root.transform, new Vector3(0f, 0.42f, 0.38f), 0.14f, 0.4f, metal, glow, renderers);
            Transform muzzle = CreatePivot("枪口", root.transform, new Vector3(0f, 0.03f, 2.06f));
            root.AddComponent<WeaponModelBinding>().Configure(muzzle);
            return SavePrefab(root, SmgPrefabPath);
        }

        private static GameObject BuildRifle(Material metal, Material polymer, Material rarity, Material glow)
        {
            GameObject root = CreateWeaponRoot("蓝色先锋步枪");
            List<Renderer> renderers = new();
            CreatePart("蓝色上机匣", root.transform, new Vector3(0f, 0.08f, 0.32f), new Vector3(0.34f, 0.26f, 0.94f), rarity, renderers);
            CreatePart("下机匣", root.transform, new Vector3(0f, -0.12f, 0.25f), new Vector3(0.31f, 0.2f, 0.58f), metal, renderers);
            CreatePart("枪机拉柄", root.transform, new Vector3(0.205f, 0.1f, 0.16f), new Vector3(0.09f, 0.08f, 0.22f), glow, renderers);
            CreatePart("枪托缓冲管", root.transform, new Vector3(0f, 0.05f, -0.37f), new Vector3(0.13f, 0.14f, 0.58f), metal, renderers);
            CreatePart("枪托上梁", root.transform, new Vector3(0f, 0.18f, -0.68f), new Vector3(0.34f, 0.13f, 0.55f), polymer, renderers, new Vector3(-6f, 0f, 0f));
            CreatePart("枪托下梁", root.transform, new Vector3(0f, -0.08f, -0.68f), new Vector3(0.28f, 0.12f, 0.48f), polymer, renderers, new Vector3(10f, 0f, 0f));
            CreatePart("枪托肩垫", root.transform, new Vector3(0f, 0.02f, -1.02f), new Vector3(0.36f, 0.53f, 0.15f), polymer, renderers);
            CreatePart("枪托蓝色饰片", root.transform, new Vector3(0f, 0.23f, -0.68f), new Vector3(0.37f, 0.07f, 0.42f), rarity, renderers);
            CreatePart("握把", root.transform, new Vector3(0f, -0.4f, -0.03f), new Vector3(0.23f, 0.48f, 0.25f), polymer, renderers, new Vector3(-13f, 0f, 0f));
            CreatePart("弧形弹匣上段", root.transform, new Vector3(0f, -0.42f, 0.4f), new Vector3(0.24f, 0.45f, 0.24f), metal, renderers, new Vector3(-6f, 0f, 0f));
            CreatePart("弧形弹匣下段", root.transform, new Vector3(0f, -0.73f, 0.31f), new Vector3(0.23f, 0.3f, 0.21f), metal, renderers, new Vector3(-17f, 0f, 0f));
            CreatePart("蓝色护木", root.transform, new Vector3(0f, 0.05f, 1.02f), new Vector3(0.38f, 0.29f, 0.94f), rarity, renderers);
            AddRailTeeth("护木顶轨", root.transform, 0.54f, 0.245f, 0.07f, 7, 0.14f, metal, renderers);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int index = 0; index < 4; index++)
                {
                    CreatePart($"护木开孔 {side} {index + 1}", root.transform,
                        new Vector3(side * 0.205f, 0.055f, 0.72f + (index * 0.19f)),
                        new Vector3(0.025f, 0.1f, 0.12f), polymer, renderers);
                }
            }
            CreateCylinder("枪管", root.transform, new Vector3(0f, 0.05f, 1.72f), new Vector3(0.07f, 0.52f, 0.07f), metal, renderers, new Vector3(90f, 0f, 0f));
            CreateCylinder("枪口制退器", root.transform, new Vector3(0f, 0.05f, 2.22f), new Vector3(0.11f, 0.16f, 0.11f), metal, renderers, new Vector3(90f, 0f, 0f));
            CreatePart("制退器左孔", root.transform, new Vector3(-0.11f, 0.05f, 2.22f), new Vector3(0.02f, 0.08f, 0.12f), polymer, renderers);
            CreatePart("制退器右孔", root.transform, new Vector3(0.11f, 0.05f, 2.22f), new Vector3(0.02f, 0.08f, 0.12f), polymer, renderers);
            AddScope("中距瞄准镜", root.transform, new Vector3(0f, 0.43f, 0.28f), 0.16f, 0.48f, metal, glow, renderers);
            Transform muzzle = CreatePivot("枪口", root.transform, new Vector3(0f, 0.05f, 2.4f));
            root.AddComponent<WeaponModelBinding>().Configure(muzzle);
            return SavePrefab(root, RiflePrefabPath);
        }

        private static GameObject BuildShotgun(Material metal, Material polymer, Material rarity, Material glow)
        {
            GameObject root = CreateWeaponRoot("紫色风暴连发霰弹枪");
            List<Renderer> renderers = new();
            CreatePart("紫色机匣", root.transform, new Vector3(0f, 0.04f, 0.34f), new Vector3(0.44f, 0.39f, 0.78f), rarity, renderers);
            CreatePart("机匣下护板", root.transform, new Vector3(0f, -0.17f, 0.32f), new Vector3(0.39f, 0.12f, 0.62f), polymer, renderers);
            CreatePart("顶部战术轨", root.transform, new Vector3(0f, 0.27f, 0.53f), new Vector3(0.23f, 0.06f, 1.05f), metal, renderers);
            AddRailTeeth("霰弹枪轨齿", root.transform, 0.12f, 0.315f, 0.08f, 8, 0.14f, metal, renderers);
            CreatePart("枪托连接", root.transform, new Vector3(0f, 0.04f, -0.34f), new Vector3(0.16f, 0.14f, 0.5f), metal, renderers);
            CreatePart("骨架托上梁", root.transform, new Vector3(0f, 0.18f, -0.65f), new Vector3(0.36f, 0.12f, 0.48f), polymer, renderers, new Vector3(-9f, 0f, 0f));
            CreatePart("骨架托下梁", root.transform, new Vector3(0f, -0.08f, -0.65f), new Vector3(0.28f, 0.1f, 0.42f), polymer, renderers, new Vector3(12f, 0f, 0f));
            CreatePart("枪托肩垫", root.transform, new Vector3(0f, 0.03f, -0.94f), new Vector3(0.4f, 0.52f, 0.16f), polymer, renderers);
            CreatePart("握把", root.transform, new Vector3(0f, -0.4f, -0.03f), new Vector3(0.25f, 0.48f, 0.27f), polymer, renderers, new Vector3(-14f, 0f, 0f));
            CreatePart("盒式弹匣上段", root.transform, new Vector3(0f, -0.42f, 0.45f), new Vector3(0.27f, 0.43f, 0.25f), metal, renderers, new Vector3(-5f, 0f, 0f));
            CreatePart("盒式弹匣下段", root.transform, new Vector3(0f, -0.7f, 0.39f), new Vector3(0.25f, 0.25f, 0.22f), glow, renderers, new Vector3(-13f, 0f, 0f));
            CreatePart("紫色加厚护木", root.transform, new Vector3(0f, 0.05f, 1.06f), new Vector3(0.48f, 0.35f, 0.82f), rarity, renderers);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int index = 0; index < 4; index++)
                {
                    CreatePart($"护木泄压孔 {side} {index + 1}", root.transform,
                        new Vector3(side * 0.255f, 0.07f, 0.78f + (index * 0.18f)),
                        new Vector3(0.026f, 0.11f, 0.11f), polymer, renderers);
                }
            }
            CreateCylinder("粗枪管", root.transform, new Vector3(0f, 0.06f, 1.68f), new Vector3(0.105f, 0.48f, 0.105f), metal, renderers, new Vector3(90f, 0f, 0f));
            CreateCylinder("枪口锁环", root.transform, new Vector3(0f, 0.06f, 2.12f), new Vector3(0.16f, 0.1f, 0.16f), rarity, renderers, new Vector3(90f, 0f, 0f));
            CreateCylinder("大口径内膛", root.transform, new Vector3(0f, 0.06f, 2.225f), new Vector3(0.09f, 0.025f, 0.09f), polymer, renderers, new Vector3(90f, 0f, 0f));
            CreatePart("反射瞄具座", root.transform, new Vector3(0f, 0.36f, 0.38f), new Vector3(0.22f, 0.09f, 0.28f), metal, renderers);
            CreatePart("反射镜片", root.transform, new Vector3(0f, 0.5f, 0.44f), new Vector3(0.18f, 0.18f, 0.035f), glow, renderers, new Vector3(-8f, 0f, 0f));
            Transform muzzle = CreatePivot("枪口", root.transform, new Vector3(0f, 0.06f, 2.27f));
            root.AddComponent<WeaponModelBinding>().Configure(muzzle);
            return SavePrefab(root, ShotgunPrefabPath);
        }

        private static GameObject BuildGoldenRifle(Material rare, Material legendary)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(RiflePrefabPath);
            contents.name = "金色破晓步枪";
            foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    if (materials[index] == rare)
                    {
                        materials[index] = legendary;
                    }
                }
                renderer.sharedMaterials = materials;
            }

            List<Renderer> renderers = new(contents.GetComponentsInChildren<Renderer>(true));
            CreatePart("传奇枪口冠", contents.transform, new Vector3(0f, 0.05f, 2.48f),
                new Vector3(0.34f, 0.26f, 0.18f), legendary, renderers);
            CreatePart("左破晓翼", contents.transform, new Vector3(-0.27f, 0.12f, 1.2f),
                new Vector3(0.12f, 0.38f, 0.72f), legendary, renderers, new Vector3(0f, 0f, -18f));
            CreatePart("右破晓翼", contents.transform, new Vector3(0.27f, 0.12f, 1.2f),
                new Vector3(0.12f, 0.38f, 0.72f), legendary, renderers, new Vector3(0f, 0f, 18f));
            CreatePart("传奇弹匣徽记", contents.transform, new Vector3(0f, -0.52f, 0.55f),
                new Vector3(0.27f, 0.11f, 0.2f), legendary, renderers, new Vector3(-8f, 0f, 0f));
            Transform muzzle = contents.transform.Find("枪口");
            if (muzzle != null)
            {
                muzzle.localPosition = new Vector3(0f, 0.05f, 2.6f);
            }
            CombineWeaponMeshes(contents, GoldenRiflePrefabPath);
            PrefabUtility.SaveAsPrefabAsset(contents, GoldenRiflePrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            return AssetDatabase.LoadAssetAtPath<GameObject>(GoldenRiflePrefabPath);
        }

        private static void AddRailTeeth(
            string prefix,
            Transform parent,
            float startZ,
            float y,
            float width,
            int count,
            float spacing,
            Material material,
            List<Renderer> renderers)
        {
            for (int index = 0; index < count; index++)
            {
                CreatePart($"{prefix} {index + 1}", parent,
                    new Vector3(0f, y, startZ + (index * spacing)),
                    new Vector3(0.24f, width, 0.055f), material, renderers);
            }
        }

        private static void AddScope(
            string name,
            Transform parent,
            Vector3 position,
            float radius,
            float length,
            Material body,
            Material lens,
            List<Renderer> renderers)
        {
            CreatePart($"{name}底座", parent, position - new Vector3(0f, radius + 0.05f, 0f),
                new Vector3(0.2f, 0.1f, length * 0.72f), body, renderers);
            CreateCylinder($"{name}镜身", parent, position, new Vector3(radius, length, radius),
                body, renderers, new Vector3(90f, 0f, 0f));
            CreateCylinder($"{name}物镜", parent, position + new Vector3(0f, 0f, length + 0.015f),
                new Vector3(radius * 0.82f, 0.025f, radius * 0.82f), lens, renderers, new Vector3(90f, 0f, 0f));
            CreateCylinder($"{name}调节轮", parent, position + new Vector3(0f, radius * 1.08f, 0f),
                new Vector3(radius * 0.42f, 0.06f, radius * 0.42f), body, renderers);
        }

        private static void BuildAmmoPickup(Material dark, Material gold, Material glow)
        {
            GameObject root = new("弹药箱");
            List<Renderer> renderers = new();
            CreatePart("箱体", root.transform, Vector3.zero, new Vector3(0.9f, 0.52f, 0.68f), dark, renderers);
            CreatePart("横向束带", root.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.96f, 0.16f, 0.72f), gold, renderers);
            CreatePart("提手", root.transform, new Vector3(0f, 0.37f, 0f), new Vector3(0.42f, 0.12f, 0.12f), glow, renderers);
            for (int i = -1; i <= 1; i++)
            {
                CreateCylinder($"子弹 {i}", root.transform, new Vector3(i * 0.2f, 0.3f, 0.16f), new Vector3(0.055f, 0.18f, 0.055f), gold, renderers);
            }
            SavePrefab(root, AmmoPrefabPath);
        }

        private static void BuildMedkitPickup(Material white, Material coral, Material glow)
        {
            GameObject root = new("医疗包");
            List<Renderer> renderers = new();
            CreatePart("软包主体", root.transform, Vector3.zero, new Vector3(0.86f, 0.64f, 0.52f), coral, renderers);
            CreatePart("上盖", root.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.9f, 0.26f, 0.56f), white, renderers);
            CreatePart("十字竖", root.transform, new Vector3(0f, 0f, 0.275f), new Vector3(0.16f, 0.42f, 0.04f), white, renderers);
            CreatePart("十字横", root.transform, new Vector3(0f, 0f, 0.28f), new Vector3(0.42f, 0.16f, 0.04f), white, renderers);
            CreatePart("提手", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.4f, 0.1f, 0.1f), glow, renderers);
            SavePrefab(root, MedkitPrefabPath);
        }

        private static void BuildArmorPickup(Material dark, Material cyan, Material glow)
        {
            GameObject root = new("护甲板");
            List<Renderer> renderers = new();
            CreatePart("护甲主体", root.transform, Vector3.zero, new Vector3(0.72f, 0.92f, 0.18f), dark, renderers, new Vector3(0f, 0f, 45f));
            CreatePart("护甲内芯", root.transform, new Vector3(0f, 0f, 0.11f), new Vector3(0.52f, 0.7f, 0.08f), cyan, renderers, new Vector3(0f, 0f, 45f));
            CreatePart("能量条", root.transform, new Vector3(0f, 0f, 0.17f), new Vector3(0.11f, 0.52f, 0.04f), glow, renderers, new Vector3(0f, 0f, 45f));
            SavePrefab(root, ArmorPrefabPath);
        }

        private static GameObject CreateWeaponRoot(string name)
        {
            GameObject root = new(name);
            root.transform.localScale = Vector3.one * 0.72f;
            return root;
        }

        private static Transform CreatePivot(string name, Transform parent, Vector3 localPosition)
        {
            GameObject pivot = new(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPosition;
            return pivot.transform;
        }

        private static GameObject CreatePart(string name, Transform parent, Vector3 position, Vector3 scale, Material material,
            List<Renderer> renderers, Vector3? euler = null)
        {
            return CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale, material, renderers, euler);
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material,
            List<Renderer> renderers, Vector3? euler = null)
        {
            return CreatePrimitive(name, PrimitiveType.Cylinder, parent, position, scale, material, renderers, euler);
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale,
            Material material, List<Renderer> renderers, Vector3? euler)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            Object.DestroyImmediate(part.GetComponent<Collider>());
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderers.Add(renderer);
            return part;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            if (root.GetComponent<WeaponModelBinding>() != null)
            {
                CombineWeaponMeshes(root, path);
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CombineWeaponMeshes(GameObject root, string prefabPath)
        {
            Dictionary<Material, List<MeshFilter>> groups = new();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || filter.sharedMesh == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                if (!groups.TryGetValue(renderer.sharedMaterial, out List<MeshFilter> filters))
                {
                    filters = new List<MeshFilter>();
                    groups.Add(renderer.sharedMaterial, filters);
                }
                filters.Add(filter);
            }

            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            foreach (KeyValuePair<Material, List<MeshFilter>> group in groups)
            {
                CombineInstance[] combines = new CombineInstance[group.Value.Count];
                for (int index = 0; index < group.Value.Count; index++)
                {
                    MeshFilter filter = group.Value[index];
                    combines[index] = new CombineInstance
                    {
                        mesh = filter.sharedMesh,
                        transform = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix
                    };
                }

                Mesh mesh = new()
                {
                    name = $"{prefabName}_{group.Key.name}",
                    indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
                };
                mesh.CombineMeshes(combines, true, true, false);
                mesh.RecalculateBounds();

                string safeMaterialName = group.Key.name.Replace(' ', '_');
                string meshPath = $"{MeshRoot}/{prefabName}_{safeMaterialName}.asset";
                AssetDatabase.DeleteAsset(meshPath);
                AssetDatabase.CreateAsset(mesh, meshPath);

                GameObject combined = new($"合批_{group.Key.name}");
                combined.transform.SetParent(root.transform, false);
                combined.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = combined.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = group.Key;
            }

            foreach (List<MeshFilter> filters in groups.Values)
            {
                foreach (MeshFilter filter in filters)
                {
                    Object.DestroyImmediate(filter.gameObject);
                }
            }
        }

        private static Material GetOrCreateMaterial(string path, Color color, float smoothness, float metallic)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateEmissiveMaterial(string path, Color color, Color emission)
        {
            Material material = GetOrCreateMaterial(path, color, 0.35f, 0.05f);
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
            return material;
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
