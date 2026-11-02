using System.Collections.Generic;
using HoldPointShowdown.Data;
using HoldPointShowdown.Presentation;
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
        public const string BalanceAssetPath = "Assets/_Game/Data/GameBalanceConfig.asset";

        public const string PistolPrefabPath = "Assets/_Game/Art/Items/Prefabs/Weapon_Pistol.prefab";
        public const string SmgPrefabPath = "Assets/_Game/Art/Items/Prefabs/Weapon_SMG.prefab";
        public const string RiflePrefabPath = "Assets/_Game/Art/Items/Prefabs/Weapon_Rifle.prefab";
        public const string ShotgunPrefabPath = "Assets/_Game/Art/Items/Prefabs/Weapon_AutoShotgun.prefab";
        public const string AmmoPrefabPath = "Assets/_Game/Art/Items/Prefabs/Pickup_Ammo.prefab";
        public const string MedkitPrefabPath = "Assets/_Game/Art/Items/Prefabs/Pickup_Medkit.prefab";
        public const string ArmorPrefabPath = "Assets/_Game/Art/Items/Prefabs/Pickup_Armor.prefab";

        private const string ItemRoot = "Assets/_Game/Art/Items";
        private const string MaterialRoot = ItemRoot + "/Materials";
        private const string PrefabRoot = ItemRoot + "/Prefabs";
        private const string WeaponDataRoot = "Assets/_Game/Data/Weapons";

        [MenuItem("HoldPoint Showdown/Art/Rebuild Items And Weapons")]
        public static void BuildAssets()
        {
            EnsureFolder(ItemRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(PrefabRoot);
            EnsureFolder(WeaponDataRoot);

            Material gunMetal = GetOrCreateMaterial(MaterialRoot + "/GunMetal.mat", new Color(0.075f, 0.09f, 0.13f), 0.55f, 0.65f);
            Material darkPolymer = GetOrCreateMaterial(MaterialRoot + "/DarkPolymer.mat", new Color(0.035f, 0.055f, 0.08f), 0.2f, 0.05f);
            Material common = GetOrCreateMaterial(MaterialRoot + "/Rarity_Common.mat", new Color(0.88f, 0.92f, 0.96f), 0.25f, 0.1f);
            Material uncommon = GetOrCreateMaterial(MaterialRoot + "/Rarity_Uncommon.mat", new Color(0.22f, 0.86f, 0.42f), 0.3f, 0.15f);
            Material rare = GetOrCreateMaterial(MaterialRoot + "/Rarity_Rare.mat", new Color(0.18f, 0.55f, 1f), 0.35f, 0.2f);
            Material epic = GetOrCreateMaterial(MaterialRoot + "/Rarity_Epic.mat", new Color(0.66f, 0.28f, 1f), 0.4f, 0.25f);
            Material white = GetOrCreateMaterial(MaterialRoot + "/MedicalWhite.mat", new Color(0.95f, 0.98f, 1f), 0.25f, 0f);
            Material coral = GetOrCreateMaterial(MaterialRoot + "/MedicalCoral.mat", new Color(1f, 0.26f, 0.22f), 0.25f, 0f);
            Material ammoGold = GetOrCreateMaterial(MaterialRoot + "/AmmoGold.mat", new Color(1f, 0.66f, 0.12f), 0.55f, 0.45f);
            Material armorCyan = GetOrCreateMaterial(MaterialRoot + "/ArmorCyan.mat", new Color(0.16f, 0.78f, 0.92f), 0.45f, 0.3f);
            Material glow = GetOrCreateEmissiveMaterial(MaterialRoot + "/ItemGlow.mat", new Color(0.92f, 0.98f, 1f), new Color(1.4f, 1.7f, 2.2f));

            GameObject pistolModel = BuildPistol(gunMetal, darkPolymer, common, glow);
            GameObject smgModel = BuildSmg(gunMetal, darkPolymer, uncommon, glow);
            GameObject rifleModel = BuildRifle(gunMetal, darkPolymer, rare, glow);
            GameObject shotgunModel = BuildShotgun(gunMetal, darkPolymer, epic, glow);
            BuildAmmoPickup(darkPolymer, ammoGold, glow);
            BuildMedkitPickup(white, coral, glow);
            BuildArmorPickup(darkPolymer, armorCyan, glow);

            WeaponConfig pistol = GetOrCreateAsset<WeaponConfig>(PistolAssetPath);
            pistol.SetBalancedValues("weapon.pistol.common", "白色基础手枪", WeaponArchetype.Pistol, ItemRarity.Common,
                AmmoKind.Light, 20f, 4f, 1, 0.8f, 12, 36, 1.4f, 30, 72f, pistolModel, new Color(0.88f, 0.92f, 0.96f));

            WeaponConfig smg = GetOrCreateAsset<WeaponConfig>(SmgAssetPath);
            smg.SetBalancedValues("weapon.smg.uncommon", "绿色疾风冲锋枪", WeaponArchetype.SubmachineGun, ItemRarity.Uncommon,
                AmmoKind.Light, 13f, 10f, 1, 2.15f, 30, 90, 2f, 36, 48f, smgModel, new Color(0.22f, 0.86f, 0.42f));

            WeaponConfig rifle = GetOrCreateAsset<WeaponConfig>(RifleAssetPath);
            rifle.SetBalancedValues("weapon.rifle.rare", "蓝色先锋步枪", WeaponArchetype.AssaultRifle, ItemRarity.Rare,
                AmmoKind.Rifle, 22f, 6f, 1, 0.65f, 24, 72, 2.2f, 30, 92f, rifleModel, new Color(0.18f, 0.55f, 1f));

            WeaponConfig shotgun = GetOrCreateAsset<WeaponConfig>(ShotgunAssetPath);
            shotgun.SetBalancedValues("weapon.shotgun.epic", "紫色风暴连发霰弹枪", WeaponArchetype.AutoShotgun, ItemRarity.Epic,
                AmmoKind.Shell, 8f, 2.2f, 8, 6.4f, 8, 32, 2.6f, 12, 24f, shotgunModel, new Color(0.66f, 0.28f, 1f));

            GameBalanceConfig balance = GetOrCreateAsset<GameBalanceConfig>(BalanceAssetPath);
            balance.SetDemoValues();

            foreach (Object asset in new Object[] { pistol, smg, rifle, shotgun, balance })
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("道具与武器资产已重建：4 档武器、弹药、医疗包、护甲与数值配置。 ");
        }

        private static GameObject BuildPistol(Material metal, Material polymer, Material rarity, Material glow)
        {
            GameObject root = CreateWeaponRoot("白色基础手枪");
            List<Renderer> renderers = new();
            CreatePart("套筒", root.transform, new Vector3(0f, 0.04f, 0.36f), new Vector3(0.22f, 0.18f, 0.76f), metal, renderers);
            CreatePart("套筒高光", root.transform, new Vector3(0f, 0.145f, 0.34f), new Vector3(0.16f, 0.035f, 0.58f), rarity, renderers);
            CreatePart("下机匣", root.transform, new Vector3(0f, -0.09f, 0.24f), new Vector3(0.2f, 0.12f, 0.46f), polymer, renderers);
            CreatePart("握把", root.transform, new Vector3(0f, -0.31f, 0.07f), new Vector3(0.19f, 0.4f, 0.2f), polymer, renderers, new Vector3(-12f, 0f, 0f));
            CreatePart("弹匣底板", root.transform, new Vector3(0f, -0.53f, 0.02f), new Vector3(0.24f, 0.07f, 0.25f), rarity, renderers);
            CreatePart("准星", root.transform, new Vector3(0f, 0.17f, 0.63f), new Vector3(0.05f, 0.07f, 0.06f), glow, renderers);
            Transform muzzle = CreatePivot("枪口", root.transform, new Vector3(0f, 0.04f, 0.77f));
            root.AddComponent<WeaponModelBinding>().Configure(muzzle);
            return SavePrefab(root, PistolPrefabPath);
        }

        private static GameObject BuildSmg(Material metal, Material polymer, Material rarity, Material glow)
        {
            GameObject root = CreateWeaponRoot("绿色疾风冲锋枪");
            List<Renderer> renderers = new();
            CreatePart("机匣", root.transform, new Vector3(0f, 0f, 0.34f), new Vector3(0.31f, 0.3f, 0.82f), polymer, renderers);
            CreatePart("机匣护片", root.transform, new Vector3(0f, 0.12f, 0.32f), new Vector3(0.34f, 0.1f, 0.56f), rarity, renderers);
            CreatePart("枪托杆", root.transform, new Vector3(0f, 0.02f, -0.26f), new Vector3(0.12f, 0.12f, 0.5f), metal, renderers);
            CreatePart("枪托", root.transform, new Vector3(0f, -0.02f, -0.52f), new Vector3(0.28f, 0.42f, 0.16f), polymer, renderers);
            CreatePart("握把", root.transform, new Vector3(0f, -0.32f, 0.03f), new Vector3(0.2f, 0.4f, 0.22f), polymer, renderers, new Vector3(-10f, 0f, 0f));
            CreatePart("弹匣", root.transform, new Vector3(0f, -0.38f, 0.38f), new Vector3(0.2f, 0.5f, 0.2f), rarity, renderers, new Vector3(-6f, 0f, 0f));
            CreatePart("护木", root.transform, new Vector3(0f, 0f, 0.84f), new Vector3(0.28f, 0.24f, 0.42f), metal, renderers);
            CreateCylinder("枪管", root.transform, new Vector3(0f, 0f, 1.13f), new Vector3(0.075f, 0.34f, 0.075f), metal, renderers, new Vector3(90f, 0f, 0f));
            CreatePart("全息瞄具", root.transform, new Vector3(0f, 0.27f, 0.39f), new Vector3(0.18f, 0.18f, 0.24f), glow, renderers);
            Transform muzzle = CreatePivot("枪口", root.transform, new Vector3(0f, 0f, 1.46f));
            root.AddComponent<WeaponModelBinding>().Configure(muzzle);
            return SavePrefab(root, SmgPrefabPath);
        }

        private static GameObject BuildRifle(Material metal, Material polymer, Material rarity, Material glow)
        {
            GameObject root = CreateWeaponRoot("蓝色先锋步枪");
            List<Renderer> renderers = new();
            CreatePart("上机匣", root.transform, new Vector3(0f, 0.08f, 0.28f), new Vector3(0.3f, 0.24f, 0.9f), metal, renderers);
            CreatePart("下机匣", root.transform, new Vector3(0f, -0.1f, 0.22f), new Vector3(0.28f, 0.19f, 0.55f), polymer, renderers);
            CreatePart("枪托", root.transform, new Vector3(0f, 0f, -0.49f), new Vector3(0.33f, 0.42f, 0.62f), polymer, renderers);
            CreatePart("枪托饰条", root.transform, new Vector3(0f, 0.12f, -0.52f), new Vector3(0.36f, 0.08f, 0.5f), rarity, renderers);
            CreatePart("握把", root.transform, new Vector3(0f, -0.36f, -0.02f), new Vector3(0.21f, 0.43f, 0.23f), polymer, renderers, new Vector3(-12f, 0f, 0f));
            CreatePart("弧形弹匣", root.transform, new Vector3(0f, -0.42f, 0.39f), new Vector3(0.22f, 0.55f, 0.24f), rarity, renderers, new Vector3(-10f, 0f, 0f));
            CreatePart("护木", root.transform, new Vector3(0f, 0.02f, 0.92f), new Vector3(0.32f, 0.28f, 0.78f), rarity, renderers);
            CreateCylinder("枪管", root.transform, new Vector3(0f, 0.02f, 1.48f), new Vector3(0.065f, 0.45f, 0.065f), metal, renderers, new Vector3(90f, 0f, 0f));
            CreatePart("瞄准镜座", root.transform, new Vector3(0f, 0.27f, 0.29f), new Vector3(0.2f, 0.1f, 0.4f), polymer, renderers);
            CreateCylinder("瞄准镜", root.transform, new Vector3(0f, 0.39f, 0.29f), new Vector3(0.13f, 0.32f, 0.13f), glow, renderers, new Vector3(90f, 0f, 0f));
            Transform muzzle = CreatePivot("枪口", root.transform, new Vector3(0f, 0.02f, 1.93f));
            root.AddComponent<WeaponModelBinding>().Configure(muzzle);
            return SavePrefab(root, RiflePrefabPath);
        }

        private static GameObject BuildShotgun(Material metal, Material polymer, Material rarity, Material glow)
        {
            GameObject root = CreateWeaponRoot("紫色风暴连发霰弹枪");
            List<Renderer> renderers = new();
            CreatePart("鼓形机匣", root.transform, new Vector3(0f, 0.03f, 0.32f), new Vector3(0.42f, 0.38f, 0.74f), polymer, renderers);
            CreatePart("机匣装甲", root.transform, new Vector3(0f, 0.18f, 0.35f), new Vector3(0.46f, 0.12f, 0.62f), rarity, renderers);
            CreatePart("短枪托", root.transform, new Vector3(0f, -0.03f, -0.39f), new Vector3(0.42f, 0.48f, 0.52f), polymer, renderers);
            CreatePart("握把", root.transform, new Vector3(0f, -0.36f, -0.01f), new Vector3(0.24f, 0.42f, 0.25f), polymer, renderers, new Vector3(-13f, 0f, 0f));
            CreateCylinder("弹鼓", root.transform, new Vector3(0f, -0.38f, 0.42f), new Vector3(0.34f, 0.17f, 0.34f), rarity, renderers);
            CreatePart("前护木", root.transform, new Vector3(0f, 0.02f, 0.88f), new Vector3(0.48f, 0.34f, 0.55f), rarity, renderers);
            CreateCylinder("左枪管", root.transform, new Vector3(-0.1f, 0.06f, 1.27f), new Vector3(0.08f, 0.42f, 0.08f), metal, renderers, new Vector3(90f, 0f, 0f));
            CreateCylinder("右枪管", root.transform, new Vector3(0.1f, 0.06f, 1.27f), new Vector3(0.08f, 0.42f, 0.08f), metal, renderers, new Vector3(90f, 0f, 0f));
            CreatePart("反射瞄具", root.transform, new Vector3(0f, 0.33f, 0.42f), new Vector3(0.24f, 0.2f, 0.2f), glow, renderers);
            Transform muzzle = CreatePivot("枪口", root.transform, new Vector3(0f, 0.06f, 1.69f));
            root.AddComponent<WeaponModelBinding>().Configure(muzzle);
            return SavePrefab(root, ShotgunPrefabPath);
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
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
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
