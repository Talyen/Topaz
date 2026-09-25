using System;
using System.Linq;
using Topaz.AnimationStudy;
using Topaz.CombatStudy;
using Topaz.FeelStudy;
using Topaz.LoopStudy;
using Topaz.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Adds mining and repeatable home builds without rebuilding existing gameplay.</summary>
    public static class MiningAndHomeSetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string Definitions = "Assets/Topaz/Gameplay/WorldLoop/Definitions/";
        const string Materials = "Assets/Topaz/Gameplay/WorldLoop/Materials/";

        [MenuItem("Topaz/Capture Mining And Home Review")]
        public static void CaptureReview()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            HomeBuilds builds = UnityEngine.Object.FindFirstObjectByType<HomeBuilds>();
            if (builds == null) throw new InvalidOperationException("Run mining setup first.");
            GameObject path = Reference<GameObject>(builds, "pathTemplate");
            GameObject anvil = Reference<GameObject>(builds, "anvilTemplate");
            foreach (Vector3 position in new[] {
                new Vector3(1.5f, 0f, 2.25f), new Vector3(2.25f, 0f, 2.25f),
                new Vector3(3f, 0f, 2.25f) })
            {
                GameObject slab = UnityEngine.Object.Instantiate(path);
                slab.SetActive(true);
                slab.transform.position = position;
            }
            GameObject station = UnityEngine.Object.Instantiate(anvil);
            station.SetActive(true);
            station.transform.position = new Vector3(3f, 0f, 3.75f);
            UnityEngine.Object.FindFirstObjectByType<Topaz.VisualStudy.VisualLookController>()?
                .SetWorldHours(12d);
            Camera camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Review camera is missing.");
            Vector3 cameraOffset = camera.transform.position -
                GameObject.Find("Player").transform.position;
            var target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            string output = "TestResults/mining-home-review.png";
            System.IO.Directory.CreateDirectory("TestResults");
            System.IO.File.WriteAllBytes(output, image.EncodeToPNG());
            camera.transform.position = new Vector3(2.25f, 0f, 2.9f) + cameraOffset;
            camera.orthographicSize = 3.5f;
            camera.Render();
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            System.IO.File.WriteAllBytes("TestResults/mining-home-detail.png", image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("[Topaz] Review image: " + output);
        }

        [MenuItem("Topaz/Add Mining And Home Builds")]
        public static void Configure()
        {
            ItemDefinition stone = Item("Stone", "material.stone", "Stone", 20);
            ItemDefinition iron = Item("Iron", "material.iron", "Iron", 20);
            ItemDefinition pickaxe = Item("Pickaxe", "gear.pickaxe.starter", "Pickaxe", 1);
            MeleeAttackDefinition strike = Asset<MeleeAttackDefinition>(Definitions + "PickaxeStrike.asset");
            Set(strike, "stableId", "pickaxe.strike");
            Set(strike, "windupSeconds", 0.45f);
            Set(strike, "activeSeconds", 0.10f);
            Set(strike, "recoverySeconds", 0.45f);
            Set(strike, "range", 2.1f);
            Set(strike, "arcDegrees", 80f);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("Player");
            WorldSession session = player != null ? player.GetComponent<WorldSession>() : null;
            if (session == null) throw new InvalidOperationException("Bootstrap WorldSession is missing.");
            GameObject old = GameObject.Find("Mining And Home Builds");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            GameObject root = new GameObject("Mining And Home Builds");
            root.transform.SetParent(GameObject.Find("World Loop Study").transform, false);

            SafeZone home = UnityEngine.Object.FindFirstObjectByType<SafeZone>();
            Set(home, "radius", 5.5f);
            GameObject boundary = GameObject.Find("Home Boundary");
            if (boundary != null) boundary.transform.localScale = Vector3.one * (5.5f / 2.6f);
            Vector3 center = home.transform.position;
            Vector3[] offsets = {
                new Vector3(-6.8f, 0f, -1.4f),
                new Vector3(6.8f, 0f, 2.3f),
                new Vector3(1.8f, 0f, 6.8f)
            };
            GameObject rockModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ThirdParty/KayKit/Forest/Models/Rock_3_B_Color1.fbx");
            if (rockModel == null) throw new InvalidOperationException("Forest rock asset is missing.");
            var rocks = new MiningRock[3];
            for (int i = 0; i < rocks.Length; i++)
            {
                GameObject rock = new GameObject($"Mining Rock {i + 1:00}");
                rock.transform.SetParent(root.transform, false);
                rock.transform.position = center + offsets[i];
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(rockModel, rock.transform);
                visual.name = "Rock Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = Vector3.one * 1.35f;
                SphereCollider collider = rock.AddComponent<SphereCollider>();
                collider.radius = 0.75f;
                collider.center = Vector3.up * 0.45f;
                NavMeshObstacle obstacle = rock.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Capsule;
                obstacle.radius = 0.8f;
                obstacle.height = 1.2f;
                obstacle.carving = true;
                MiningRock node = rock.AddComponent<MiningRock>();
                Set(node, "stableObjectId", $"topaz.home.rock.{i + 1:00}");
                Set(node, "visualRoot", visual);
                Set(node, "rockCollider", collider);
                Set(node, "obstacle", obstacle);
                rocks[i] = node;
            }

            GameObject pathTemplate = Template("Stone Path Template", root.transform,
                "Assets/ThirdParty/KayKit/Dungeon/Models/floor_tile_small.fbx", 0.72f);
            GameObject anvilTemplate = Template("Blacksmith's Anvil Template", root.transform,
                "Assets/ThirdParty/KayKit/RPGTools/Models/anvil.fbx", 1.15f);
            GameObject baseModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ThirdParty/KayKit/ResourceBits/Models/Stone_Brick.fbx");
            if (baseModel != null)
            {
                GameObject baseVisual = (GameObject)PrefabUtility.InstantiatePrefab(
                    baseModel, anvilTemplate.transform);
                baseVisual.name = "Stone Base";
                baseVisual.transform.localPosition = Vector3.zero;
                baseVisual.transform.localScale = new Vector3(2f, 0.6f, 1.6f);
                Transform anvilMesh = anvilTemplate.transform.GetChild(0);
                anvilMesh.localPosition = Vector3.up * 0.38f;
            }
            BoxCollider anvilCollider = anvilTemplate.AddComponent<BoxCollider>();
            anvilCollider.center = Vector3.up * 0.5f;
            anvilCollider.size = new Vector3(1.1f, 1f, 0.8f);
            NavMeshObstacle anvilObstacle = anvilTemplate.AddComponent<NavMeshObstacle>();
            anvilObstacle.shape = NavMeshObstacleShape.Box;
            anvilObstacle.center = Vector3.up * 0.5f;
            anvilObstacle.size = new Vector3(1.1f, 1f, 0.8f);
            anvilObstacle.carving = true;
            pathTemplate.SetActive(false);
            anvilTemplate.SetActive(false);

            HomeBuilds builds = root.AddComponent<HomeBuilds>();
            Set(builds, "home", home);
            Set(builds, "player", player.GetComponent<FeelStudyPlayer>());
            Set(builds, "workbench", Reference<Transform>(session, "workbench"));
            Set(builds, "restPoint", Reference<Transform>(session, "restPoint"));
            Set(builds, "gate", Reference<Transform>(session, "homeGate"));
            Set(builds, "campfire", Reference<Campfire>(session, "homeCampfire").transform);
            Set(builds, "chest", Reference<StorageChest>(session, "chest"));
            Set(builds, "pathTemplate", pathTemplate);
            Set(builds, "anvilTemplate", anvilTemplate);

            Set(session, "stone", stone);
            Set(session, "iron", iron);
            Set(session, "pickaxeItem", pickaxe);
            SetArray(session, "rocks", rocks);
            Set(session, "homeBuilds", builds);
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            Set(combat, "pickaxeAttack", strike);
            GameObject pivot = new GameObject("Pickaxe Pivot");
            pivot.transform.SetParent(player.transform.Find("Facing Visual"), false);
            pivot.transform.localPosition = new Vector3(0.39f, 1.04f, 0.18f);
            pivot.SetActive(false);
            Set(combat, "pickaxePivot", pivot.transform);

            PlayerAppearance appearance = player.GetComponent<PlayerAppearance>();
            GameObject rogue = Reference<GameObject>(appearance, "rogueVisual");
            Transform hand = rogue.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(value => value.name == "handslot.r");
            GameObject pickaxeModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ThirdParty/KayKit/RPGTools/Models/pickaxe.fbx");
            if (hand == null || pickaxeModel == null)
                throw new InvalidOperationException("Pickaxe model or hand socket is missing.");
            Transform previousPickaxe = hand.Find("Held Pickaxe");
            if (previousPickaxe != null) UnityEngine.Object.DestroyImmediate(previousPickaxe.gameObject);
            GameObject held = (GameObject)PrefabUtility.InstantiatePrefab(pickaxeModel, hand);
            held.name = "Held Pickaxe";
            Transform axe = hand.Find("Held Axe");
            held.transform.localPosition = axe.localPosition;
            held.transform.localRotation = axe.localRotation;
            held.transform.localScale = axe.localScale;
            held.SetActive(false);
            Set(rogue.GetComponent<CharacterAnimationDriver>(), "pickaxeVisual", held);

            GameObject canvas = GameObject.Find("Loop HUD");
            TopazUiTheme theme = AssetDatabase.LoadAssetAtPath<TopazUiTheme>(
                "Assets/Topaz/UI/Themes/TopazUiTheme.asset");
            UiDesignSystemSetup.ApplyToCanvas(canvas.transform, theme);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Mining rocks, tool, path, and Blacksmith's Anvil authored.");
        }

        static GameObject Template(string name, Transform parent, string assetPath, float scale)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null) throw new InvalidOperationException("Missing build model: " + assetPath);
            GameObject template = new GameObject(name);
            template.transform.SetParent(parent, false);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, template.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * scale;
            return template;
        }

        static ItemDefinition Item(string name, string id, string label, int stack)
        {
            ItemDefinition item = Asset<ItemDefinition>(Definitions + name + ".asset");
            Set(item, "stableId", id);
            Set(item, "displayName", label);
            Set(item, "maxStack", stack);
            return item;
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static T Reference<T>(UnityEngine.Object owner, string field) where T : UnityEngine.Object
        {
            return new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
        }

        static void Set(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(owner);
            SerializedProperty property = data.FindProperty(field);
            if (property == null) throw new InvalidOperationException("Missing field: " + field);
            property.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object owner, string field, string value)
        {
            var data = new SerializedObject(owner);
            data.FindProperty(field).stringValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object owner, string field, float value)
        {
            var data = new SerializedObject(owner);
            data.FindProperty(field).floatValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object owner, string field, int value)
        {
            var data = new SerializedObject(owner);
            data.FindProperty(field).intValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray<T>(UnityEngine.Object owner, string field, T[] values)
            where T : UnityEngine.Object
        {
            var data = new SerializedObject(owner);
            SerializedProperty property = data.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
