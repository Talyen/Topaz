using System;
using System.IO;
using System.Linq;
using Topaz.CombatStudy;
using Topaz.FeelStudy;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Builds the replaceable one-sword, one-enemy combat study.</summary>
    public static class CombatStudySetup
    {
        const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        const string ControlsPath = "Assets/Topaz/Input/TopazControls.inputactions";
        const string DefinitionPath = "Assets/Topaz/CombatStudy/Definitions";
        const string MaterialPath = "Assets/Topaz/CombatStudy/Materials";
        const string NavigationPath = "Assets/Topaz/CombatStudy/Navigation/PracticeNavMesh.asset";

        [MenuItem("Topaz/Build Combat Study")]
        public static void Configure()
        {
            EnsureFolder(DefinitionPath);
            EnsureFolder(MaterialPath);
            EnsureFolder("Assets/Topaz/CombatStudy/Navigation");
            EnsureAttackBinding();

            CreateDefinition<MeleeAttackDefinition>($"{DefinitionPath}/PracticeSword.asset");
            CreateDefinition<EnemyDefinition>($"{DefinitionPath}/PracticeEnemy.asset");
            Material enemyMaterial = CreateMaterial("Enemy", new Color(0.87f, 0.36f, 0.31f), true);
            Material swordMaterial = CreateMaterial("Sword", new Color(0.82f, 0.91f, 0.94f), true);
            Material homeLineMaterial = CreateMaterial("HomeLine", new Color(0.25f, 0.78f, 0.90f), false);
            Material enemyLineMaterial = CreateMaterial("EnemyTell", new Color(1f, 0.26f, 0.19f), false);
            Material swordLineMaterial = CreateMaterial("SwordArc", new Color(0.35f, 0.92f, 1f), false);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject feelRoot = GameObject.Find("Feel Study");
            GameObject player = GameObject.Find("Player");
            if (feelRoot == null || player == null)
                throw new InvalidOperationException("Run the feel study setup before the combat setup.");

            GameObject prior = GameObject.Find("Combat Study");
            if (prior != null) UnityEngine.Object.DestroyImmediate(prior);
            GameObject combatRoot = new GameObject("Combat Study");
            combatRoot.transform.SetParent(feelRoot.transform, false);

            NavMeshSurface surface = BuildNavigation(feelRoot.transform);
            if (surface.navMeshData == null) throw new InvalidOperationException("Combat NavMesh bake failed.");

            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            MeleeAttackDefinition sword = AssetDatabase.LoadAssetAtPath<MeleeAttackDefinition>(
                $"{DefinitionPath}/PracticeSword.asset");
            EnemyDefinition enemyDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                $"{DefinitionPath}/PracticeEnemy.asset");
            if (controls == null || sword == null || enemyDefinition == null)
                throw new InvalidOperationException("A combat study asset failed to load.");

            var home = new GameObject("Safe Homestead");
            home.transform.SetParent(combatRoot.transform, false);
            home.transform.position = Vector3.zero;
            SafeZone safeZone = home.AddComponent<SafeZone>();
            LineRenderer homeRing = AddLine(home.transform, "Home Boundary", homeLineMaterial,
                new Color(0.25f, 0.78f, 0.90f), 0.06f);
            homeRing.useWorldSpace = false;
            homeRing.loop = true;
            homeRing.positionCount = 40;
            for (int i = 0; i < homeRing.positionCount; i++)
            {
                float angle = (float)i / homeRing.positionCount * Mathf.PI * 2f;
                homeRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * safeZone.Radius,
                    0.035f, Mathf.Sin(angle) * safeZone.Radius));
            }

            FeelStudyPlayer movement = player.GetComponent<FeelStudyPlayer>();
            if (movement == null) throw new InvalidOperationException("The feel study player is missing.");
            RemoveComponent<PlayerCombat>(player);
            RemoveComponent<PlayerVitality>(player);
            PlayerVitality vitality = player.AddComponent<PlayerVitality>();
            SetReference(vitality, "movement", movement);
            SetReference(vitality, "safeZone", safeZone);

            GameObject enemy = new GameObject("Enemy");
            enemy.transform.SetParent(combatRoot.transform, false);
            Vector3 spawn = new Vector3(5.2f, 0f, 5.2f);
            if (!NavMesh.SamplePosition(spawn, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                throw new InvalidOperationException("Enemy spawn is not on the baked NavMesh.");
            enemy.transform.position = navHit.position;
            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.agentTypeID = surface.agentTypeID;
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.angularSpeed = 540f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 1.4f;
            CapsuleCollider enemyCollider = enemy.AddComponent<CapsuleCollider>();
            enemyCollider.center = new Vector3(0f, 0.9f, 0f);
            enemyCollider.height = 1.8f;
            enemyCollider.radius = 0.4f;

            var enemyVisual = new GameObject("Enemy Visual");
            enemyVisual.transform.SetParent(enemy.transform, false);
            GameObject enemyBody = Primitive("Enemy Body", PrimitiveType.Capsule, enemyVisual.transform,
                new Vector3(0f, 0.9f, 0f), new Vector3(0.48f, 0.85f, 0.48f), enemyMaterial);
            Primitive("Enemy Facing", PrimitiveType.Cube, enemyVisual.transform,
                new Vector3(0f, 1.15f, 0.48f), new Vector3(0.2f, 0.18f, 0.58f), swordMaterial);
            LineRenderer enemyArc = AddLine(combatRoot.transform, "Enemy Attack Tell", enemyLineMaterial,
                new Color(1f, 0.26f, 0.19f), 0.10f);
            enemyArc.enabled = false;
            EnemyCombatant enemyCombatant = enemy.AddComponent<EnemyCombatant>();
            SetReference(enemyCombatant, "definition", enemyDefinition);
            SetReference(enemyCombatant, "target", vitality);
            SetReference(enemyCombatant, "safeZone", safeZone);
            SetReference(enemyCombatant, "visualRoot", enemyVisual.transform);
            SetReference(enemyCombatant, "bodyRenderer", enemyBody.GetComponent<Renderer>());
            SetReference(enemyCombatant, "bodyCollider", enemyCollider);
            SetReference(enemyCombatant, "telegraph", enemyArc);

            Transform visual = player.transform.Find("Facing Visual");
            if (visual == null) throw new InvalidOperationException("The player facing visual is missing.");
            Transform oldSword = visual.Find("Sword Pivot");
            if (oldSword != null) UnityEngine.Object.DestroyImmediate(oldSword.gameObject);
            var swordPivot = new GameObject("Sword Pivot");
            swordPivot.transform.SetParent(visual, false);
            swordPivot.transform.localPosition = new Vector3(0.38f, 1.05f, 0.18f);
            Primitive("Practice Blade", PrimitiveType.Cube, swordPivot.transform,
                new Vector3(0f, 0f, 0.7f), new Vector3(0.12f, 0.12f, 1.4f), swordMaterial);
            swordPivot.SetActive(false);
            LineRenderer playerArc = AddLine(combatRoot.transform, "Sword Sweep", swordLineMaterial,
                new Color(0.35f, 0.92f, 1f), 0.07f);
            playerArc.enabled = false;

            PlayerCombat playerCombat = player.AddComponent<PlayerCombat>();
            SetReference(playerCombat, "attack", sword);
            SetReference(playerCombat, "controls", controls);
            SetReference(playerCombat, "movement", movement);
            SetReference(playerCombat, "enemy", enemyCombatant);
            SetReference(playerCombat, "swordPivot", swordPivot.transform);
            SetReference(playerCombat, "swingArc", playerArc);

            CombatSceneBootstrap startup = combatRoot.AddComponent<CombatSceneBootstrap>();
            SetReference(startup, "surface", surface);
            SetReference(startup, "enemy", enemy);
            enemy.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Combat study and baked NavMesh are ready.");
        }

        static NavMeshSurface BuildNavigation(Transform feelRoot)
        {
            Transform geometry = feelRoot.Find("Navigation Geometry");
            if (geometry == null)
            {
                geometry = new GameObject("Navigation Geometry").transform;
                geometry.SetParent(feelRoot, false);
            }
            foreach (Transform child in feelRoot.Cast<Transform>().ToArray())
            {
                if (child.name == "Ground" || child.name.StartsWith("Obstacle "))
                    child.SetParent(geometry, true);
            }

            NavMeshSurface surface = geometry.GetComponent<NavMeshSurface>();
            if (surface == null) surface = geometry.gameObject.AddComponent<NavMeshSurface>();
            surface.agentTypeID = 0; // Unity's default Humanoid agent.
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.defaultArea = 0;
            surface.BuildNavMesh();
            NavMeshData data = surface.navMeshData;
            if (data == null) return surface;
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationPath) != null)
                AssetDatabase.DeleteAsset(NavigationPath);
            AssetDatabase.CreateAsset(data, NavigationPath);
            surface.navMeshData = data;
            EditorUtility.SetDirty(surface);
            return surface;
        }

        static void EnsureAttackBinding()
        {
            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null) throw new InvalidOperationException("Topaz controls asset is missing.");
            InputActionMap player = controls.FindActionMap("Player", true);
            if (player.FindAction("Attack") != null) return;
            InputAction attack = player.AddAction("Attack", InputActionType.Button);
            attack.AddBinding("<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/rightTrigger");
            File.WriteAllText(ControlsPath, controls.ToJson());
            AssetDatabase.ImportAsset(ControlsPath, ImportAssetOptions.ForceSynchronousImport);
        }

        static T CreateDefinition<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            T definition = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        static Material CreateMaterial(string name, Color color, bool lit)
        {
            string path = $"{MaterialPath}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find(lit ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("Required URP shader is unavailable.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static LineRenderer AddLine(Transform parent, string name, Material material, Color color, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier = width;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        static GameObject Primitive(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return go;
        }

        static void RemoveComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component != null) UnityEngine.Object.DestroyImmediate(component);
        }

        static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string segment in path.Split('/').Skip(1))
            {
                string next = $"{current}/{segment}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segment);
                current = next;
            }
        }

        static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException($"Missing {property} on {target.GetType().Name}.");
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (new SerializedObject(target).FindProperty(property).objectReferenceValue != value)
                throw new InvalidOperationException($"Could not assign {property} on {target.GetType().Name}.");
        }
    }
}
