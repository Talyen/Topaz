using System;
using System.IO;
using System.Linq;
using Topaz.AnimationStudy;
using Topaz.CombatStudy;
using Topaz.CombatStudy.Editor;
using Topaz.LoopStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Topaz.Editor
{
    /// <summary>Idempotent authored equipment slice for Bootstrap.</summary>
    public static class EquipmentSetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string ItemsPath = "Assets/Topaz/Gameplay/WorldLoop/Definitions/Equipment";
        const string ControlsPath = "Assets/Topaz/Core/Input/TopazControls.inputactions";

        [MenuItem("Topaz/Apply Equipment System")]
        public static void Apply()
        {
            EnsureFolder(ItemsPath);
            EquipmentIconSetup.Generate();
            ItemDefinition[] items = {
                Item(EquipmentState.Sword, "Practice Sword", EquipmentSlot.Weapon, 1, 0, 0, 0, 0, 0),
                Item(EquipmentState.TwoHandedAxe, "Two-Handed Axe", EquipmentSlot.Weapon,
                    2, 0, 0, 0, 0, 0),
                Item(EquipmentState.Axe, "Wood Axe", EquipmentSlot.Tool, 0, 0, 0, 0, 0, 1),
                Item(EquipmentState.Shield, "Round Shield", EquipmentSlot.Offhand, 0, 0, 0, 0, 0, 0),
                Item(EquipmentState.Helm, "Field Helm", EquipmentSlot.Head, 0, 0, 1, 0, 0, 0),
                Item(EquipmentState.Body, "Field Coat", EquipmentSlot.Body, 0, 0, 1, 0, 0, 0),
                Item(EquipmentState.Gloves, "Field Gloves", EquipmentSlot.Hands, 1, 0, 0, 0, 0, 0),
                Item(EquipmentState.Boots, "Field Boots", EquipmentSlot.Boots, 0, 0, 0, 1, 0, 0),
                Item(EquipmentState.SwiftGloves, "Swift Gloves", EquipmentSlot.Hands, 0, 1, 0, 0, 0, 0),
                Item(EquipmentState.AgileBody, "Agile Coat", EquipmentSlot.Body, 0, 0, 0, 0, 1, 0),
                Item(EquipmentState.AgileBoots, "Agile Boots", EquipmentSlot.Boots, 0, 0, 0, 0, 1, 0)
            };
            WeaponTypesSetup.Configure(items);
            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null) throw new InvalidOperationException("Topaz input actions are missing.");
            InputActionMap playerMap = controls.FindActionMap("Player", true);
            foreach (string obsolete in new[] { "EquipSword", "EquipAxe", "CycleTool" })
                playerMap.FindAction(obsolete)?.RemoveAction();
            InputAction block = playerMap.FindAction("Block") ??
                playerMap.AddAction("Block", InputActionType.Button);
            if (!block.bindings.Any(binding => binding.path == "<Mouse>/rightButton"))
                block.AddBinding("<Mouse>/rightButton");
            if (!block.bindings.Any(binding => binding.path == "<Gamepad>/leftTrigger"))
                block.AddBinding("<Gamepad>/leftTrigger");
            File.WriteAllText(ControlsPath, controls.ToJson());
            AssetDatabase.ImportAsset(ControlsPath, ImportAssetOptions.ForceUpdate);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("Player");
            GameObject canvas = GameObject.Find("Loop HUD");
            if (player == null || canvas == null)
                throw new InvalidOperationException("Bootstrap player or HUD is missing.");
            WorldSession session = player.GetComponent<WorldSession>();
            AudioSource weaponAudio = player.GetComponent<AudioSource>();
            if (weaponAudio == null) weaponAudio = player.AddComponent<AudioSource>();
            weaponAudio.playOnAwake = false;
            weaponAudio.spatialBlend = 0f;
            weaponAudio.volume = 0.45f;
            Set(player.GetComponent<PlayerCombat>(), "weaponAudio", weaponAudio);
            SetArray(session, "equipmentItems", items);
            Transform rack = AuthorRack();
            Set(session, "gearRack", rack);
            Set(player.GetComponent<PlayerVitality>(), "maximumHealth", 6);
            AttachShield(player.GetComponent<PlayerAppearance>());
            GeneratePortraits(player.GetComponent<PlayerAppearance>());
            Topaz.UI.TopazUiTheme theme = AssetDatabase.LoadAssetAtPath<Topaz.UI.TopazUiTheme>(
                "Assets/Topaz/UI/Themes/TopazUiTheme.asset");
            UiDesignSystemSetup.ApplyToCanvas(canvas.transform, theme);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            foreach (string name in new[] { "PracticeEnemy", "ExpeditionScout", "ExpeditionGuardian" })
            {
                string[] guids = AssetDatabase.FindAssets(name + " t:EnemyDefinition");
                foreach (string guid in guids)
                {
                    var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                        AssetDatabase.GUIDToAssetPath(guid));
                    var values = new SerializedObject(enemy);
                    SerializedProperty health = values.FindProperty("health");
                    if (health != null) health.intValue = name == "PracticeEnemy" ? 15 :
                        name == "ExpeditionScout" ? 10 : 20;
                    values.FindProperty("damage").intValue = 4;
                    values.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Topaz/Equipment] Equipment items, rack, shield, input, and journal authored.");
        }

        static ItemDefinition Item(string id, string label, EquipmentSlot slot,
            int attack, int attackSpeed, int armor, int moveSpeed, int dodge, int logging)
        {
            string path = $"{ItemsPath}/{id}.asset";
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }
            var data = new SerializedObject(item);
            data.FindProperty("stableId").stringValue = id;
            data.FindProperty("displayName").stringValue = label;
            data.FindProperty("maxStack").intValue = 1;
            data.FindProperty("equipmentSlot").enumValueIndex = (int)slot;
            data.FindProperty("attack").intValue = attack;
            data.FindProperty("attackSpeed").intValue = attackSpeed;
            data.FindProperty("armor").intValue = armor;
            data.FindProperty("moveSpeed").intValue = moveSpeed;
            data.FindProperty("dodge").intValue = dodge;
            data.FindProperty("logging").intValue = logging;
            data.FindProperty("journalIcon").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(EquipmentIconSetup.Path(slot));
            data.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        static Transform AuthorRack()
        {
            Transform root = GameObject.Find("World Loop Study")?.transform;
            if (root == null) throw new InvalidOperationException("World Loop Study is missing.");
            Transform rack = root.Find("Equipment Rack");
            if (rack == null)
            {
                var holder = new GameObject("Equipment Rack");
                rack = holder.transform;
                rack.SetParent(root, false);
            }
            rack.position = new Vector3(0.65f, 0f, 2.7f);
            if (rack.childCount == 0)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/ThirdParty/KayKit/Furniture/Models/shelf_A_small.fbx");
                if (model == null) throw new InvalidOperationException("Approved rack model is missing.");
                GameObject instance = PrefabUtility.InstantiatePrefab(model, rack) as GameObject;
                instance.name = "Gear Shelf";
                instance.transform.localScale = Vector3.one * 1.8f;
            }
            Transform shelf = rack.Find("Gear Shelf");
            if (shelf != null) shelf.localScale = Vector3.one * 1.8f;
            DisplayProp(rack, "Display Sword",
                "Assets/ThirdParty/KayKit/FantasyWeapons/Models/sword_A.fbx",
                "Assets/Topaz/Presentation/Art/Materials/Weapons.mat",
                new Vector3(-0.28f, 0.55f, 0f), Quaternion.Euler(0f, 0f, -65f), 0.65f);
            DisplayProp(rack, "Display Shield",
                "Assets/ThirdParty/KayKit/Adventurers/Models/shield_round.fbx",
                "Assets/Topaz/Presentation/Art/Materials/Rogue.mat",
                new Vector3(0.30f, 0.48f, 0f), Quaternion.Euler(0f, 90f, 0f), 0.8f);
            DisplayProp(rack, "Display Combat Axe",
                "Assets/ThirdParty/KayKit/Adventurers/Models/axe_2handed.fbx",
                "Assets/Topaz/Presentation/Art/Materials/Weapons.mat",
                new Vector3(0f, 0.72f, 0.04f), Quaternion.Euler(0f, 0f, 55f), 0.8f);
            if (rack.GetComponent<BoxCollider>() == null)
            {
                BoxCollider collider = rack.gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.6f, 0f);
                collider.size = new Vector3(1.3f, 1.2f, 0.65f);
            }
            return rack;
        }

        static void DisplayProp(Transform parent, string name, string modelPath,
            string materialPath, Vector3 position, Quaternion rotation, float scale)
        {
            if (parent.Find(name) != null) return;
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (model == null || material == null)
                throw new InvalidOperationException("Gear rack display asset is missing: " + modelPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(model, parent) as GameObject;
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = Vector3.one * scale;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Enumerable.Repeat(material,
                    Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
        }

        static void AttachShield(PlayerAppearance appearance)
        {
            if (appearance == null) throw new InvalidOperationException("Player appearance is missing.");
            var appearanceData = new SerializedObject(appearance);
            Transform rogue = ((GameObject)appearanceData.FindProperty("rogueVisual")
                .objectReferenceValue).transform;
            Transform left = rogue.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "handslot.l");
            if (left == null) throw new InvalidOperationException("Rogue left hand is missing.");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ThirdParty/KayKit/Adventurers/Models/shield_round.fbx");
            GameObject instance = left.Find("Held Shield")?.gameObject ??
                PrefabUtility.InstantiatePrefab(prefab, left) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not attach shield.");
            instance.name = "Held Shield";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            instance.transform.localScale = Vector3.one * 0.8f;
            Material weapon = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Topaz/Presentation/Art/Materials/Rogue.mat");
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Enumerable.Repeat(weapon,
                    Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
            instance.SetActive(false);
            CharacterAnimationDriver driver = rogue.GetComponent<CharacterAnimationDriver>();
            if (driver != null) Set(driver, "shieldVisual", instance);
            ShieldGuardPose pose = rogue.GetComponent<ShieldGuardPose>() ??
                rogue.gameObject.AddComponent<ShieldGuardPose>();
            pose.Bind(appearance.GetComponent<PlayerCombat>());
        }

        static void GeneratePortraits(PlayerAppearance appearance)
        {
            const string folder = "Assets/Topaz/UI/Art/EquipmentPortraits";
            EnsureFolder(folder);
            var data = new SerializedObject(appearance);
            SerializedProperty looks = data.FindProperty("looks");
            for (int i = 0; i < looks.arraySize; i++)
            {
                SerializedProperty look = looks.GetArrayElementAtIndex(i);
                string id = look.FindPropertyRelative("id").stringValue;
                string path = folder + "/" + id.Replace('.', '-') + ".png";
                GameObject model = look.FindPropertyRelative("model").objectReferenceValue as GameObject;
                Material material = look.FindPropertyRelative("material").objectReferenceValue as Material;
                if (model == null || material == null) continue;
                var preview = new PreviewRenderUtility();
                GameObject instance = null;
                try
                {
                    instance = UnityEngine.Object.Instantiate(model);
                    instance.hideFlags = HideFlags.HideAndDontSave;
                    preview.AddSingleGO(instance);
                    foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
                        animator.enabled = false;
                    AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        "Assets/Topaz/Characters/Animation/Clips/Idle.anim");
                    if (idle != null) idle.SampleAnimation(instance, 0f);
                    Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0) continue;
                    foreach (Renderer renderer in renderers)
                        renderer.sharedMaterials = Enumerable.Repeat(material,
                            Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
                    Bounds bounds = renderers[0].bounds;
                    foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    Camera camera = preview.camera;
                    camera.orthographic = true;
                    camera.orthographicSize = Math.Max(0.5f, bounds.extents.y * 1.35f);
                    camera.nearClipPlane = 0.01f;
                    camera.farClipPlane = 100f;
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(0.16f, 0.23f, 0.22f);
                    camera.transform.rotation = Quaternion.Euler(8f, 155f, 0f);
                    camera.transform.position = bounds.center - camera.transform.forward * 12f;
                    preview.lights[0].intensity = 1.4f;
                    preview.lights[0].transform.rotation = Quaternion.Euler(35f, -35f, 0f);
                    preview.lights[1].intensity = 0.6f;
                    preview.lights[1].transform.rotation = Quaternion.Euler(30f, 130f, 0f);
                    preview.ambientColor = new Color(0.55f, 0.59f, 0.54f);
                    preview.BeginStaticPreview(new Rect(0, 0, 420, 560));
                    preview.Render(true, false);
                    Texture2D image = preview.EndStaticPreview();
                    try { File.WriteAllBytes(path, image.EncodeToPNG()); }
                    finally { UnityEngine.Object.DestroyImmediate(image); }
                    AssetDatabase.ImportAsset(path);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    importer.textureType = TextureImporterType.Sprite;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
                finally
                {
                    if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                    preview.Cleanup();
                }
            }
        }

        static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object target, string field, int value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).intValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(UnityEngine.Object target, string field, ItemDefinition[] values)
        {
            var data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            string parent = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = parent + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(parent, part);
                parent = next;
            }
        }
    }
}
