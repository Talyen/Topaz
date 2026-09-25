using System;
using System.Linq;
using Topaz.AnimationStudy;
using Topaz.Menus;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Authors a small isolated campfire clearing for full-screen menus.</summary>
    public static class MainMenuStageSetup
    {
        const string KayKit = "Assets/ThirdParty/KayKit/";
        const string Materials = "Assets/Topaz/Presentation/Art/Materials/";

        internal static MainMenuStage Configure(Scene scene, GameObject player,
            PlayerAppearance appearance)
        {
            foreach (Transform existing in UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsSortMode.None).Where(value => value.parent == null &&
                    value.gameObject.scene == scene &&
                    (value.name == "Character Preview Stage" || value.name == "Main Menu Stage"))
                .ToArray())
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            var root = new GameObject("Main Menu Stage");
            root.transform.position = new Vector3(1000f, 0f, 1000f);
            Material dirt = Load<Material>(Materials + "Dungeon.mat");
            Material forest = Load<Material>(Materials + "Forest.mat");
            Material wood = Load<Material>(Materials + "Resources.mat");
            GameObject floor = Load<GameObject>(KayKit + "Dungeon/Models/floor_dirt_large.fbx");
            for (int z = -2; z <= 2; z++)
            for (int x = -3; x <= 3; x++)
            {
                GameObject tile = Place(floor, root.transform, dirt, $"Dirt {x}-{z}",
                    new Vector3(x * 4f, -.105f, z * 4f), Vector3.one,
                    90f * ((x + z + 8) % 4));
                tile.layer = 5;
            }

            PlaceAtHeight("Forest/Models/Tree_4_B_Color1.fbx", root.transform, forest,
                "Right Pine", new Vector3(6.1f, 0f, 4.4f), 5f, -35f);
            PlaceAtHeight("Forest/Models/Rock_3_B_Color1.fbx", root.transform, forest,
                "Camp Rock", new Vector3(-1.4f, 0f, 1.2f), 1f, 21f);
            PlaceAtHeight("Forest/Models/Rock_3_A_Color1.fbx", root.transform, forest,
                "Right Rock", new Vector3(5.6f, 0f, -1.2f), .8f, -15f);
            PlaceAtHeight("Forest/Models/Grass_1_A_Color1.fbx", root.transform, forest,
                "Left Grass", new Vector3(-2.7f, 0f, -2.6f), .42f, 72f);
            PlaceAtHeight("Forest/Models/Grass_1_A_Color1.fbx", root.transform, forest,
                "Right Grass", new Vector3(4.4f, 0f, 1.7f), .42f, -32f);

            var camp = new GameObject("Campfire");
            camp.transform.SetParent(root.transform, false);
            camp.transform.localPosition = new Vector3(.8f, 0f, -.65f);
            for (int i = 0; i < 3; i++)
            {
                GameObject log = Place(Load<GameObject>(KayKit +
                    (i == 1 ? "ResourceBits/Models/Wood_Log_B.fbx" :
                        "ResourceBits/Models/Wood_Log_A.fbx")),
                    camp.transform, wood, "Firewood " + (i + 1),
                    new Vector3((i - 1) * .15f, .08f, 0f), Vector3.one * .62f,
                    i * 60f);
                SetLayer(log.transform, 5);
            }
            BuildFlame(camp.transform);
            BuildLight("Campfire Glow", camp.transform, new Vector3(0f, .75f, 0f),
                3f, 5.8f, new Color(1f, .47f, .20f));
            BuildLight("Character Warm Key", root.transform,
                new Vector3(3.8f, 2.2f, -1.3f), 4f, 6f,
                new Color(1f, .66f, .40f));

            var actorAnchor = new GameObject("Character Preview Stage");
            actorAnchor.transform.SetParent(root.transform, false);
            actorAnchor.transform.localPosition = new Vector3(3.35f, 0f, .15f);
            actorAnchor.layer = 5;

            var cameraObject = new GameObject("Main Menu Camera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 6.5f, -8.7f);
            cameraObject.transform.LookAt(root.transform.position + new Vector3(.5f, .35f, 0f));
            Camera menuCamera = cameraObject.GetComponent<Camera>();
            menuCamera.orthographic = true;
            menuCamera.orthographicSize = 4f;
            menuCamera.nearClipPlane = .1f;
            menuCamera.farClipPlane = 45f;
            menuCamera.cullingMask = 1 << 5;
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = new Color(.13f, .12f, .09f);
            menuCamera.enabled = false;
            UniversalAdditionalCameraData menuData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            menuData.renderPostProcessing = true;
            menuData.volumeLayerMask = 1 << 0;
            menuData.antialiasing = AntialiasingMode.TemporalAntiAliasing;

            Camera gameplayCamera = Camera.main;
            UniversalAdditionalCameraData gameplayData = gameplayCamera != null
                ? gameplayCamera.GetComponent<UniversalAdditionalCameraData>() : null;
            Transform homeTrigger = GameObject.Find("Workbench")?.transform;
            if (gameplayCamera == null || gameplayData == null || homeTrigger == null)
                throw new InvalidOperationException("Gameplay camera or home Volume trigger is missing.");
            MainMenuStage stage = root.AddComponent<MainMenuStage>();
            Ref(stage, "menuCamera", menuCamera);
            Ref(stage, "gameplayCamera", gameplayCamera);
            Ref(stage, "menuCameraData", menuData);
            Ref(stage, "gameplayCameraData", gameplayData);
            Ref(stage, "volumeTrigger", homeTrigger);
            Ref(stage, "actorAnchor", actorAnchor.transform);
            Ref(stage, "appearance", appearance);
            WarmRefugeStageSetup.ApplyToStage(root.transform);
            return stage;
        }

        static void BuildFlame(Transform camp)
        {
            var flame = new GameObject("Campfire Flames", typeof(ParticleSystem));
            flame.transform.SetParent(camp, false);
            flame.transform.localPosition = new Vector3(0f, .19f, 0f);
            flame.layer = 5;
            ParticleSystem particles = flame.GetComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.35f, .8f);
            main.startSize = new ParticleSystem.MinMaxCurve(.11f, .24f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, .45f, .08f, .55f), new Color(1f, .78f, .25f, .75f));
            main.maxParticles = 45;
            main.useUnscaledTime = true;
            var emission = particles.emission;
            emission.rateOverTime = 14f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = .18f;
            shape.angle = 15f;
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Load<Material>(
                "Assets/Topaz/Presentation/Effects/Resources/TopazEffectsParticles.mat");
        }

        static void BuildLight(string name, Transform parent, Vector3 position,
            float intensity, float range, Color color)
        {
            var objectWithLight = new GameObject(name, typeof(Light));
            objectWithLight.transform.SetParent(parent, false);
            objectWithLight.transform.localPosition = position;
            objectWithLight.layer = 5;
            Light light = objectWithLight.GetComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = range;
            light.color = color;
            light.cullingMask = 1 << 5;
            light.shadows = LightShadows.None;
        }

        static GameObject PlaceAtHeight(string path, Transform parent, Material material,
            string name, Vector3 position, float height, float yaw)
        {
            GameObject model = Place(Load<GameObject>(KayKit + path), parent, material,
                name, position, Vector3.one, yaw);
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y > .001f) model.transform.localScale *= height / bounds.size.y;
            SetLayer(model.transform, 5);
            return model;
        }

        static GameObject Place(GameObject asset, Transform parent, Material material,
            string name, Vector3 position, Vector3 scale, float yaw)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not place " + asset.name);
            instance.name = name;
            instance.transform.SetParent(parent, false);
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

        static void Ref(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            if (property == null) throw new InvalidOperationException("Missing " + field + " on " + target.name);
            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
