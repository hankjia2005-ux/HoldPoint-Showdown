using System.Collections.Generic;
using HoldPointShowdown.Presentation;
using UnityEditor;
using UnityEngine;

namespace HoldPointShowdown.Editor
{
    public static class CharacterArtBuilder
    {
        public const string BluePrefabPath = "Assets/_Game/Art/Characters/Prefabs/BlueBlockCharacter.prefab";
        public const string RedPrefabPath = "Assets/_Game/Art/Characters/Prefabs/RedBlockCharacter.prefab";

        private const string CharacterRoot = "Assets/_Game/Art/Characters";
        private const string MaterialRoot = CharacterRoot + "/Materials";
        private const string PrefabRoot = CharacterRoot + "/Prefabs";

        [MenuItem("HoldPoint Showdown/Art/Rebuild Block Characters")]
        public static void BuildAssets()
        {
            EnsureFolder(CharacterRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(PrefabRoot);

            Material blue = GetOrCreateMaterial(MaterialRoot + "/CharacterBlue.mat", new Color(0.12f, 0.62f, 1f));
            Material blueAccent = GetOrCreateMaterial(MaterialRoot + "/CharacterBlueAccent.mat", new Color(0.35f, 0.82f, 1f));
            Material red = GetOrCreateMaterial(MaterialRoot + "/CharacterRed.mat", new Color(1f, 0.24f, 0.2f));
            Material redAccent = GetOrCreateMaterial(MaterialRoot + "/CharacterRedAccent.mat", new Color(1f, 0.5f, 0.38f));
            Material skin = GetOrCreateMaterial(MaterialRoot + "/CharacterSkin.mat", new Color(1f, 0.83f, 0.62f));
            Material hair = GetOrCreateMaterial(MaterialRoot + "/CharacterHair.mat", new Color(0.27f, 0.12f, 0.055f));
            Material dark = GetOrCreateMaterial(MaterialRoot + "/CharacterDark.mat", new Color(0.045f, 0.075f, 0.12f));
            Material white = GetOrCreateMaterial(MaterialRoot + "/CharacterWhite.mat", new Color(0.95f, 0.98f, 1f));
            Material mouth = GetOrCreateMaterial(MaterialRoot + "/CharacterMouth.mat", new Color(0.95f, 0.24f, 0.26f));
            Material muzzle = GetOrCreateEmissiveMaterial(
                MaterialRoot + "/CharacterMuzzle.mat",
                new Color(1f, 0.65f, 0.08f),
                new Color(2.5f, 1.1f, 0.08f));

            BuildCharacterPrefab("蓝方方块小人", BluePrefabPath, blue, blueAccent, skin, hair, dark, white, mouth, muzzle);
            BuildCharacterPrefab("红方方块小人", RedPrefabPath, red, redAccent, skin, hair, dark, white, mouth, muzzle);
            AssetDatabase.SaveAssets();
            Debug.Log("方块小人美术资产已重建：红蓝阵营 Prefab、材质、关节与枪口挂点。 ");
        }

        private static void BuildCharacterPrefab(
            string name,
            string path,
            Material team,
            Material accent,
            Material skin,
            Material hair,
            Material dark,
            Material white,
            Material mouth,
            Material muzzleMaterial)
        {
            GameObject root = new(name);
            GameObject model = new("模型");
            model.transform.SetParent(root.transform, false);
            List<Renderer> renderers = new();

            CreatePart("躯干", model.transform, new Vector3(0f, 1.42f, 0f), new Vector3(0.92f, 0.88f, 0.58f), team, renderers);
            CreatePart("腰带", model.transform, new Vector3(0f, 0.98f, 0f), new Vector3(0.94f, 0.18f, 0.6f), dark, renderers);
            CreatePart("胸前装饰1", model.transform, new Vector3(0f, 1.62f, 0.301f), new Vector3(0.48f, 0.08f, 0.025f), white, renderers);
            CreatePart("胸前装饰2", model.transform, new Vector3(0f, 1.46f, 0.301f), new Vector3(0.48f, 0.08f, 0.025f), white, renderers);
            CreatePart("胸前装饰3", model.transform, new Vector3(0f, 1.30f, 0.301f), new Vector3(0.48f, 0.08f, 0.025f), white, renderers);

            CreatePart("头", model.transform, new Vector3(0f, 2.22f, 0f), new Vector3(0.9f, 0.78f, 0.8f), skin, renderers);
            CreatePart("头发顶部", model.transform, new Vector3(0f, 2.66f, -0.01f), new Vector3(0.96f, 0.18f, 0.85f), hair, renderers);
            CreatePart("头发后侧", model.transform, new Vector3(0f, 2.31f, -0.40f), new Vector3(0.96f, 0.55f, 0.14f), hair, renderers);
            CreatePart("左侧刘海", model.transform, new Vector3(-0.34f, 2.45f, 0.405f), new Vector3(0.24f, 0.24f, 0.06f), hair, renderers);
            CreatePart("右侧刘海", model.transform, new Vector3(0.27f, 2.50f, 0.405f), new Vector3(0.34f, 0.16f, 0.06f), hair, renderers);
            CreatePart("左眼", model.transform, new Vector3(-0.21f, 2.27f, 0.418f), new Vector3(0.085f, 0.09f, 0.04f), dark, renderers);
            CreatePart("右眼", model.transform, new Vector3(0.21f, 2.27f, 0.418f), new Vector3(0.085f, 0.09f, 0.04f), dark, renderers);
            CreatePart("嘴巴", model.transform, new Vector3(0f, 2.06f, 0.422f), new Vector3(0.16f, 0.075f, 0.04f), mouth, renderers);

            Transform leftArmPivot = CreatePivot("左臂关节", model.transform, new Vector3(-0.57f, 1.72f, 0f));
            CreatePart("左上臂", leftArmPivot, new Vector3(0f, -0.28f, 0f), new Vector3(0.26f, 0.58f, 0.3f), team, renderers);
            CreatePart("左手", leftArmPivot, new Vector3(0f, -0.64f, 0f), new Vector3(0.27f, 0.22f, 0.29f), skin, renderers);

            Transform rightArmPivot = CreatePivot("右臂关节", model.transform, new Vector3(0.57f, 1.72f, 0f));
            CreatePart("右上臂", rightArmPivot, new Vector3(0f, -0.28f, 0f), new Vector3(0.26f, 0.58f, 0.3f), accent, renderers);
            CreatePart("右手", rightArmPivot, new Vector3(0f, -0.64f, 0.02f), new Vector3(0.27f, 0.22f, 0.29f), skin, renderers);
            Transform weaponSocket = CreatePivot("武器挂点", rightArmPivot, new Vector3(0f, -0.53f, 0.12f));
            Transform muzzle = CreatePivot("备用枪口挂点", weaponSocket, new Vector3(0f, 0f, 0.9f));

            Transform leftLegPivot = CreatePivot("左腿关节", model.transform, new Vector3(-0.24f, 0.92f, 0f));
            CreatePart("左腿", leftLegPivot, new Vector3(0f, -0.38f, 0f), new Vector3(0.34f, 0.76f, 0.38f), skin, renderers);
            CreatePart("左鞋", leftLegPivot, new Vector3(0f, -0.78f, 0.08f), new Vector3(0.42f, 0.18f, 0.58f), accent, renderers);

            Transform rightLegPivot = CreatePivot("右腿关节", model.transform, new Vector3(0.24f, 0.92f, 0f));
            CreatePart("右腿", rightLegPivot, new Vector3(0f, -0.38f, 0f), new Vector3(0.34f, 0.76f, 0.38f), skin, renderers);
            CreatePart("右鞋", rightLegPivot, new Vector3(0f, -0.78f, 0.08f), new Vector3(0.42f, 0.18f, 0.58f), accent, renderers);

            CreatePart("背包", model.transform, new Vector3(0f, 1.42f, -0.39f), new Vector3(0.58f, 0.62f, 0.2f), dark, renderers);

            CapsuleCollider damageHitbox = root.AddComponent<CapsuleCollider>();
            damageHitbox.center = new Vector3(0f, 1.35f, 0f);
            damageHitbox.height = 2.8f;
            damageHitbox.radius = 0.72f;
            damageHitbox.isTrigger = true;

            BlockCharacterVisual visual = root.AddComponent<BlockCharacterVisual>();
            visual.Configure(model.transform, leftArmPivot, rightArmPivot, leftLegPivot, rightLegPivot, muzzle, weaponSocket, renderers.ToArray());
            root.AddComponent<CharacterCombatFeedback>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static Transform CreatePivot(string name, Transform parent, Vector3 localPosition)
        {
            GameObject pivot = new(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPosition;
            return pivot.transform;
        }

        private static GameObject CreatePart(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            List<Renderer> renderers,
            Vector3? localEuler = null)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = Quaternion.Euler(localEuler ?? Vector3.zero);
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderers.Add(renderer);
            return part;
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.18f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateEmissiveMaterial(string path, Color color, Color emission)
        {
            Material material = GetOrCreateMaterial(path, color);
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
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
    }
}
