using System;
using System.Collections.Generic;
using HoldPointShowdown.AI;
using HoldPointShowdown.Combat;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Gameplay;
using HoldPointShowdown.Player;
using HoldPointShowdown.Presentation;
using HoldPointShowdown.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldPointShowdown.Editor
{
    public static class ProjectBootstrapper
    {
    private const string GameRoot = "Assets/_Game";
    private const string ScenesRoot = GameRoot + "/Scenes";
    private const string DataRoot = GameRoot + "/Data";
    private const string MaterialsRoot = GameRoot + "/Art/Materials";

    [MenuItem("HoldPoint Showdown/Bootstrap Project")]
    public static void RunFromMenu()
    {
        BuildProjectFoundation(false);
    }

    [MenuItem("HoldPoint Showdown/Rebuild Prototype Scenes")]
    public static void RebuildFromMenu()
    {
        if (EditorUtility.DisplayDialog(
                "Rebuild prototype scenes?",
                "This overwrites Bootstrap, MainMenu, TestGym and ArenaTown scenes.",
                "Rebuild",
                "Cancel"))
        {
            BuildProjectFoundation(true);
        }
    }

    public static void RunBatch()
    {
        BuildProjectFoundation(false);
    }

    [MenuItem("HoldPoint Showdown/Rebuild ArenaTown Visual Slice")]
    public static void RebuildArenaTownVisualSlice()
    {
        EnsureFolder(ScenesRoot);
        EnsureFolder(DataRoot);
        EnsureFolder(MaterialsRoot);

        GameRuleConfig rules = GetOrCreateAsset<GameRuleConfig>(DataRoot + "/GameRuleConfig.asset");
        rules.SetDemoValues();
        EditorUtility.SetDirty(rules);
        ItemArtBuilder.BuildAssets();
        WeaponConfig pistol = AssetDatabase.LoadAssetAtPath<WeaponConfig>(ItemArtBuilder.PistolAssetPath);

        Material blue = GetOrCreateMaterial(MaterialsRoot + "/TeamBlue.mat", new Color(0.16f, 0.64f, 1f));
        Material red = GetOrCreateMaterial(MaterialsRoot + "/TeamRed.mat", new Color(1f, 0.25f, 0.22f));
        Material neutral = GetOrCreateMaterial(MaterialsRoot + "/NeutralTown.mat", new Color(0.78f, 0.82f, 0.84f));
        Material dark = GetOrCreateMaterial(MaterialsRoot + "/DarkAccent.mat", new Color(0.12f, 0.15f, 0.2f));

        CreateSceneIfNeeded("ArenaTown", true, scene => CreateArenaTownVisualScene(rules, pistol, blue, red, neutral, dark));
        AssetDatabase.SaveAssets();
        Debug.Log("ArenaTown visual slice rebuilt and opened.");
    }

    private static void BuildProjectFoundation(bool overwriteScenes)
    {
        EnsureFolder(ScenesRoot);
        EnsureFolder(DataRoot);
        EnsureFolder(MaterialsRoot);
        EnsureFolder(GameRoot + "/Art");
        EnsureFolder(GameRoot + "/Audio");
        EnsureFolder(GameRoot + "/Prefabs");
        EnsureFolder(GameRoot + "/Settings");
        EnsureFolder(GameRoot + "/Tests/EditMode");
        EnsureFolder(GameRoot + "/Tests/PlayMode");
        EnsureFolder(GameRoot + "/UI");

        GameRuleConfig rules = GetOrCreateAsset<GameRuleConfig>(DataRoot + "/GameRuleConfig.asset");
        rules.SetDemoValues();
        EditorUtility.SetDirty(rules);
        ItemArtBuilder.BuildAssets();
        WeaponConfig pistol = AssetDatabase.LoadAssetAtPath<WeaponConfig>(ItemArtBuilder.PistolAssetPath);

        Material blue = GetOrCreateMaterial(MaterialsRoot + "/TeamBlue.mat", new Color(0.16f, 0.64f, 1f));
        Material red = GetOrCreateMaterial(MaterialsRoot + "/TeamRed.mat", new Color(1f, 0.25f, 0.22f));
        Material neutral = GetOrCreateMaterial(MaterialsRoot + "/NeutralTown.mat", new Color(0.78f, 0.82f, 0.84f));
        Material dark = GetOrCreateMaterial(MaterialsRoot + "/DarkAccent.mat", new Color(0.12f, 0.15f, 0.2f));

        CreateSceneIfNeeded("Bootstrap", overwriteScenes, scene => CreateBootstrapScene());
        CreateSceneIfNeeded("MainMenu", overwriteScenes, scene => CreateMainMenuScene());
        CreateSceneIfNeeded("TestGym", overwriteScenes, scene => CreateTestGymScene(rules, pistol, blue, red, neutral, dark));
        CreateSceneIfNeeded("ArenaTown", overwriteScenes, scene => CreateArenaTownVisualScene(rules, pistol, blue, red, neutral, dark));

        CleanupTemplateAssets();
        ConfigureProjectSettings();
        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("HoldPoint Showdown project foundation is ready.");
    }

    private static void CreateBootstrapScene()
    {
        CreateCamera(new Vector3(0f, 2f, -8f), Quaternion.identity, false);
        CreateSceneLabel("BOOTSTRAP", "Global services and startup flow will live in this scene.");
    }

    private static void CreateMainMenuScene()
    {
        Camera camera = CreateCamera(new Vector3(0f, 3f, -10f), Quaternion.identity, false);
        camera.backgroundColor = new Color(0.08f, 0.12f, 0.18f);
        CreateSceneLabel("HOLDPOINT SHOWDOWN", "Prototype foundation\nOpen TestGym to test movement and shooting.");
    }

    private static void CreateTestGymScene(
        GameRuleConfig rules,
        WeaponConfig pistol,
        Material blue,
        Material red,
        Material neutral,
        Material dark)
    {
        CreateLighting();
        CreatePrimitive("Floor", PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f), new Vector3(24f, 1f, 24f), neutral);
        CreatePrimitive("Backstop", PrimitiveType.Cube, new Vector3(0f, 2f, 10f), new Vector3(24f, 5f, 0.5f), dark);
        CreatePrimitive("LeftWall", PrimitiveType.Cube, new Vector3(-12f, 2f, 0f), new Vector3(0.5f, 5f, 24f), dark);
        CreatePrimitive("RightWall", PrimitiveType.Cube, new Vector3(12f, 2f, 0f), new Vector3(0.5f, 5f, 24f), dark);

        GameObject player = CreatePrimitive("PrototypePlayer", PrimitiveType.Capsule, new Vector3(0f, 1f, -5f), Vector3.one, blue);
        player.layer = LayerMask.NameToLayer("Ignore Raycast");
        Collider primitiveCollider = player.GetComponent<Collider>();
        if (primitiveCollider != null)
        {
            UnityEngine.Object.DestroyImmediate(primitiveCollider);
        }

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.45f;
        characterController.center = Vector3.zero;

        Damageable playerDamageable = player.AddComponent<Damageable>();
        playerDamageable.Configure(rules.PlayerMaxHealth, rules.PlayerMaxArmor);

        Camera camera = CreateCamera(new Vector3(0f, 3.5f, -10f), Quaternion.identity, true);
        ThirdPersonCameraRig cameraRig = camera.GetComponent<ThirdPersonCameraRig>();
        cameraRig.Configure(player.transform);

        PrototypeHitscanWeapon weapon = player.AddComponent<PrototypeHitscanWeapon>();
        weapon.Configure(pistol, camera);
        ThirdPersonPlayerController controller = player.AddComponent<ThirdPersonPlayerController>();
        controller.Configure(camera, weapon);

        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 3.5f;
            float z = 4f + (Mathf.Abs(i - 2) * 1.2f);
            GameObject target = CreatePrimitive($"DamageTarget_{i + 1}", PrimitiveType.Capsule, new Vector3(x, 1f, z), Vector3.one, red);
            target.AddComponent<Damageable>().Configure(100f, i % 2 == 0 ? 50f : 0f);
        }

        GameObject flowObject = new("MatchFlow");
        MatchPhaseController match = flowObject.AddComponent<MatchPhaseController>();
        match.Configure(rules);

        GameObject hudObject = new("PrototypeHUD");
        PrototypeHud hud = hudObject.AddComponent<PrototypeHud>();
        hud.Configure(match, weapon, playerDamageable);
    }

    private static void CreateArenaTownScene(Material blue, Material red, Material neutral, Material dark)
    {
        CreateLighting();
        CreatePrimitive("TownGround", PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f), new Vector3(50f, 1f, 80f), neutral);
        CreatePrimitive("BlueBase", PrimitiveType.Cube, new Vector3(0f, 1.5f, -32f), new Vector3(14f, 3f, 8f), blue);
        CreatePrimitive("RedBase", PrimitiveType.Cube, new Vector3(0f, 1.5f, 32f), new Vector3(14f, 3f, 8f), red);

        for (int row = -2; row <= 2; row++)
        {
            if (row == 0)
            {
                continue;
            }

            CreatePrimitive($"LeftBuilding_{row}", PrimitiveType.Cube, new Vector3(-11f, 2f, row * 10f), new Vector3(8f, 4f, 7f), dark);
            CreatePrimitive($"RightBuilding_{row}", PrimitiveType.Cube, new Vector3(11f, 2f, row * 10f), new Vector3(8f, 4f, 7f), dark);
        }

        CreatePrimitive("CentralCover", PrimitiveType.Cube, new Vector3(0f, 1f, 0f), new Vector3(5f, 2f, 5f), dark);
        Camera camera = CreateCamera(new Vector3(34f, 42f, -48f), Quaternion.Euler(28f, -32f, 0f), false);
        camera.farClipPlane = 250f;
        CreateSceneLabel("ARENA TOWN · GRAYBOX", "Symmetric bases, central contest area and two side routes.");
    }

    private static void CreateArenaTownVisualScene(
        GameRuleConfig rules,
        WeaponConfig pistol,
        Material blue,
        Material red,
        Material neutral,
        Material dark)
    {
        CharacterArtBuilder.BuildAssets();
        ItemArtBuilder.BuildAssets();

        GameBalanceConfig balance = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(ItemArtBuilder.BalanceAssetPath);
        WeaponConfig smg = AssetDatabase.LoadAssetAtPath<WeaponConfig>(ItemArtBuilder.SmgAssetPath);
        WeaponConfig rifle = AssetDatabase.LoadAssetAtPath<WeaponConfig>(ItemArtBuilder.RifleAssetPath);
        WeaponConfig shotgun = AssetDatabase.LoadAssetAtPath<WeaponConfig>(ItemArtBuilder.ShotgunAssetPath);

        Material grass = GetOrCreateMaterial(MaterialsRoot + "/Grass.mat", new Color(0.34f, 0.68f, 0.43f));
        Material road = GetOrCreateMaterial(MaterialsRoot + "/Road.mat", new Color(0.17f, 0.22f, 0.28f));
        Material warmWhite = GetOrCreateMaterial(MaterialsRoot + "/WarmWhite.mat", new Color(0.94f, 0.89f, 0.76f));
        Material coral = GetOrCreateMaterial(MaterialsRoot + "/CoralRoof.mat", new Color(0.96f, 0.43f, 0.31f));
        Material teal = GetOrCreateMaterial(MaterialsRoot + "/TealAccent.mat", new Color(0.16f, 0.72f, 0.67f));
        Material yellow = GetOrCreateEmissiveMaterial(
            MaterialsRoot + "/LootGlow.mat",
            new Color(1f, 0.72f, 0.18f),
            new Color(2.2f, 1.1f, 0.1f));
        Material glass = GetOrCreateEmissiveMaterial(
            MaterialsRoot + "/WindowGlow.mat",
            new Color(0.16f, 0.3f, 0.42f),
            new Color(0.08f, 0.35f, 0.5f));
        Material blueShield = GetOrCreateTransparentMaterial(
            MaterialsRoot + "/BlueShield.mat",
            new Color(0.18f, 0.72f, 1f, 0.18f));
        Material redShield = GetOrCreateTransparentMaterial(
            MaterialsRoot + "/RedShield.mat",
            new Color(1f, 0.28f, 0.24f, 0.18f));

        CreateLighting();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.58f, 0.78f, 0.9f);
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.ambientLight = new Color(0.62f, 0.68f, 0.74f);
        RenderSettings.reflectionIntensity = 0.65f;
        QualitySettings.shadowDistance = 110f;

        GameObject environment = new("Environment");
        GameObject architecture = new("Architecture");
        GameObject props = new("Gameplay Props");
        GameObject presentation = new("Presentation");

        CreateBlock("Grass Field", environment.transform, new Vector3(0f, -0.75f, 0f), new Vector3(62f, 1.5f, 94f), grass);
        CreateBlock("Main Street", environment.transform, new Vector3(0f, 0.04f, 0f), new Vector3(13f, 0.12f, 82f), road);
        CreateBlock("Left Sidewalk", environment.transform, new Vector3(-8f, 0.13f, 0f), new Vector3(3f, 0.26f, 82f), neutral);
        CreateBlock("Right Sidewalk", environment.transform, new Vector3(8f, 0.13f, 0f), new Vector3(3f, 0.26f, 82f), neutral);
        CreateBlock("Center Plaza", environment.transform, new Vector3(0f, 0.16f, 0f), new Vector3(19f, 0.32f, 19f), warmWhite);

        for (int z = -30; z <= 30; z += 6)
        {
            CreateBlock($"Road Mark {z}", environment.transform, new Vector3(0f, 0.13f, z), new Vector3(0.28f, 0.03f, 2.8f), warmWhite, false);
        }

        GameObject flowObject = new("Match Flow");
        MatchPhaseController match = flowObject.AddComponent<MatchPhaseController>();
        match.Configure(rules);

        GameObject blueChest = CreateTeamBase("Blue Base", -34f, true, TeamId.Blue, match,
            architecture.transform, props.transform, blue, warmWhite, dark, yellow, blueShield);
        GameObject redChest = CreateTeamBase("Red Base", 34f, false, TeamId.Red, match,
            architecture.transform, props.transform, red, warmWhite, dark, yellow, redShield);

        float[] buildingRows = { -22f, -9f, 9f, 22f };
        for (int i = 0; i < buildingRows.Length; i++)
        {
            float height = i % 2 == 0 ? 6.4f : 7.8f;
            Material accent = i % 2 == 0 ? coral : teal;
            CreateShopBuilding($"West Shop {i + 1}", -18f, buildingRows[i], true, height, architecture.transform, warmWhite, accent, dark, glass);
            CreateShopBuilding($"East Shop {i + 1}", 18f, -buildingRows[i], false, height, architecture.transform, warmWhite, accent, dark, glass);
        }

        CreateCentralLandmark(props.transform, dark, teal, yellow);
        CreateCoverLayout(props.transform, dark, warmWhite, coral, teal);
        CreateTacticalStructures(props.transform, dark, warmWhite, coral, teal, yellow, glass);
        CreateStreetFurniture(presentation.transform, dark, teal, yellow, grass);
        CreateSupplyPickup("护甲补给", new Vector3(-6.4f, 1.25f, -11f), props.transform,
            SupplyKind.ArmorPlate, ItemArtBuilder.ArmorPrefabPath, balance, teal);
        CreateSupplyPickup("轻型弹药", new Vector3(6.4f, 1.25f, 11f), props.transform,
            SupplyKind.AmmoPack, ItemArtBuilder.AmmoPrefabPath, balance, yellow);
        CreateSupplyPickup("医疗补给", new Vector3(-6.4f, 1.25f, 15f), props.transform,
            SupplyKind.Medkit, ItemArtBuilder.MedkitPrefabPath, balance, coral);
        CreateWeaponPickup("绿色冲锋枪刷新点", new Vector3(6.4f, 1.15f, -15f), props.transform, smg, balance);
        CreateWeaponPickup("蓝色步枪刷新点", new Vector3(-13.2f, 1.15f, 6.8f), props.transform, rifle, balance);
        CreateWeaponPickup("紫色霰弹枪刷新点", new Vector3(13.2f, 1.15f, -6.8f), props.transform, shotgun, balance);
        CreateSupplyPickup("东侧弹药", new Vector3(18f, 1f, 22f), props.transform,
            SupplyKind.AmmoPack, ItemArtBuilder.AmmoPrefabPath, balance, yellow);
        CreateSupplyPickup("西侧医疗", new Vector3(-18f, 1f, -22f), props.transform,
            SupplyKind.Medkit, ItemArtBuilder.MedkitPrefabPath, balance, coral);

        GameObject player = CreateShowcasePlayer(new Vector3(0f, 0.15f, -35.6f), rules);
        Camera camera = CreateCamera(new Vector3(0f, 4.5f, -42f), Quaternion.identity, true);
        camera.backgroundColor = new Color(0.43f, 0.72f, 0.91f);
        camera.fieldOfView = 64f;
        camera.farClipPlane = 180f;
        camera.GetComponent<ThirdPersonCameraRig>().Configure(player.transform);

        PrototypeHitscanWeapon weapon = player.AddComponent<PrototypeHitscanWeapon>();
        weapon.Configure(pistol, camera);
        ThirdPersonPlayerController controller = player.AddComponent<ThirdPersonPlayerController>();
        controller.Configure(camera, weapon);

        Damageable playerDamageable = player.GetComponent<Damageable>();
        TeamMember playerTeam = player.GetComponent<TeamMember>();
        BlockCharacterVisual playerVisual = player.GetComponentInChildren<BlockCharacterVisual>();
        PrototypeWeaponEffects playerEffects = player.AddComponent<PrototypeWeaponEffects>();
        playerEffects.Configure(weapon, playerVisual != null ? playerVisual.Muzzle : null);
        EquippedWeaponView weaponView = player.AddComponent<EquippedWeaponView>();
        weaponView.Configure(weapon, playerVisual != null ? playerVisual.WeaponSocket : null, playerEffects);
        WeaponLoadout loadout = player.AddComponent<WeaponLoadout>();
        loadout.Configure(weapon, balance != null ? balance.MaxCarriedWeapons : 2, pistol);
        PrototypeInventory inventory = player.AddComponent<PrototypeInventory>();
        inventory.Configure(weapon, playerDamageable, loadout, balance);
        CombatantLifecycle playerLifecycle = player.AddComponent<CombatantLifecycle>();
        playerLifecycle.Configure(match, pistol, inventory, loadout, true);

        blueChest.AddComponent<SharedSupplyChest>().Configure(TeamId.Blue, player.transform, playerTeam, inventory, match);
        redChest.AddComponent<SharedSupplyChest>().Configure(TeamId.Red, player.transform, playerTeam, inventory, match);

        CreateBot("蓝方支援", new Vector3(-3.6f, 0.15f, -34.8f), props.transform,
            rules, match, TeamId.Blue, 0f, rifle);
        CreateBot("红方守卫甲", new Vector3(-3.6f, 0.15f, 34.8f), props.transform,
            rules, match, TeamId.Red, 25f, rifle);
        CreateBot("红方守卫乙", new Vector3(3.6f, 0.15f, 34.8f), props.transform,
            rules, match, TeamId.Red, 0f, smg);

        GameObject hudObject = new("Showcase HUD");
        PrototypeHud hud = hudObject.AddComponent<PrototypeHud>();
        hud.Configure(match, weapon, playerDamageable, inventory);
    }

    private static GameObject CreateTeamBase(
        string name,
        float z,
        bool isBlue,
        TeamId teamId,
        MatchPhaseController match,
        Transform architectureParent,
        Transform propsParent,
        Material team,
        Material warmWhite,
        Material dark,
        Material glow,
        Material shield)
    {
        float forward = isBlue ? 1f : -1f;
        GameObject root = new(name);
        root.transform.SetParent(architectureParent);

        CreateBlock("Team Platform", root.transform, new Vector3(0f, 0.28f, z), new Vector3(19f, 0.56f, 12f), team);
        CreateBlock("Inner Deck", root.transform, new Vector3(0f, 0.59f, z + (forward * 0.4f)), new Vector3(16.5f, 0.16f, 9.5f), warmWhite);
        CreateBlock("Rear Wall", root.transform, new Vector3(0f, 3f, z - (forward * 5.2f)), new Vector3(19f, 5.4f, 0.55f), dark);
        CreateBlock("Left Wing", root.transform, new Vector3(-8.7f, 2f, z - (forward * 1.5f)), new Vector3(1.2f, 3.4f, 7.2f), team);
        CreateBlock("Right Wing", root.transform, new Vector3(8.7f, 2f, z - (forward * 1.5f)), new Vector3(1.2f, 3.4f, 7.2f), team);
        CreateBlock("Roof", root.transform, new Vector3(0f, 5.9f, z - (forward * 4.4f)), new Vector3(19f, 0.55f, 2.2f), team);
        CreateBlock("Base Sign", root.transform, new Vector3(0f, 4.1f, z - (forward * 5.55f)), new Vector3(7f, 1.25f, 0.24f), warmWhite, false);

        GameObject spawnRing = CreateBlock("Spawn Ring", root.transform, new Vector3(0f, 0.72f, z - (forward * 1.6f)), new Vector3(3.2f, 0.08f, 3.2f), glow, false, PrimitiveType.Cylinder);
        spawnRing.transform.rotation = Quaternion.identity;

        GameObject shieldObject = CreateBlock("Protection Shield", root.transform, new Vector3(0f, 3.4f, z), new Vector3(10.4f, 6.2f, 8.4f), shield, false, PrimitiveType.Sphere);
        Renderer shieldRenderer = shieldObject.GetComponent<Renderer>();
        if (shieldRenderer != null)
        {
            shieldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shieldRenderer.receiveShadows = false;
        }

        GameObject protectionZone = new($"{name} Protection Zone");
        protectionZone.transform.SetParent(root.transform, true);
        protectionZone.transform.position = new Vector3(0f, 3.25f, z);
        BoxCollider protectionTrigger = protectionZone.AddComponent<BoxCollider>();
        protectionTrigger.isTrigger = true;
        protectionTrigger.size = new Vector3(18.5f, 6.5f, 11.5f);
        BaseProtectionZone protection = protectionZone.AddComponent<BaseProtectionZone>();
        protection.Configure(
            teamId,
            match,
            shieldRenderer,
            new Vector3(0f, 0.15f, z + (forward * 7.2f)));

        GameObject chestRoot = new("Shared Supply Chest");
        chestRoot.transform.SetParent(propsParent);
        Vector3 chestPosition = new(0f, 1.15f, z + (forward * 2.2f));
        CreateBlock("Chest Body", chestRoot.transform, chestPosition, new Vector3(3.2f, 1.5f, 1.8f), dark);
        CreateBlock("Chest Lid", chestRoot.transform, chestPosition + new Vector3(0f, 0.95f, 0f), new Vector3(3.45f, 0.45f, 2.05f), team);
        CreateBlock("Chest Lock", chestRoot.transform, chestPosition + new Vector3(0f, 0.15f, forward * 0.96f), new Vector3(0.55f, 0.65f, 0.16f), glow, false);

        for (int side = -1; side <= 1; side += 2)
        {
            CreateBlock($"Beacon Pole {side}", root.transform, new Vector3(side * 7.2f, 3.2f, z - (forward * 4.8f)), new Vector3(0.22f, 3.2f, 0.22f), dark, false, PrimitiveType.Cylinder);
            GameObject flag = CreateBlock($"Team Flag {side}", root.transform, new Vector3(side * 7.2f, 5.25f, z - (forward * 4.5f)), new Vector3(1.6f, 1f, 0.16f), team, false);
            flag.transform.rotation = Quaternion.Euler(0f, 0f, side * -5f);
        }

        return chestRoot;
    }

    private static void CreateShopBuilding(
        string name,
        float x,
        float z,
        bool westSide,
        float height,
        Transform parent,
        Material wall,
        Material accent,
        Material dark,
        Material glass)
    {
        GameObject root = new(name);
        root.transform.SetParent(parent);

        float streetDirection = westSide ? 1f : -1f;
        float frontX = x + (streetDirection * 4.15f);
        float backX = x - (streetDirection * 4.15f);
        float centerY = height * 0.5f;

        CreateBlock("Floor", root.transform, new Vector3(x, 0.28f, z), new Vector3(8.5f, 0.55f, 9.5f), dark);
        CreateBlock("Back Wall", root.transform, new Vector3(backX, centerY, z), new Vector3(0.55f, height, 9.5f), wall);
        CreateBlock("North Wall", root.transform, new Vector3(x, centerY, z + 4.5f), new Vector3(8.5f, height, 0.55f), wall);
        CreateBlock("South Wall", root.transform, new Vector3(x, centerY, z - 4.5f), new Vector3(8.5f, height, 0.55f), wall);
        CreateBlock("Front Panel North", root.transform, new Vector3(frontX, centerY, z + 3.2f), new Vector3(0.55f, height, 2.5f), wall);
        CreateBlock("Front Panel South", root.transform, new Vector3(frontX, centerY, z - 3.2f), new Vector3(0.55f, height, 2.5f), wall);
        CreateBlock("Door Header", root.transform, new Vector3(frontX, height - 0.6f, z), new Vector3(0.55f, 1.2f, 3.9f), wall);
        CreateBlock("Roof", root.transform, new Vector3(x, height + 0.28f, z), new Vector3(9.2f, 0.55f, 10.2f), accent);
        CreateBlock("Roof Trim", root.transform, new Vector3(frontX + (streetDirection * 0.2f), height + 0.55f, z), new Vector3(0.45f, 0.65f, 10.4f), dark, false);

        for (int side = -1; side <= 1; side += 2)
        {
            CreateBlock(
                $"Window {side}",
                root.transform,
                new Vector3(frontX + (streetDirection * 0.31f), centerY + 0.45f, z + (side * 3.15f)),
                new Vector3(0.12f, 1.65f, 1.45f),
                glass,
                false);
        }

        GameObject awning = CreateBlock(
            "Awning",
            root.transform,
            new Vector3(frontX + (streetDirection * 1.15f), height - 1.45f, z),
            new Vector3(2.6f, 0.28f, 4.6f),
            accent,
            false);
        awning.transform.rotation = Quaternion.Euler(0f, 0f, westSide ? -8f : 8f);

        CreateBlock("门口踏板", root.transform, new Vector3(frontX + (streetDirection * 0.5f), 0.42f, z),
            new Vector3(1.55f, 0.28f, 3.2f), accent);
        CreateBlock("店铺招牌", root.transform, new Vector3(frontX + (streetDirection * 0.38f), height - 0.55f, z),
            new Vector3(0.22f, 1.05f, 3.5f), accent, false);

        for (int side = -1; side <= 1; side += 2)
        {
            CreateBlock($"立面包边 {side}", root.transform,
                new Vector3(frontX + (streetDirection * 0.28f), centerY, z + (side * 4.18f)),
                new Vector3(0.28f, height + 0.3f, 0.28f), dark, false);
            CreateBlock($"室内货架 {side}", root.transform,
                new Vector3(x - (streetDirection * 1.4f), 1.45f, z + (side * 3.35f)),
                new Vector3(2.8f, 0.18f, 1.05f), dark);
            CreateBlock($"室内货物 {side}", root.transform,
                new Vector3(x - (streetDirection * 1.35f), 1.8f, z + (side * 3.35f)),
                new Vector3(1.9f, 0.5f, 0.8f), accent);
        }

        CreateBlock("收银台", root.transform,
            new Vector3(x - (streetDirection * 2.6f), 1.05f, z),
            new Vector3(1.35f, 1.5f, 3.2f), dark);
        CreateBlock("收银台面", root.transform,
            new Vector3(x - (streetDirection * 2.6f), 1.87f, z),
            new Vector3(1.55f, 0.16f, 3.45f), accent);
        CreateBlock("室内顶灯", root.transform,
            new Vector3(x, height - 0.35f, z), new Vector3(1.8f, 0.08f, 0.5f), glass, false);

        for (int side = -1; side <= 1; side += 2)
        {
            CreateBlock($"屋顶女儿墙 {side}", root.transform,
                new Vector3(x, height + 0.75f, z + (side * 4.72f)),
                new Vector3(9.2f, 0.75f, 0.25f), dark, false);
        }
    }

    private static void CreateCentralLandmark(Transform parent, Material dark, Material teal, Material glow)
    {
        CreateBlock("Plaza Ring", parent, new Vector3(0f, 0.45f, 0f), new Vector3(5.8f, 0.28f, 5.8f), dark, true, PrimitiveType.Cylinder);
        CreateBlock("Plaza Inner", parent, new Vector3(0f, 0.74f, 0f), new Vector3(4.7f, 0.12f, 4.7f), teal, true, PrimitiveType.Cylinder);
        CreateBlock("Beacon Column", parent, new Vector3(0f, 2.2f, 0f), new Vector3(0.72f, 1.55f, 0.72f), dark, true, PrimitiveType.Cylinder);
        GameObject beacon = CreateBlock("Beacon Core", parent, new Vector3(0f, 4.15f, 0f), new Vector3(1.25f, 1.25f, 1.25f), glow, false, PrimitiveType.Sphere);
        beacon.AddComponent<ShowcasePropMotion>().Configure(36f, 0.18f, 0.8f);
    }

    private static void CreateCoverLayout(Transform parent, Material dark, Material warmWhite, Material coral, Material teal)
    {
        Vector3[] lowWalls =
        {
            new(-3.8f, 0.9f, -12f), new(3.8f, 0.9f, 12f),
            new(4.4f, 0.9f, -20f), new(-4.4f, 0.9f, 20f)
        };

        for (int i = 0; i < lowWalls.Length; i++)
        {
            GameObject wall = CreateBlock($"Low Cover {i + 1}", parent, lowWalls[i], new Vector3(4.5f, 1.8f, 0.9f), i % 2 == 0 ? warmWhite : dark);
            wall.transform.rotation = Quaternion.Euler(0f, i % 2 == 0 ? 18f : -18f, 0f);
        }

        Vector3[] cratePositions =
        {
            new(-5.4f, 0.75f, -4.8f), new(5.4f, 0.75f, 4.8f),
            new(6f, 0.75f, -8f), new(-6f, 0.75f, 8f)
        };

        for (int i = 0; i < cratePositions.Length; i++)
        {
            CreateBlock($"Supply Crate {i + 1}", parent, cratePositions[i], new Vector3(1.5f, 1.5f, 1.5f), i % 2 == 0 ? coral : teal);
            CreateBlock($"Crate Strap {i + 1}", parent, cratePositions[i] + new Vector3(0f, 0.15f, 0f), new Vector3(1.62f, 0.28f, 1.62f), dark, false);
        }
    }

    private static void CreateTacticalStructures(
        Transform parent,
        Material dark,
        Material warmWhite,
        Material coral,
        Material teal,
        Material glow,
        Material glass)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 10.8f;
            float z = side * 3.5f;
            GameObject stop = new($"战术候车亭 {side}");
            stop.transform.SetParent(parent);
            CreateBlock("后挡板", stop.transform, new Vector3(x, 1.55f, z), new Vector3(0.26f, 3f, 5.8f), glass);
            CreateBlock("顶棚", stop.transform, new Vector3(x - (side * 0.9f), 3.15f, z), new Vector3(2.1f, 0.22f, 6.2f), side < 0 ? coral : teal);
            CreateBlock("长椅", stop.transform, new Vector3(x - (side * 0.75f), 0.72f, z), new Vector3(1.35f, 0.42f, 3.5f), dark);
            CreateBlock("灯牌", stop.transform, new Vector3(x - (side * 1.75f), 2.2f, z), new Vector3(0.12f, 1.4f, 1.8f), glow, false);
        }

        Vector3[] barricades =
        {
            new(-2.7f, 0.72f, -6.5f), new(2.7f, 0.72f, 6.5f),
            new(2.8f, 0.72f, -27f), new(-2.8f, 0.72f, 27f)
        };
        for (int i = 0; i < barricades.Length; i++)
        {
            Vector3 position = barricades[i];
            GameObject root = new($"模块化路障 {i + 1}");
            root.transform.SetParent(parent);
            CreateBlock("底座", root.transform, position, new Vector3(3.6f, 0.35f, 0.9f), dark);
            CreateBlock("左护板", root.transform, position + new Vector3(-1.15f, 0.62f, 0f), new Vector3(1.1f, 1.2f, 0.5f), warmWhite);
            CreateBlock("右护板", root.transform, position + new Vector3(1.15f, 0.62f, 0f), new Vector3(1.1f, 1.2f, 0.5f), warmWhite);
            CreateBlock("警示条", root.transform, position + new Vector3(0f, 0.55f, -0.28f), new Vector3(0.9f, 0.22f, 0.08f), i % 2 == 0 ? coral : teal, false);
        }
    }

    private static void CreateStreetFurniture(Transform parent, Material dark, Material teal, Material glow, Material grass)
    {
        for (int z = -24; z <= 24; z += 12)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 10.2f;
                CreateBlock($"Lamp Pole {side} {z}", parent, new Vector3(x, 2.5f, z), new Vector3(0.16f, 2.5f, 0.16f), dark, false, PrimitiveType.Cylinder);
                CreateBlock($"Lamp Head {side} {z}", parent, new Vector3(x, 5.05f, z), new Vector3(0.65f, 0.45f, 0.65f), glow, false, PrimitiveType.Sphere);
            }
        }

        Vector3[] treePositions =
        {
            new(-27f, 0f, -28f), new(27f, 0f, -28f),
            new(-27f, 0f, -12f), new(27f, 0f, -12f),
            new(-27f, 0f, 12f), new(27f, 0f, 12f),
            new(-27f, 0f, 28f), new(27f, 0f, 28f)
        };

        for (int i = 0; i < treePositions.Length; i++)
        {
            Vector3 position = treePositions[i];
            CreateBlock($"Tree Trunk {i + 1}", parent, position + new Vector3(0f, 1.25f, 0f), new Vector3(0.45f, 1.25f, 0.45f), dark, true, PrimitiveType.Cylinder);
            CreateBlock($"Tree Crown {i + 1}", parent, position + new Vector3(0f, 3.25f, 0f), new Vector3(2.2f, 2.4f, 2.2f), i % 2 == 0 ? grass : teal, false, PrimitiveType.Sphere);
        }
    }

    private static void CreateSupplyPickup(
        string name,
        Vector3 position,
        Transform parent,
        SupplyKind kind,
        string prefabPath,
        GameBalanceConfig balance,
        Material padMaterial)
    {
        GameObject root = new(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        CreateBlock("拾取底座", root.transform, position - new Vector3(0f, 0.74f, 0f),
            new Vector3(1.45f, 0.08f, 1.45f), padMaterial, false, PrimitiveType.Cylinder);
        GameObject visual = InstantiatePrefab(prefabPath, root.transform);
        visual.name = "悬浮道具模型";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale *= 1.15f;
        visual.AddComponent<ShowcasePropMotion>().Configure(55f, 0.18f, 1f);
        WorldItemPickup pickup = root.AddComponent<WorldItemPickup>();
        pickup.ConfigureSupply(kind, 1, balance != null ? balance.PickupRespawnSeconds : 22f, visual);
        SphereCollider trigger = root.GetComponent<SphereCollider>();
        trigger.radius = balance != null ? balance.PickupRadius : 1.35f;
    }

    private static void CreateWeaponPickup(
        string name,
        Vector3 position,
        Transform parent,
        WeaponConfig config,
        GameBalanceConfig balance)
    {
        if (config == null || config.ModelPrefab == null)
        {
            return;
        }

        GameObject root = new(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        Material padMaterial = GetOrCreateEmissiveMaterial(
            $"{MaterialsRoot}/Pickup_{config.Rarity}.mat",
            config.RarityColor * 0.75f,
            config.RarityColor * 2.2f);
        CreateBlock("武器底座", root.transform, position - new Vector3(0f, 0.72f, 0f),
            new Vector3(1.7f, 0.08f, 1.7f), padMaterial, false, PrimitiveType.Cylinder);
        GameObject visual = PrefabUtility.InstantiatePrefab(config.ModelPrefab) as GameObject;
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        visual.transform.localScale *= 1.2f;
        visual.AddComponent<ShowcasePropMotion>().Configure(42f, 0.2f, 1.15f);
        WorldItemPickup pickup = root.AddComponent<WorldItemPickup>();
        pickup.ConfigureWeapon(config, balance != null ? balance.PickupRespawnSeconds : 22f, visual);
        SphereCollider trigger = root.GetComponent<SphereCollider>();
        trigger.radius = balance != null ? balance.PickupRadius : 1.35f;
    }

    private static GameObject InstantiatePrefab(string path, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Missing generated prefab: {path}");
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static GameObject CreateShowcasePlayer(Vector3 position, GameRuleConfig rules)
    {
        GameObject player = new("蓝方玩家");
        player.transform.position = position;
        player.layer = LayerMask.NameToLayer("Ignore Raycast");

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 2.75f;
        characterController.radius = 0.48f;
        characterController.center = new Vector3(0f, 1.35f, 0f);

        Damageable damageable = player.AddComponent<Damageable>();
        damageable.Configure(rules.PlayerMaxHealth, rules.PlayerMaxArmor, false, 0f);
        player.AddComponent<TeamMember>().Configure(TeamId.Blue);

        GameObject visual = InstantiateCharacterVisual(CharacterArtBuilder.BluePrefabPath, player.transform);
        SetLayerRecursively(visual, player.layer);
        visual.GetComponent<CharacterCombatFeedback>()?.Bind(damageable);

        return player;
    }

    private static GameObject CreateBot(
        string name,
        Vector3 position,
        Transform parent,
        GameRuleConfig rules,
        MatchPhaseController match,
        TeamId team,
        float startingArmor,
        WeaponConfig weaponConfig)
    {
        GameObject target = new(name);
        target.transform.SetPositionAndRotation(
            position,
            Quaternion.Euler(0f, team == TeamId.Blue ? 0f : 180f, 0f));
        target.transform.SetParent(parent, true);

        CharacterController characterController = target.AddComponent<CharacterController>();
        characterController.height = 2.75f;
        characterController.radius = 0.48f;
        characterController.center = new Vector3(0f, 1.35f, 0f);

        Damageable damageable = target.AddComponent<Damageable>();
        damageable.Configure(rules.PlayerMaxHealth, rules.PlayerMaxArmor, false, startingArmor);
        target.AddComponent<TeamMember>().Configure(team);

        string visualPath = team == TeamId.Blue ? CharacterArtBuilder.BluePrefabPath : CharacterArtBuilder.RedPrefabPath;
        GameObject visual = InstantiateCharacterVisual(visualPath, target.transform);
        CharacterCombatFeedback feedback = visual.GetComponent<CharacterCombatFeedback>();
        feedback?.Bind(damageable);
        BlockCharacterVisual characterVisual = visual.GetComponent<BlockCharacterVisual>();
        Transform muzzle = characterVisual != null ? characterVisual.Muzzle : null;

        if (weaponConfig != null && weaponConfig.ModelPrefab != null && characterVisual != null && characterVisual.WeaponSocket != null)
        {
            GameObject weaponModel = PrefabUtility.InstantiatePrefab(weaponConfig.ModelPrefab) as GameObject;
            weaponModel.transform.SetParent(characterVisual.WeaponSocket, false);
            WeaponModelBinding binding = weaponModel.GetComponent<WeaponModelBinding>();
            if (binding != null && binding.Muzzle != null)
            {
                muzzle = binding.Muzzle;
            }
        }

        PrototypeWeaponEffects effects = target.AddComponent<PrototypeWeaponEffects>();
        effects.Configure(null, muzzle);
        PrototypeEnemyAI ai = target.AddComponent<PrototypeEnemyAI>();
        ai.Configure(muzzle);
        CombatantLifecycle lifecycle = target.AddComponent<CombatantLifecycle>();
        lifecycle.Configure(match, weaponConfig, null, null, false);
        return target;
    }

    private static GameObject InstantiateCharacterVisual(string assetPath, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Character prefab was not generated: {assetPath}");
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static GameObject CreateBlock(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Material material,
        bool keepCollider = true,
        PrimitiveType primitiveType = PrimitiveType.Cube)
    {
        GameObject result = CreatePrimitive(name, primitiveType, position, scale, material);
        result.transform.SetParent(parent, true);

        if (!keepCollider)
        {
            Collider collider = result.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        return result;
    }

    private static Camera CreateCamera(Vector3 position, Quaternion rotation, bool addThirdPersonRig)
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetPositionAndRotation(position, rotation);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.35f, 0.68f, 0.9f);
        cameraObject.AddComponent<AudioListener>();

        if (addThirdPersonRig)
        {
            cameraObject.AddComponent<ThirdPersonCameraRig>();
        }

        return camera;
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.6f);
    }

    private static GameObject CreatePrimitive(
        string name,
        PrimitiveType primitiveType,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        GameObject result = GameObject.CreatePrimitive(primitiveType);
        result.name = name;
        result.transform.position = position;
        result.transform.localScale = scale;
        Renderer renderer = result.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        return result;
    }

    private static void CreateSceneLabel(string title, string message)
    {
        GameObject labelObject = new("SceneLabel");
        labelObject.AddComponent<PrototypeSceneLabel>().Configure(title, message);
    }

    private static void CreateSceneIfNeeded(string sceneName, bool overwrite, Action<Scene> populate)
    {
        string path = $"{ScenesRoot}/{sceneName}.unity";
        if (!overwrite && AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        populate(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);
    }

    private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
        {
            return existing;
        }

        T asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new(shader)
        {
            color = color
        };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material GetOrCreateEmissiveMaterial(string path, Color color, Color emission)
    {
        Material material = GetOrCreateMaterial(path, color);
        material.SetColor("_BaseColor", color);
        material.SetColor("_EmissionColor", emission);
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreateTransparentMaterial(string path, Color color)
    {
        Material material = GetOrCreateMaterial(path, color);
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
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

    private static void ConfigureProjectSettings()
    {
        PlayerSettings.companyName = "hankjia2005-ux";
        PlayerSettings.productName = "HoldPoint Showdown";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.runInBackground = true;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultIsNativeResolution = true;
        EditorSettings.serializationMode = SerializationMode.ForceText;
        EditorSettings.lineEndingsForNewScripts = LineEndingsMode.Unix;
    }

        private static void CleanupTemplateAssets()
        {
            foreach (string path in new[]
                     {
                         "Assets/TutorialInfo",
                         "Assets/Scenes",
                         "Assets/Readme.asset",
                         "Assets/Settings/SampleSceneProfile.asset"
                     })
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null || AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        private static void ConfigureBuildScenes()
        {
            List<EditorBuildSettingsScene> scenes = new();
            foreach (string sceneName in new[] { "Bootstrap", "MainMenu", "TestGym", "ArenaTown" })
            {
                scenes.Add(new EditorBuildSettingsScene($"{ScenesRoot}/{sceneName}.unity", true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
