using System;
using Topaz.Expedition;
using Topaz.LoopStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Adds the two authored recovery points to the current home and clearing scenes.</summary>
    public static class CampfireSetup
    {
        const string HomeScene = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string ClearingScene = "Assets/Topaz/World/Scenes/Expedition.unity";

        [MenuItem("Topaz/Build Campfires")]
        public static void Configure()
        {
            Material wood = Load<Material>("Assets/Topaz/Gameplay/WorldLoop/Materials/Woodwork.mat");
            Material stone = Load<Material>("Assets/Topaz/World/Expedition/Materials/Clearing Rock.mat");
            Material ember = Load<Material>("Assets/Topaz/Presentation/Effects/Materials/Lantern Ember.mat");
            Material particle = Load<Material>(
                "Assets/Topaz/Presentation/Effects/Resources/TopazEffectsParticles.mat");

            Scene homeScene = EditorSceneManager.OpenScene(HomeScene, OpenSceneMode.Single);
            GameObject homeRoot = GameObject.Find("World Loop Study");
            GameObject player = GameObject.Find("Player");
            if (homeRoot == null || player == null)
                throw new InvalidOperationException("The home scene is missing its world loop or player.");
            RemoveOld("Home Campfire");
            Campfire home = Create("Home Campfire", homeRoot.transform,
                new Vector3(-1.2f, 0f, 1f), new Vector3(0f, 0f, .35f),
                Campfire.HomeId, TopazSaveData.HomeRegion, wood, stone, ember, particle);
            Set(player.GetComponent<WorldSession>(), "homeCampfire", home);
            EditorSceneManager.MarkSceneDirty(homeScene);
            EditorSceneManager.SaveScene(homeScene);

            Scene clearingScene = EditorSceneManager.OpenScene(ClearingScene, OpenSceneMode.Single);
            GameObject clearingRoot = GameObject.Find("Expedition Clearing");
            if (clearingRoot == null)
                throw new InvalidOperationException("The expedition clearing is missing.");
            RemoveOld("Clearing Campfire");
            Campfire clearing = Create("Clearing Campfire", clearingRoot.transform,
                new Vector3(96f, 0f, -14f), new Vector3(96f, 0f, -15.25f),
                Campfire.ClearingId, TopazSaveData.ExpeditionRegion,
                wood, stone, ember, particle);
            Set(clearingRoot.GetComponent<ExpeditionSceneBootstrap>(), "campfire", clearing);
            EditorSceneManager.MarkSceneDirty(clearingScene);
            EditorSceneManager.SaveScene(clearingScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Home and clearing Campfires are authored and connected.");
        }

        public static Campfire Create(string name, Transform parent, Vector3 position, Vector3 arrival,
            string id, string region, Material wood, Material stone, Material ember,
            Material particle)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI * 2f / 10f;
                GameObject rock = Primitive("Hearth Stone", PrimitiveType.Cube, root.transform,
                    new Vector3(Mathf.Cos(angle) * .55f, .08f, Mathf.Sin(angle) * .55f),
                    new Vector3(.31f, .16f, .25f), stone);
                rock.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }
            for (int i = 0; i < 3; i++)
            {
                GameObject log = Primitive("Fire Log", PrimitiveType.Cylinder, root.transform,
                    new Vector3(0f, .13f, 0f), new Vector3(.13f, .42f, .13f), wood);
                log.transform.localRotation = Quaternion.Euler(90f, i * 60f, 0f);
            }
            Primitive("Ember Core", PrimitiveType.Sphere, root.transform,
                new Vector3(0f, .28f, 0f), new Vector3(.29f, .19f, .29f), ember);

            var ringObject = new GameObject("Activation Ring");
            ringObject.transform.SetParent(root.transform, false);
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.sharedMaterial = ember;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.widthMultiplier = .045f;
            ring.positionCount = 48;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * 1.6f,
                    .035f, Mathf.Sin(angle) * 1.6f));
            }

            var flame = new GameObject("Flame and Embers");
            flame.transform.SetParent(root.transform, false);
            flame.transform.localPosition = new Vector3(0f, .3f, 0f);
            ParticleSystem embers = flame.AddComponent<ParticleSystem>();
            var main = embers.main;
            main.loop = true;
            main.startLifetime = .7f;
            main.startSpeed = .7f;
            main.startSize = .16f;
            main.startColor = new Color(1f, .58f, .18f, .9f);
            main.maxParticles = 48;
            var emission = embers.emission;
            emission.rateOverTime = 9f;
            var shape = embers.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = .18f;
            var renderer = flame.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particle;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var lightObject = new GameObject("Fire Glow");
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, .55f, 0f);
            Light glow = lightObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, .58f, .28f);
            glow.intensity = 1.35f;
            glow.range = 8f;
            glow.shadows = LightShadows.None;

            Transform arrivalPoint = new GameObject("Safe Arrival").transform;
            arrivalPoint.SetParent(root.transform, false);
            arrivalPoint.position = arrival;
            Campfire campfire = root.AddComponent<Campfire>();
            Set(campfire, "stableId", id);
            Set(campfire, "regionId", region);
            Set(campfire, "arrival", arrivalPoint);
            Set(campfire, "embers", embers);
            return campfire;
        }

        static GameObject Primitive(string name, PrimitiveType type, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject result = GameObject.CreatePrimitive(type);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = localPosition;
            result.transform.localScale = localScale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(result.GetComponent<Collider>());
            return result;
        }

        static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Missing Campfire material: " + path);

        static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            if (target == null) throw new InvalidOperationException("Missing Campfire owner.");
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object target, string name, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void RemoveOld(string name)
        {
            GameObject old = GameObject.Find(name);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
        }
    }
}
