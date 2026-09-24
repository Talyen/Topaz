using System;
using System.Linq;
using Topaz.AnimationStudy.Editor;
using Topaz.CombatStudy;
using Topaz.Expedition;
using Topaz.LoopStudy;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Authors a small KayKit clearing connected to the persistent homestead.</summary>
    public static class ExpeditionStudySetup
    {
        const string HomeScene = "Assets/Scenes/Bootstrap.unity";
        const string ExpeditionScene = "Assets/Scenes/Expedition.unity";
        const string Root = "Assets/Topaz/Expedition";
        const string NavigationPath = Root + "/Navigation/ClearingNavMesh.asset";
        const string KayKit = "Assets/ThirdParty/KayKit/";

        [MenuItem("Topaz/Build Expedition Study")]
        public static void Configure()
        {
            EnsureFolder(Root + "/Definitions");
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Navigation");
            AnimationStudySetup.EnsureGenericAvatar(KayKit + "Skeletons/Characters/Skeleton_Minion.fbx");
            AnimationStudySetup.EnsureGenericAvatar(KayKit + "Skeletons/Characters/Skeleton_Warrior.fbx");

            EnemyDefinition scout = Definition("ExpeditionScout", "enemy.expedition.scout",
                2, 9f, 3.5f, 1.65f, 65f, 0.45f, 0.75f);
            EnemyDefinition guardian = Definition("ExpeditionGuardian", "enemy.expedition.guardian",
                4, 10f, 2.8f, 2.35f, 145f, 1.05f, 1.15f);
            Material ground = CreateMaterial("Clearing Ground", new Color(.30f, .35f, .29f));
            Material rock = CreateMaterial("Clearing Rock", new Color(.41f, .43f, .39f));
            Material trail = CreateUnlitMaterial("Blue Trail Marker", new Color(.25f, .79f, 1f));
            Material guardianMaterial = CloneMaterial("Guardian Skeleton",
                "Assets/Topaz/ArtStudy/Materials/Skeleton.mat", new Color(.94f, .76f, .48f));
            Material forest = Load<Material>("Assets/Topaz/ArtStudy/Materials/Forest.mat");
            Material dungeon = Load<Material>("Assets/Topaz/ArtStudy/Materials/Dungeon.mat");
            Material skeleton = Load<Material>("Assets/Topaz/ArtStudy/Materials/Skeleton.mat");
            Material tell = Load<Material>("Assets/Topaz/CombatStudy/Materials/EnemyTell.mat");

            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ExpeditionScene) == null
                ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)
                : EditorSceneManager.OpenScene(ExpeditionScene, OpenSceneMode.Single);
            GameObject old = GameObject.Find("Expedition Clearing");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject("Expedition Clearing");
            root.transform.position = new Vector3(100f, 0f, 0f);

            Transform navigation = new GameObject("Navigation Geometry").transform;
            navigation.SetParent(root.transform, false);
            Primitive("Ground", PrimitiveType.Cube, navigation,
                new Vector3(0f, -.12f, 0f), new Vector3(34f, .24f, 44f), ground, true);
            Vector3[] obstacles =
            {
                new Vector3(-4f, .7f, -7f), new Vector3(4f, .7f, -1f),
                new Vector3(-4f, .7f, 5f), new Vector3(4f, .7f, 10f)
            };
            foreach (Vector3 position in obstacles)
                Primitive("Rock Obstacle", PrimitiveType.Cube, navigation, position,
                    new Vector3(2.2f, 1.4f, 2.2f), rock, true);
            NavMeshSurface surface = navigation.gameObject.AddComponent<NavMeshSurface>();
            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            NavMeshData savedNavigation = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationPath);
            if (savedNavigation == null)
            {
                surface.BuildNavMesh();
                if (surface.navMeshData == null)
                    throw new InvalidOperationException("Expedition NavMesh bake failed.");
                AssetDatabase.CreateAsset(surface.navMeshData, NavigationPath);
                savedNavigation = surface.navMeshData;
            }
            surface.navMeshData = savedNavigation;

            Transform decoration = new GameObject("KayKit Clearing Art").transform;
            decoration.SetParent(root.transform, false);
            foreach (Vector3 position in obstacles)
                AttachModel(decoration, "Forest/Models/Rock_2_A_Color1.fbx", forest,
                    new Vector3(position.x, 0f, position.z), 1.9f, "Forest Rock");
            AddTree(decoration, forest, new Vector3(-11f, 0f, -12f), 3.5f);
            AddTree(decoration, forest, new Vector3(11f, 0f, -9f), 3.1f);
            AddTree(decoration, forest, new Vector3(-12f, 0f, 8f), 3.7f);
            AddTree(decoration, forest, new Vector3(11f, 0f, 16f), 3.4f);

            Transform arrival = new GameObject("Arrival").transform;
            arrival.SetParent(root.transform, false);
            arrival.localPosition = new Vector3(0f, 0f, -17f);
            Transform departure = new GameObject("Return Trail Marker").transform;
            departure.SetParent(root.transform, false);
            departure.localPosition = new Vector3(0f, 0f, -18.5f);
            AttachModel(departure, "Dungeon/Models/banner_blue.fbx", dungeon,
                departure.position, 2.2f, "Blue Trail Banner");
            AddTrailMarker(departure, trail);
            Transform cache = new GameObject("Guarded Supply Cache").transform;
            cache.SetParent(root.transform, false);
            cache.localPosition = new Vector3(0f, 0f, 16f);
            GameObject cacheVisual = AttachModel(cache, "Dungeon/Models/chest_gold.fbx", dungeon,
                cache.position, .9f, "KayKit Supply Chest");

            EnemyCombatant scoutA = Enemy(root.transform, "Scout A", scout,
                new Vector3(-3.5f, 0f, -4f), "Skeletons/Characters/Skeleton_Minion.fbx",
                skeleton, tell, false);
            EnemyCombatant scoutB = Enemy(root.transform, "Scout B", scout,
                new Vector3(3.5f, 0f, 5f), "Skeletons/Characters/Skeleton_Minion.fbx",
                skeleton, tell, false);
            EnemyCombatant guard = Enemy(root.transform, "Wide-Sweep Guardian", guardian,
                new Vector3(0f, 0f, 12f), "Skeletons/Characters/Skeleton_Warrior.fbx",
                guardianMaterial, tell, true);
            var context = root.AddComponent<ExpeditionSceneBootstrap>();
            Ref(context, "surface", surface);
            Ref(context, "arrival", arrival);
            Ref(context, "departure", departure);
            Ref(context, "supplyCache", cache);
            Ref(context, "cacheVisual", cacheVisual);
            Ref(context, "guardian", guard);
            var contextData = new SerializedObject(context);
            SerializedProperty enemies = contextData.FindProperty("enemies");
            enemies.arraySize = 3;
            enemies.GetArrayElementAtIndex(0).objectReferenceValue = scoutA;
            enemies.GetArrayElementAtIndex(1).objectReferenceValue = scoutB;
            enemies.GetArrayElementAtIndex(2).objectReferenceValue = guard;
            contextData.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ExpeditionScene);
            Scene homeScene = EditorSceneManager.OpenScene(HomeScene, OpenSceneMode.Single);
            GameObject player = GameObject.Find("Player");
            if (player == null) throw new InvalidOperationException("Build the world loop before the expedition.");
            Transform oldGate = GameObject.Find("Expedition Gate")?.transform;
            if (oldGate != null) UnityEngine.Object.DestroyImmediate(oldGate.gameObject);
            Transform gate = new GameObject("Expedition Gate").transform;
            gate.position = new Vector3(0f, 0f, -5f);
            AttachModel(gate, "Dungeon/Models/banner_blue.fbx", dungeon,
                gate.position, 2.2f, "Blue Trail Banner");
            AddTrailMarker(gate, trail);
            Ref(player.GetComponent<WorldSession>(), "homeGate", gate);
            EditorSceneManager.MarkSceneDirty(homeScene);
            EditorSceneManager.SaveScene(homeScene);

            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            if (!existingScenes.Any(entry => entry.path == ExpeditionScene))
                EditorBuildSettings.scenes = existingScenes.Append(
                    new EditorBuildSettingsScene(ExpeditionScene, true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Authored expedition clearing, guardian, reward cache, and travel gate are ready.");
        }

        static EnemyCombatant Enemy(Transform parent, string name, EnemyDefinition definition,
            Vector3 localPosition, string modelPath, Material modelMaterial,
            Material tellMaterial, bool guardian)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
            agent.agentTypeID = 0;
            agent.radius = guardian ? .46f : .38f;
            agent.height = guardian ? 1.85f : 1.6f;
            agent.angularSpeed = 540f;
            agent.acceleration = 12f;
            CapsuleCollider body = go.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, guardian ? .92f : .78f, 0f);
            body.height = guardian ? 1.85f : 1.55f;
            body.radius = guardian ? .46f : .38f;
            var visual = new GameObject("Enemy Visual");
            visual.transform.SetParent(go.transform, false);
            GameObject model = AttachModel(visual.transform, modelPath, modelMaterial,
                visual.transform.position, guardian ? 1.85f : 1.55f, "KayKit Skeleton");
            Renderer renderer = model.GetComponentInChildren<Renderer>(true);
            LineRenderer tell = AddLine(parent, name + " Tell", tellMaterial,
                guardian ? .13f : .09f);
            var combatant = go.AddComponent<EnemyCombatant>();
            Ref(combatant, "definition", definition);
            Ref(combatant, "visualRoot", visual.transform);
            Ref(combatant, "bodyRenderer", renderer);
            Ref(combatant, "bodyCollider", body);
            Ref(combatant, "telegraph", tell);
            Bool(combatant, "keepVisualOnDefeat", true);
            Bool(combatant, "respawns", false);
            if (guardian)
            {
                Tint(combatant, "normalBodyTint", new Color(.94f, .76f, .48f));
                Transform hand = model.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "handslot.r");
                if (hand != null)
                    AttachModel(hand, "Skeletons/Models/Skeleton_Blade.fbx", modelMaterial,
                        hand.position, .8f, "Guardian Blade", true);
            }
            AnimationStudySetup.BindExpeditionEnemy(model, KayKit + modelPath, combatant, agent, guardian);
            go.SetActive(false);
            return combatant;
        }

        static EnemyDefinition Definition(string name, string stableId, int health,
            float detection, float speed, float range, float arc, float telegraph, float recovery)
        {
            string path = Root + "/Definitions/" + name + ".asset";
            EnemyDefinition result = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (result == null)
            {
                result = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(result, path);
            }
            String(result, "stableId", stableId);
            Int(result, "health", health);
            Float(result, "detectionRange", detection);
            Float(result, "travelSpeed", speed);
            Float(result, "strikeRange", range);
            Float(result, "strikeArcDegrees", arc);
            Float(result, "telegraphSeconds", telegraph);
            Float(result, "recoverySeconds", recovery);
            return result;
        }

        static void AddTree(Transform parent, Material material, Vector3 position, float height) =>
            AttachModel(parent, "Forest/Models/Tree_1_A_Color1.fbx", material,
                parent.TransformPoint(position), height, "Forest Tree");

        static GameObject AttachModel(Transform parent, string path, Material material,
            Vector3 worldPosition, float height, string name, bool longestAxis = false)
        {
            GameObject prefab = Load<GameObject>(KayKit + path);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate " + path);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.position = worldPosition;
            instance.transform.localRotation = Quaternion.identity;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Model has no renderer: " + path);
            foreach (Renderer renderer in renderers)
                renderer.sharedMaterials = Enumerable.Repeat(material,
                    Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float size = longestAxis ? Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) : bounds.size.y;
            if (size > .001f) instance.transform.localScale *= height / size;
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            instance.transform.position += Vector3.up * (worldPosition.y - bounds.min.y);
            return instance;
        }

        static GameObject Primitive(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Material material, bool collider)
        {
            GameObject result = GameObject.CreatePrimitive(type);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(result.GetComponent<Collider>());
            return result;
        }

        static LineRenderer AddLine(Transform parent, string name, Material material, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.startColor = Color.red;
            line.endColor = Color.red;
            line.widthMultiplier = width;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        static void AddTrailMarker(Transform parent, Material material)
        {
            Primitive("Blue Trail Post", PrimitiveType.Cylinder, parent,
                new Vector3(0f, 1f, 0f), new Vector3(.20f, 1f, .20f), material, false);
            Primitive("Blue Trail Light", PrimitiveType.Sphere, parent,
                new Vector3(0f, 2.25f, 0f), new Vector3(.38f, .38f, .38f), material, false);
            LineRenderer ring = AddLine(parent, "Trail Interaction Ring", material, .075f);
            ring.startColor = UnityEngine.Color.white;
            ring.endColor = UnityEngine.Color.white;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 32;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * .95f,
                    .06f, Mathf.Sin(angle) * .95f));
            }
            ring.enabled = true;
        }

        static Material CreateMaterial(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var result = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            result.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        static Material CreateUnlitMaterial(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var result = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
            result.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        static Material CloneMaterial(string name, string sourcePath, Color tint)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var result = new Material(Load<Material>(sourcePath)) { name = name };
            result.SetColor("_BaseColor", tint);
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Missing asset: " + path);

        static void Ref(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(fieldName).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void String(UnityEngine.Object target, string fieldName, string value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(fieldName).stringValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Int(UnityEngine.Object target, string fieldName, int value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(fieldName).intValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Float(UnityEngine.Object target, string fieldName, float value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(fieldName).floatValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Bool(UnityEngine.Object target, string fieldName, bool value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(fieldName).boolValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Tint(UnityEngine.Object target, string fieldName, Color value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(fieldName).colorValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
