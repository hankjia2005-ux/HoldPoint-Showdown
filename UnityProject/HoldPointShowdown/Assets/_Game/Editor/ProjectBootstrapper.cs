using System;
using System.Collections.Generic;
using HoldPointShowdown.Combat;
using HoldPointShowdown.Data;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Player;
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
        WeaponConfig pistol = GetOrCreateAsset<WeaponConfig>(DataRoot + "/PrototypePistol.asset");
        pistol.SetPrototypeValues("Prototype Pistol", 20f, 4f, 12, 30, 80f);
        EditorUtility.SetDirty(pistol);

        Material blue = GetOrCreateMaterial(MaterialsRoot + "/TeamBlue.mat", new Color(0.16f, 0.64f, 1f));
        Material red = GetOrCreateMaterial(MaterialsRoot + "/TeamRed.mat", new Color(1f, 0.25f, 0.22f));
        Material neutral = GetOrCreateMaterial(MaterialsRoot + "/NeutralTown.mat", new Color(0.78f, 0.82f, 0.84f));
        Material dark = GetOrCreateMaterial(MaterialsRoot + "/DarkAccent.mat", new Color(0.12f, 0.15f, 0.2f));

        CreateSceneIfNeeded("Bootstrap", overwriteScenes, scene => CreateBootstrapScene());
        CreateSceneIfNeeded("MainMenu", overwriteScenes, scene => CreateMainMenuScene());
        CreateSceneIfNeeded("TestGym", overwriteScenes, scene => CreateTestGymScene(rules, pistol, blue, red, neutral, dark));
        CreateSceneIfNeeded("ArenaTown", overwriteScenes, scene => CreateArenaTownScene(blue, red, neutral, dark));

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
