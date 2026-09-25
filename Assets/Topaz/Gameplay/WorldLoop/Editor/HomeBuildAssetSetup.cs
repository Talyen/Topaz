using Topaz.LoopStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Topaz.EditorTools
{
    /// <summary>Creates owned furniture prefabs and binds them in the authored Home scene.</summary>
    public static class HomeBuildAssetSetup
    {
        const string Folder = "Assets/Topaz/Gameplay/WorldLoop/Prefabs/Home";
        const string Scene = "Assets/Topaz/World/Scenes/Bootstrap.unity";

        [MenuItem("Topaz/Home/Build Furniture Prefabs")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Topaz/Gameplay/WorldLoop/Prefabs", "Home");
            GameObject bed = Ensure("Bed", "Assets/ThirdParty/KayKit/Furniture/Models/bed_single_A.fbx",
                new Vector3(1f, .65f, 1.85f), false);
            GameObject table = Ensure("Table", "Assets/ThirdParty/KayKit/Furniture/Models/table_small.fbx",
                new Vector3(1.2f, .9f, .8f), false);
            GameObject lantern = Ensure("Lantern",
                "Assets/ThirdParty/KayKit/Halloween/Models/lantern_standing.fbx",
                new Vector3(.4f, 1.25f, .4f), true);

            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (var enemy in root.GetComponentsInChildren<Topaz.CombatStudy.EnemyCombatant>(true))
                    enemy.gameObject.SetActive(false);
            HomeBuilds builds = Object.FindFirstObjectByType<HomeBuilds>();
            if (builds == null) throw new System.InvalidOperationException("HomeBuilds missing in Bootstrap.");
            GameObject ground = GameObject.Find("Dirt Floor");
            if (ground == null) throw new System.InvalidOperationException("Home ground is missing.");
            Renderer[] groundRenderers = ground.GetComponentsInChildren<Renderer>(true);
            if (groundRenderers.Length == 0) throw new System.InvalidOperationException("Home ground has no visuals.");
            Bounds groundBounds = groundRenderers[0].bounds;
            foreach (Renderer renderer in groundRenderers)
                groundBounds.Encapsulate(renderer.bounds);
            Transform boundsTransform = builds.transform.Find("Home Build Bounds");
            GameObject boundsObject = boundsTransform != null ? boundsTransform.gameObject :
                new GameObject("Home Build Bounds");
            boundsObject.transform.SetParent(builds.transform, true);
            boundsObject.transform.position = new Vector3(groundBounds.center.x, 0f,
                groundBounds.center.z);
            boundsObject.layer = 2; // Ignore Raycast: it must not intercept aim or interactions.
            BoxCollider homeArea = boundsObject.GetComponent<BoxCollider>();
            if (homeArea == null) homeArea = boundsObject.AddComponent<BoxCollider>();
            homeArea.center = Vector3.zero;
            homeArea.size = new Vector3(groundBounds.size.x - 2f, 2f,
                groundBounds.size.z - 2f);
            homeArea.isTrigger = true;
            var serialized = new SerializedObject(builds);
            serialized.FindProperty("bedTemplate").objectReferenceValue = bed;
            serialized.FindProperty("tableTemplate").objectReferenceValue = table;
            serialized.FindProperty("lanternTemplate").objectReferenceValue = lantern;
            serialized.FindProperty("finalHomeArea").objectReferenceValue = homeArea;
            WorldSession session = Object.FindFirstObjectByType<WorldSession>();
            var sessionSerialized = new SerializedObject(session);
            SerializedProperty cryptGate = sessionSerialized.FindProperty("homeCryptGate");
            serialized.FindProperty("cryptGate").objectReferenceValue =
                cryptGate != null ? cryptGate.objectReferenceValue : null;
            serialized.FindProperty("gearRack").objectReferenceValue =
                sessionSerialized.FindProperty("gearRack").objectReferenceValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            LoopHud hud = Object.FindFirstObjectByType<LoopHud>();
            var hudSerialized = new SerializedObject(hud);
            hudSerialized.FindProperty("homeJournalBackground").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Topaz/UI/Art/JournalBackground.png");
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(builds);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static GameObject Ensure(string name, string sourcePath, Vector3 footprint, bool light)
        {
            string path = Folder + "/" + name + ".prefab";
            string visualPath = "Assets/Topaz/World/Environment/Prefabs/Visuals/Home " +
                name + " Visual.prefab";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) throw new System.InvalidOperationException("Missing approved model: " + sourcePath);
            GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
            if (visualPrefab == null)
            {
                var visualRoot = new GameObject("Home " + name + " Visual");
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
                model.name = "Visual";
                model.transform.SetParent(visualRoot.transform, false);
                visualPrefab = PrefabUtility.SaveAsPrefabAsset(visualRoot, visualPath);
                Object.DestroyImmediate(visualRoot);
            }
            var root = new GameObject(name);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            var collider = root.AddComponent<BoxCollider>();
            collider.size = footprint;
            collider.center = new Vector3(0f, footprint.y * .5f, 0f);
            if (light) root.AddComponent<HomeNightLight>();
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }
    }
}
