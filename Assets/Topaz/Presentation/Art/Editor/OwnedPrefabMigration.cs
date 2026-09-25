using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Topaz.AnimationStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Topaz.Art.Editor
{
    /// <summary>One-time extraction of prototype content using Unity's normal prefab APIs.
    /// Existing assets are never overwritten: subsequent runs only migrate new loose content.</summary>
    public static class OwnedPrefabMigration
    {
        public static readonly string[] ScenePaths = {
            "Assets/Topaz/World/Scenes/Bootstrap.unity", "Assets/Topaz/World/Scenes/Expedition.unity",
            "Assets/Topaz/World/Scenes/Crypt.unity" };
        const string ManifestPath = "Assets/Topaz/Presentation/Art/Editor/PrefabSources.json";
        [Serializable] public sealed class Sources { public Entry[] entries; }
        [Serializable] public sealed class Entry { public string source; public string prefab; }
        static readonly Dictionary<string, string> defaults = new Dictionary<string, string>();
        static readonly string[] EquipmentFields = { "swordVisual", "axeVisual", "pickaxeVisual",
            "combatAxeVisual", "staffVisual", "crossbowVisual", "shieldVisual" };

        [MenuItem("Topaz/Prefabs/Migrate Prototype Content")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            ReadManifest();
            try
            {
                foreach (string scenePath in ScenePaths)
                {
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    MigrateModels(scene);
                    ConfigureLooks(scene);
                    MigrateAssetReferences(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)));
                    foreach (var lantern in All(scene).Select(g => g.GetComponent<Topaz.FeelStudy.PlayerLantern>()).Where(c => c != null).ToArray())
                        Set(lantern, "lanternModel", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Topaz/Player/Prefabs/Visuals/Carried Lantern.prefab"));
                    ExtractOwners(scene);
                    ExtractGeometry(scene);
                    foreach (var stage in All(scene).Select(g => g.GetComponent<Topaz.Menus.MainMenuStage>()).Where(c => c != null).ToArray())
                    foreach (Transform child in stage.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.GetComponent<Camera>() != null) continue;
                        child.gameObject.layer = 5;
                        if (PrefabUtility.IsPartOfPrefabInstance(child)) PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save " + scenePath);
                }
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Topaz" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    // Newly extracted visuals already contain only intended imported-model references.
                    if (path.Contains("/Visuals/") || path.StartsWith("Assets/Topaz/UI/Prefabs/")) continue;
                    GameObject root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        MigrateModels(root.scene);
                        MigrateAssetReferences(root.GetComponentsInChildren<MonoBehaviour>(true));
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Topaz" }))
                    MigrateAssetReferences(new[] { AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)) });
                File.WriteAllText(ManifestPath, JsonUtility.ToJson(new Sources { entries = defaults.OrderBy(p => p.Key)
                    .Select(p => new Entry { source = p.Key, prefab = p.Value }).ToArray() }, true) + "\n");
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                Debug.Log("[Topaz] Owned prefab migration complete; " + defaults.Count + " source models mapped.");
            }
            finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        static void ReadManifest()
        {
            defaults.Clear();
            if (!File.Exists(ManifestPath)) return;
            foreach (Entry entry in JsonUtility.FromJson<Sources>(File.ReadAllText(ManifestPath)).entries)
                defaults[entry.source] = entry.prefab;
        }

        static bool Vendor(string path) => path.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal) &&
            path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
        static int Depth(Transform t) => t.parent == null ? 0 : 1 + Depth(t.parent);
        static IEnumerable<GameObject> All(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject);

        static void MigrateModels(Scene scene)
        {
            var models = All(scene).Where(g => PrefabUtility.GetNearestPrefabInstanceRoot(g) == g &&
                Vendor(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g)))
                .OrderByDescending(g => Depth(g.transform)).ToArray();
            foreach (GameObject model in models)
            {
                // A nested imported model inside an owned visual is the intended boundary.
                bool owned = false;
                for (Transform p = model.transform.parent; p != null; p = p.parent)
                    if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p.gameObject).Contains("/Visuals/"))
                    { owned = true; break; }
                if (owned) continue;
                string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model);
                string role = SemanticName(source);
                CharacterAnimationDriver driver = model.GetComponent<CharacterAnimationDriver>();
                if (driver != null) role += Get(driver, "player") != null ? " Player" : " Enemy";
                string directory = source.Contains("/Characters/") ? "Assets/Topaz/Characters/Prefabs/Visuals/" :
                    IsEquipment(source) ? "Assets/Topaz/Gameplay/Combat/Prefabs/Visuals/" :
                    "Assets/Topaz/World/Environment/Prefabs/Visuals/";
                string signature = string.Join("|", model.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(r => r.sharedMaterials).Select(m => AssetDatabase.GetAssetPath(m)));
                // Different authored materials are intentional visual variants, shared across scenes.
                string suffix = Hash128.Compute(signature).ToString().Substring(0, 8);
                string path = defaults.TryGetValue(source, out string existingPath) && File.Exists(ManifestPath)
                    ? existingPath : directory + role + " " + suffix + ".prefab";
                Transform oldParent = model.transform.parent;
                int sibling = model.transform.GetSiblingIndex();
                var root = new GameObject(model.name);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetParent(oldParent, false);
                root.transform.localPosition = model.transform.localPosition;
                root.transform.localRotation = model.transform.localRotation;
                root.transform.localScale = model.transform.localScale;
                root.transform.SetSiblingIndex(sibling);
                root.layer = model.layer;
                root.SetActive(model.activeSelf);
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                model.name = "Visual";
                model.SetActive(true);
                if (model.GetComponent<Animator>() != null) ConfigureBindings(root, model);
                Connect(root, path);
                if (!defaults.ContainsKey(source)) defaults.Add(source, path);
                // Consumers reference the stable wrapper, rather than the imported model root.
                RemapRootReferences(scene, model, root);
            }
        }

        static void RemapRootReferences(Scene scene, GameObject old, GameObject replacement)
        {
            foreach (MonoBehaviour component in All(scene).SelectMany(g => g.GetComponents<MonoBehaviour>()).Where(c => c != null))
            {
                if (component.transform.IsChildOf(replacement.transform)) continue;
                var data = new SerializedObject(component);
                var property = data.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (property.objectReferenceValue == old) property.objectReferenceValue = replacement;
                    else if (property.objectReferenceValue == old.transform) property.objectReferenceValue = replacement.transform;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void ConfigureBindings(GameObject root, GameObject model)
        {
            CharacterVisual bindings = root.GetComponent<CharacterVisual>() ?? root.AddComponent<CharacterVisual>();
            Transform[] bones = model.GetComponentsInChildren<Transform>(true);
            Set(bindings, "animator", model.GetComponent<Animator>());
            Set(bindings, "bodyRenderer", model.GetComponentInChildren<SkinnedMeshRenderer>(true) as Renderer ??
                model.GetComponentInChildren<Renderer>(true));
            Set(bindings, "rightHand", bones.FirstOrDefault(t => t.name == "handslot.r"));
            Set(bindings, "leftHand", bones.FirstOrDefault(t => t.name == "handslot.l"));
            Set(bindings, "lanternAnchor", bones.FirstOrDefault(t => t.name == "hips"));
            Set(bindings, "guardUpperArm", bones.FirstOrDefault(t => t.name == "upperarm.l"));
            Set(bindings, "guardLowerArm", bones.FirstOrDefault(t => t.name == "lowerarm.l"));
            Set(bindings, "shield", bones.FirstOrDefault(t => t.name == "Held Shield")?.gameObject);
        }

        sealed class SavedProperty
        {
            public Component owner;
            public string path;
            public Object reference;
            public string text;
        }

        static void Connect(GameObject root, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            bool active = root.activeSelf;
            int layer = root.layer;
            var bindings = new List<SavedProperty>();
            foreach (Component component in root.GetComponentsInChildren<Component>(true).Where(c => c != null && !(c is Transform)))
            {
                var data = new SerializedObject(component);
                var p = data.GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType == SerializedPropertyType.String && (p.name == "stableObjectId" || (component.GetType().Name == "Campfire" && (p.name == "stableId" || p.name == "regionId"))))
                    {
                        bindings.Add(new SavedProperty { owner = component, path = p.propertyPath, text = p.stringValue });
                        p.stringValue = "";
                    }
                    if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                    Object value = p.objectReferenceValue;
                    Transform t = value is GameObject g ? g.transform : value is Component c ? c.transform : null;
                    if (t == null || EditorUtility.IsPersistent(value) || t.IsChildOf(root.transform)) continue;
                    if (!(component is CharacterAnimationDriver) && !(component is ShieldGuardPose))
                        bindings.Add(new SavedProperty { owner = component, path = p.propertyPath, reference = value });
                    p.objectReferenceValue = null;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
            else
                PrefabUtility.ConvertToPrefabInstance(root, asset, new ConvertToPrefabInstanceSettings {
                    objectMatchMode = ObjectMatchMode.ByHierarchy,
                    recordPropertyOverridesOfMatches = !path.Contains("/Visuals/"),
                    componentsNotMatchedBecomesOverride = true,
                    gameObjectsNotMatchedBecomesOverride = true,
                    changeRootNameToAssetName = false
                }, InteractionMode.AutomatedAction);
            root.SetActive(active);
            root.layer = layer;
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
            foreach (SavedProperty saved in bindings)
            {
                if (saved.owner == null) throw new InvalidOperationException("Migration lost binding " + saved.path + " on " + path);
                var data = new SerializedObject(saved.owner);
                var p = data.FindProperty(saved.path);
                if (saved.text != null) p.stringValue = saved.text;
                else p.objectReferenceValue = saved.reference;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(saved.owner);
            }
        }

        static void ConfigureLooks(Scene scene)
        {
            PlayerAppearance appearance = All(scene).Select(g => g.GetComponent<PlayerAppearance>()).FirstOrDefault(c => c != null);
            if (appearance == null) return;
            GameObject rogue = Get(appearance, "rogueVisual") as GameObject;
            CharacterAnimationDriver template = rogue.GetComponentInChildren<CharacterAnimationDriver>(true);
            var data = new SerializedObject(appearance);
            var looks = data.FindProperty("looks");
            for (int i = 0; i < looks.arraySize; i++)
            {
                var look = looks.GetArrayElementAtIndex(i);
                var model = look.FindPropertyRelative("model");
                string source = AssetDatabase.GetAssetPath(model.objectReferenceValue);
                if (!Vendor(source)) continue;
                string path = "Assets/Topaz/Characters/Prefabs/Visuals/" + SemanticName(source) + " Player.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    var root = new GameObject(SemanticName(source));
                    GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model.objectReferenceValue, root.transform);
                    visual.name = "Visual";
                    Material material = look.FindPropertyRelative("material").objectReferenceValue as Material;
                    foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
                        r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
                    Animator animator = visual.GetComponent<Animator>();
                    animator.avatar = look.FindPropertyRelative("avatar").objectReferenceValue as Avatar;
                    animator.runtimeAnimatorController = Get(appearance, "playerController") as RuntimeAnimatorController;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var driver = visual.AddComponent<CharacterAnimationDriver>();
                    EditorUtility.CopySerialized(template, driver);
                    foreach (string f in new[] { "player", "playerCombat", "enemy", "enemyAgent" }) Set(driver, f, null);
                    ConfigureBindings(root, visual);
                    CharacterVisual cv = root.GetComponent<CharacterVisual>();
                    foreach (string field in EquipmentFields)
                    {
                        GameObject held = Get(template, field) as GameObject;
                        if (held == null) continue;
                        GameObject heldAsset = PrefabUtility.GetCorrespondingObjectFromSource(held);
                        if (heldAsset == null) throw new InvalidOperationException("Equipment is not a prefab: " + held.name);
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(heldAsset,
                            field == "shieldVisual" ? cv.LeftHand : cv.RightHand);
                        instance.name = held.name;
                        instance.transform.localPosition = held.transform.localPosition;
                        instance.transform.localRotation = held.transform.localRotation;
                        instance.transform.localScale = held.transform.localScale;
                        instance.SetActive(false);
                        Set(driver, field, instance);
                        if (field == "shieldVisual") Set(cv, "shield", instance);
                    }
                    visual.AddComponent<ShieldGuardPose>();
                    EnsureFolder(Path.GetDirectoryName(path));
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                    Object.DestroyImmediate(root);
                }
                model.objectReferenceValue = prefab;
                defaults[source] = path;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ExtractOwners(Scene scene)
        {
            var ownerTypes = new HashSet<string> { "HarvestTree", "MiningRock", "EnemyCombatant", "StorageChest",
                "Campfire", "WorldPickup", "EquipmentRack", "CryptLootCache", "FeelStudyPlayer", "MainMenuStage" };
            foreach (GameObject root in All(scene).Where(g => g.GetComponents<MonoBehaviour>()
                .Any(c => c != null && ownerTypes.Contains(c.GetType().Name))).OrderByDescending(g => Depth(g.transform)).ToArray())
            {
                if (PrefabUtility.IsPartOfPrefabInstance(root)) continue;
                Component owner = root.GetComponents<MonoBehaviour>().First(c => c != null && ownerTypes.Contains(c.GetType().Name));
                string definition = AssetDatabase.GetAssetPath(Get(owner, "definition"));
                string visuals = string.Join("|", root.GetComponentsInChildren<Transform>(true)
                    .Select(t => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)).Distinct());
                string path = "Assets/Topaz/" + (owner.GetType().Name == "EnemyCombatant" ? "Gameplay/Combat" : "Gameplay/WorldLoop") +
                    "/Prefabs/" + owner.GetType().Name + " " + Path.GetFileNameWithoutExtension(definition) + " " +
                    Hash128.Compute(visuals).ToString().Substring(0, 8) + ".prefab";
                if (owner.GetType().Name == "FeelStudyPlayer") path = "Assets/Topaz/Player/Prefabs/Player.prefab";
                if (owner.GetType().Name == "MainMenuStage") path = "Assets/Topaz/UI/Menus/Prefabs/Main Menu Stage.prefab";
                Connect(root, path);
            }
            foreach (MonoBehaviour builds in All(scene).SelectMany(g => g.GetComponents<MonoBehaviour>())
                .Where(c => c != null && c.GetType().Name == "HomeBuilds").ToArray())
            foreach (string field in new[] { "pathTemplate", "anvilTemplate" })
            {
                GameObject template = Get(builds, field) as GameObject;
                if (template == null || EditorUtility.IsPersistent(template)) continue;
                string path = "Assets/Topaz/Gameplay/WorldLoop/Prefabs/" +
                    (field == "pathTemplate" ? "Stone Path" : "Blacksmith Anvil") + ".prefab";
                template.SetActive(true);
                if (!PrefabUtility.IsPartOfPrefabInstance(template)) Connect(template, path);
                Set(builds, field, AssetDatabase.LoadAssetAtPath<GameObject>(path));
                Object.DestroyImmediate(template);
            }
        }

        static void ExtractGeometry(Scene scene)
        {
            foreach (MeshFilter mesh in All(scene).Select(g => g.GetComponent<MeshFilter>()).Where(c => c != null).ToArray())
            {
                if (PrefabUtility.IsPartOfPrefabInstance(mesh.gameObject) || mesh.GetComponent<CanvasRenderer>() != null) continue;
                GameObject root = mesh.gameObject;
                string role = Regex.Replace(root.name, @"[0-9]+$", "").Trim().Replace('/', '-');
                Connect(root, "Assets/Topaz/World/Environment/Prefabs/Geometry/" + role + ".prefab");
            }
        }

        static void MigrateAssetReferences(IEnumerable<Object> objects)
        {
            foreach (Object target in objects.Where(o => o != null))
            {
                var data = new SerializedObject(target);
                var p = data.GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || !(p.objectReferenceValue is GameObject model)) continue;
                    string source = AssetDatabase.GetAssetPath(model);
                    if (!Vendor(source)) continue;
                    if (!defaults.TryGetValue(source, out string path))
                    {
                        path = "Assets/Topaz/" + (IsEquipment(source) ? "Gameplay/Combat" : "World/Environment") +
                            "/Prefabs/Visuals/" + SemanticName(source) + ".prefab";
                        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                        {
                            var root = new GameObject(SemanticName(source));
                            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                            visual.name = "Visual";
                            EnsureFolder(Path.GetDirectoryName(path));
                            PrefabUtility.SaveAsPrefabAsset(root, path);
                            Object.DestroyImmediate(root);
                        }
                        defaults[source] = path;
                    }
                    p.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
                if (data.ApplyModifiedPropertiesWithoutUndo()) EditorUtility.SetDirty(target);
            }
        }

        static bool IsEquipment(string source) => source.Contains("Weapons/") || source.Contains("Skeletons/Models/") ||
            source.Contains("Adventurers/Models/") || Regex.IsMatch(source, @"/(axe|pickaxe|lantern)\.fbx$");
        static string SemanticName(string source)
        {
            string name = Path.GetFileNameWithoutExtension(source);
            switch (name)
            {
                case "Tree_3_B_Color1": return "Broadleaf Tree";
                case "Tree_4_A_Color1": return "Pine Tall";
                case "Tree_4_B_Color1": return "Pine Medium";
                case "Tree_4_C_Color1": return "Pine Small";
                case "Tree_Bare_1_A_Color1": return "Bare Tree";
                case "Rock_3_A_Color1": return "Boulder Tall";
                case "Rock_3_B_Color1": return "Boulder Round";
                case "Rock_3_C_Color1": return "Boulder Flat";
                case "Grass_1_A_Color1": return "Grass Short";
                case "Grass_1_C_Color1": return "Grass Short Meadow";
                case "Grass_2_A_Color1": return "Grass Tall";
                case "Grass_2_C_Color1": return "Grass Tall Meadow";
                case "Bush_1_A_Color1": return "Bush Low";
                case "Bush_2_A_Color1": return "Bush Tall";
                case "sword_A": return "Starter Sword";
                case "axe_2handed": return "Battle Axe";
                case "shelf_A_small": return "Equipment Shelf";
                case "Skeleton_Minion": return "Skeleton Melee";
            }
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Replace('_', ' '));
        }
        static Object Get(Object target, string name) => new SerializedObject(target).FindProperty(name)?.objectReferenceValue;
        static void Set(Object target, string name, Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(name).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
