using System;
using System.Linq;
using Topaz.CombatStudy;
using Topaz.LoopStudy;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Turns the existing forest scenery into persistent nodes and fills the home woodland.</summary>
    public static class GatheringAndHomeSetup
    {
        const string HomeScene = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string ExpeditionScene = "Assets/Topaz/World/Scenes/Expedition.unity";
        const string HomeNavigation = "Assets/Topaz/World/Home/Navigation/PracticeNavMesh.asset";
        const string ExpeditionNavigation =
            "Assets/Topaz/World/Expedition/Navigation/ClearingNavMesh.asset";
        const string ForestModels = "Assets/ThirdParty/KayKit/Forest/Models/";
        const string ForestMaterial = "Assets/Topaz/Presentation/Art/Materials/Forest.mat";
        const string TreeDefinition = "Assets/Topaz/Gameplay/WorldLoop/Definitions/Tree.asset";

        static readonly Vector3[] HomeTrees =
        {
            new(-13f, 0f, -12f), new(-9.5f, 0f, -13f), new(-5.5f, 0f, -12.2f),
            new(-13.2f, 0f, -5f), new(-12.8f, 0f, 8.5f), new(-8.5f, 0f, 12f),
            new(-3f, 0f, 13.5f), new(3.8f, 0f, 13.2f), new(8.5f, 0f, 11.8f),
            new(13f, 0f, 8f), new(12.7f, 0f, -3.6f), new(11.5f, 0f, -11.5f),
            new(5.2f, 0f, -13.4f)
        };

        [MenuItem("Topaz/Apply Gathering And Home Woodland")]
        public static void Apply()
        {
            ApplyHome();
            ApplyExpedition();
        }

        public static void ApplyHomeWithoutGrass() => ApplyHome(false);

        public static void ApplyHome(bool rebuildGrass = true)
        {
            if (rebuildGrass) GroundArtSetup.ApplyHome();
            Scene scene = EditorSceneManager.OpenScene(HomeScene, OpenSceneMode.Single);
            Transform feel = GameObject.Find("Feel Study")?.transform;
            Transform art = feel?.Find("KayKit Art Study");
            Transform navigation = feel?.Find("Navigation Geometry");
            SafeZone circle = UnityEngine.Object.FindAnyObjectByType<SafeZone>();
            if (art == null || navigation == null || circle == null)
                throw new InvalidOperationException("Home woodland requires art, navigation, and SafeZone.");

            foreach (Transform child in scene.GetRootGameObjects().SelectMany(root =>
                         root.GetComponentsInChildren<Transform>(true)))
                if (child.name.StartsWith("Firewood ", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);

            // The four graybox obstacles were baked into navigation. Their rock art now
            // supplies its own collider and carving obstacle, so depleted nodes open a path.
            foreach (Transform child in navigation.Cast<Transform>().ToArray())
                if (child.name.StartsWith("Obstacle ", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);

            foreach (Transform child in art.Cast<Transform>().ToArray())
            {
                if (!child.name.StartsWith("Rock_", StringComparison.Ordinal)) continue;
                Vector3 offset = child.position - circle.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < (circle.Radius + .5f) * (circle.Radius + .5f))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            AddHomeTrees(art);
            ConvertScenery(art, "topaz.home");
            RebuildNavigation(navigation.GetComponent<NavMeshSurface>(), HomeNavigation);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void ApplyExpedition()
        {
            Scene scene = EditorSceneManager.OpenScene(ExpeditionScene, OpenSceneMode.Single);
            Transform root = GameObject.Find("Expedition Clearing")?.transform;
            Transform art = root?.Find("KayKit Clearing Art");
            Transform navigation = root?.Find("Navigation Geometry");
            if (art == null || navigation == null)
                throw new InvalidOperationException("Expedition art or navigation is missing.");
            foreach (Transform child in navigation.Cast<Transform>().ToArray())
                if (child.name == "Rock Obstacle")
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            ConvertScenery(art, "topaz.expedition");
            RebuildNavigation(navigation.GetComponent<NavMeshSurface>(), ExpeditionNavigation);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void AddHomeTrees(Transform art)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ForestMaterial);
            string[] models = { "Tree_4_A_Color1.fbx", "Tree_4_B_Color1.fbx",
                "Tree_4_C_Color1.fbx" };
            if (material == null) throw new InvalidOperationException("Forest material is missing.");
            for (int i = 0; i < HomeTrees.Length; i++)
            {
                string name = $"Home Tree {i + 1:00}";
                if (art.Find(name) != null) continue;
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ForestModels +
                    models[i % models.Length]);
                if (asset == null) throw new InvalidOperationException("Home tree art is missing.");
                var owner = new GameObject(name);
                owner.transform.SetParent(art, false);
                owner.transform.position = HomeTrees[i];
                GameObject visual = PrefabUtility.InstantiatePrefab(asset, owner.transform) as GameObject;
                if (visual == null) throw new InvalidOperationException("Could not place home tree.");
                visual.name = "Visual";
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = Enumerable.Repeat(material,
                        renderer.sharedMaterials.Length).ToArray();
                Bounds bounds = visual.GetComponentsInChildren<Renderer>(true)[0].bounds;
                float height = 3.2f + .2f * (i % 4);
                if (bounds.size.y > .001f)
                    visual.transform.localScale *= height / bounds.size.y;
                bounds = visual.GetComponentsInChildren<Renderer>(true)[0].bounds;
                visual.transform.position += Vector3.up * (HomeTrees[i].y - bounds.min.y);
            }
        }

        static void ConvertScenery(Transform art, string regionId)
        {
            HarvestDefinition definition = AssetDatabase.LoadAssetAtPath<HarvestDefinition>(TreeDefinition);
            if (definition == null) throw new InvalidOperationException("Tree definition is missing.");
            foreach (Transform child in art.Cast<Transform>().ToArray())
            {
                bool tree = child.name.StartsWith("Tree_", StringComparison.Ordinal) ||
                    child.name.StartsWith("Home Tree ", StringComparison.Ordinal) ||
                    child.name.StartsWith("Forest Tree", StringComparison.Ordinal);
                bool rock = child.name.StartsWith("Rock_", StringComparison.Ordinal) ||
                    child.name.StartsWith("Forest Rock", StringComparison.Ordinal);
                if (!tree && !rock) continue;
                GameObject owner = child.gameObject;
                GameObject visual = child.Find("Visual")?.gameObject;
                if (visual == null)
                {
                    var wrapper = new GameObject(child.name);
                    wrapper.transform.SetParent(art, false);
                    wrapper.transform.position = child.position;
                    child.SetParent(wrapper.transform, true);
                    child.name = "Visual";
                    owner = wrapper;
                    visual = child.gameObject;
                }
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
                string id = StableId(regionId, tree ? "tree" : "rock", owner.transform.position);
                if (tree) ConfigureTree(owner, visual, definition, id);
                else ConfigureRock(owner, visual, id);
            }
        }

        static void ConfigureTree(GameObject owner, GameObject visual,
            HarvestDefinition definition, string id)
        {
            CapsuleCollider collider = owner.GetComponent<CapsuleCollider>();
            if (collider == null) collider = owner.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1.1f, 0f);
            collider.radius = .43f;
            collider.height = 2.2f;
            NavMeshObstacle obstacle = owner.GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle = owner.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.radius = .48f;
            obstacle.height = 2.2f;
            obstacle.carving = true;
            HarvestTree node = owner.GetComponent<HarvestTree>();
            if (node == null)
            {
                node = owner.AddComponent<HarvestTree>();
                Set(node, "stableObjectId", id);
            }
            Set(node, "definition", definition);
            Set(node, "visualRoot", visual);
            Set(node, "trunkRenderer", visual.GetComponentInChildren<Renderer>(true));
            Set(node, "trunkCollider", collider);
            Set(node, "obstacle", obstacle);
        }

        static void ConfigureRock(GameObject owner, GameObject visual, string id)
        {
            SphereCollider collider = owner.GetComponent<SphereCollider>();
            if (collider == null) collider = owner.AddComponent<SphereCollider>();
            collider.center = Vector3.up * .45f;
            collider.radius = .75f;
            NavMeshObstacle obstacle = owner.GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle = owner.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.radius = .8f;
            obstacle.height = 1.2f;
            obstacle.carving = true;
            MiningRock node = owner.GetComponent<MiningRock>();
            if (node == null)
            {
                node = owner.AddComponent<MiningRock>();
                Set(node, "stableObjectId", id);
            }
            Set(node, "visualRoot", visual);
            Set(node, "rockCollider", collider);
            Set(node, "obstacle", obstacle);
        }

        static string StableId(string region, string kind, Vector3 position) =>
            $"{region}.{kind}.{Mathf.RoundToInt(position.x * 100f)}.{Mathf.RoundToInt(position.z * 100f)}";

        static void Set(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object owner, string field, string value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void RebuildNavigation(NavMeshSurface surface, string path)
        {
            if (surface == null) throw new InvalidOperationException("Navigation surface is missing.");
            surface.BuildNavMesh();
            NavMeshData data = surface.navMeshData;
            if (data == null) throw new InvalidOperationException("Navigation bake failed.");
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(data, path);
            surface.navMeshData = data;
            EditorUtility.SetDirty(surface);
        }
    }
}
