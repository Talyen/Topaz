using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Frames the real KayKit homestead vocabulary behind the title.</summary>
    public static class WarmRefugeStageSetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string KayKit = "Assets/ThirdParty/KayKit/";
        const string Materials = "Assets/Topaz/Presentation/Art/Materials/";

        [MenuItem("Topaz/Frame Warm Refuge Menu Stage")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform stage = GameObject.Find("Main Menu Stage")?.transform;
            if (stage == null) throw new InvalidOperationException("Main Menu Stage is missing.");
            ApplyToStage(stage);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Topaz] Warm refuge menu stage framed around the campfire.");
        }

        internal static void ApplyToStage(Transform stage)
        {
            Material dungeon = Load<Material>(Materials + "Dungeon.mat");
            for (int z = 3; z <= 4; z++)
                for (int x = -3; x <= 3; x++)
                    EnsureModel(stage, $"Refuge Dirt {x}-{z}",
                        KayKit + "Dungeon/Models/floor_dirt_large.fbx", dungeon,
                        new Vector3(x * 4f, -.105f, z * 4f), Vector3.one,
                        90f * ((x + z + 8) % 4));
            Transform actor = stage.Find("Character Preview Stage");
            if (actor != null) actor.localPosition = new Vector3(4.15f, 0f, .3f);
            Transform camp = stage.Find("Campfire");
            if (camp != null)
            {
                camp.localPosition = new Vector3(1.75f, 0f, -.1f);
                foreach (Transform child in camp)
                    if (child.name.StartsWith("Firewood ")) child.localScale = Vector3.one * 1.08f;
                ParticleSystem flames = camp.Find("Campfire Flames")?.GetComponent<ParticleSystem>();
                if (flames != null)
                {
                    var main = flames.main;
                    main.startSize = new ParticleSystem.MinMaxCurve(.18f, .32f);
                }
                Light glow = camp.Find("Campfire Glow")?.GetComponent<Light>();
                if (glow != null) { glow.range = 7.2f; glow.intensity = 3.2f; }
                Material resources = Load<Material>(Materials + "Resources.mat");
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * Mathf.PI * 2f / 6f;
                    EnsureModel(camp, "Hearth Stone " + (i + 1),
                        KayKit + "ResourceBits/Models/Stone_Chunks_Small.fbx", resources,
                        new Vector3(Mathf.Cos(angle) * .6f, -.01f, Mathf.Sin(angle) * .6f),
                        Vector3.one * .34f, i * 48f);
                }
            }

            Material resourcesMaterial = Load<Material>(Materials + "Resources.mat");
            Material tools = Load<Material>(Materials + "Tools.mat");
            Material forest = Load<Material>(Materials + "Forest.mat");
            EnsureModel(stage, "Home Table", KayKit + "Dungeon/Models/table_small.fbx",
                dungeon, new Vector3(6.1f, 0f, -.95f), Vector3.one * 1.35f, -18f);
            EnsureModel(stage, "Field Lantern", KayKit + "RPGTools/Models/lantern.fbx",
                tools, new Vector3(6.15f, 1.06f, -.95f), Vector3.one * .8f, -18f);
            EnsureModel(stage, "Wood Stores", KayKit + "ResourceBits/Models/Wood_Log_Stack.fbx",
                resourcesMaterial, new Vector3(6.7f, 0f, .7f), Vector3.one * .85f, 25f);
            EnsureModel(stage, "Storage Barrel", KayKit + "Dungeon/Models/barrel_small.fbx",
                dungeon, new Vector3(5.45f, 0f, .4f), Vector3.one * .75f, 12f);
            EnsureModel(stage, "Back Pine", KayKit + "Forest/Models/Tree_3_B_Color1.fbx",
                forest, new Vector3(7.6f, 0f, 3.2f), Vector3.one * .52f, -28f);
            EnsureModel(stage, "Hearth Shrub", KayKit + "Forest/Models/Bush_2_A_Color1.fbx",
                forest, new Vector3(3f, 0f, 3.15f), Vector3.one * .8f, 34f);
            Transform rightPine = stage.Find("Right Pine");
            if (rightPine != null) rightPine.localPosition = new Vector3(5.5f, 0f, 4.1f);

            Camera camera = stage.Find("Main Menu Camera")?.GetComponent<Camera>();
            if (camera == null) throw new InvalidOperationException("Main Menu Camera is missing.");
            camera.transform.localPosition = new Vector3(1.4f, 7.5f, -10.5f);
            camera.transform.LookAt(stage.TransformPoint(new Vector3(2.5f, .45f, .55f)));
            camera.orthographicSize = 5.1f;
        }

        static GameObject EnsureModel(Transform parent, string name, string path,
            Material material, Vector3 position, Vector3 scale, float yaw)
        {
            Transform existing = parent.Find(name);
            GameObject instance;
            if (existing != null) instance = existing.gameObject;
            else
            {
                GameObject asset = Load<GameObject>(path);
                instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not place " + path);
                instance.name = name;
                instance.transform.SetParent(parent, false);
            }
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = scale;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Enumerable.Repeat(material,
                    Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            SetLayer(instance.transform, 5);
            return instance;
        }

        static void SetLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayer(child, layer);
        }

        static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Menu stage asset is missing: " + path);
    }
}
