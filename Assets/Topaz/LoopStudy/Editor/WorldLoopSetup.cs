using System;
using System.IO;
using System.Linq;
using TMPro;
using Topaz.CombatStudy;
using Topaz.FeelStudy;
using Topaz.LoopStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Author the first persistent gathering and homestead loop.</summary>
    public static class WorldLoopSetup
    {
        const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        const string ControlsPath = "Assets/Topaz/Input/TopazControls.inputactions";
        const string Definitions = "Assets/Topaz/LoopStudy/Definitions";
        const string Materials = "Assets/Topaz/LoopStudy/Materials";

        [MenuItem("Topaz/Build World Loop Study")]
        public static void Configure()
        {
            EnsureBindings();
            ItemDefinition wood = Asset<ItemDefinition>($"{Definitions}/Wood.asset");
            HarvestDefinition harvest = Asset<HarvestDefinition>($"{Definitions}/Tree.asset");
            StructureDefinition chestDefinition = Asset<StructureDefinition>($"{Definitions}/StorageChest.asset");
            RecipeDefinition recipe = Asset<RecipeDefinition>($"{Definitions}/StorageChestRecipe.asset");
            MeleeAttackDefinition axe = Asset<MeleeAttackDefinition>($"{Definitions}/AxeChop.asset");
            Ref(harvest, "yieldItem", wood);
            Ref(recipe, "ingredient", wood);
            Ref(recipe, "result", chestDefinition);
            Value(axe, "stableId", "axe.chop");
            Value(axe, "windupSeconds", 0.26f);
            Value(axe, "activeSeconds", 0.12f);
            Value(axe, "recoverySeconds", 0.34f);
            Value(axe, "range", 2.1f);
            Value(axe, "arcDegrees", 90f);

            Material trunk = Mat("TreeTrunk", new Color(.46f, .32f, .22f));
            Material leaves = Mat("TreeCanopy", new Color(.39f, .67f, .43f));
            Material woodMat = Mat("Woodwork", new Color(.68f, .48f, .30f));
            Material bedMat = Mat("RestPoint", new Color(.50f, .68f, .78f));
            Material ghostMat = Mat("Placement", new Color(.40f, .9f, .60f));
            Material axeMat = Mat("Axe", new Color(.77f, .85f, .79f));
            Material arcMat = Mat("AxeArc", new Color(.80f, .89f, .42f), false);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("Player");
            GameObject feel = GameObject.Find("Feel Study");
            SafeZone home = UnityEngine.Object.FindFirstObjectByType<SafeZone>();
            if (player == null || feel == null || home == null)
                throw new InvalidOperationException("Build the combat study before the world loop.");
            GameObject prior = GameObject.Find("World Loop Study");
            if (prior != null) UnityEngine.Object.DestroyImmediate(prior);
            var root = new GameObject("World Loop Study");
            root.transform.SetParent(feel.transform, false);
            foreach (Transform child in feel.transform)
                if (child.name == "Practice Resource") child.gameObject.SetActive(false);

            GameObject treeObject = new GameObject("Authored Tree 01");
            treeObject.transform.SetParent(root.transform, false);
            treeObject.transform.position = new Vector3(-5f, 0f, 3.4f);
            var treeCollider = treeObject.AddComponent<CapsuleCollider>();
            treeCollider.center = new Vector3(0, 1.1f, 0);
            treeCollider.radius = .43f;
            treeCollider.height = 2.2f;
            var obstacle = treeObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.radius = .48f;
            obstacle.height = 2.2f;
            obstacle.carving = true;
            var treeVisual = new GameObject("Tree Visual");
            treeVisual.transform.SetParent(treeObject.transform, false);
            Renderer trunkRenderer = Primitive("Trunk", PrimitiveType.Cylinder, treeVisual.transform,
                new Vector3(0, 1f, 0), new Vector3(.36f, 1f, .36f), trunk);
            Primitive("Canopy", PrimitiveType.Sphere, treeVisual.transform,
                new Vector3(0, 2.65f, 0), new Vector3(2f, 1.9f, 2f), leaves);
            HarvestTree tree = treeObject.AddComponent<HarvestTree>();
            Ref(tree, "definition", harvest);
            Ref(tree, "visualRoot", treeVisual);
            Ref(tree, "trunkRenderer", trunkRenderer);
            Ref(tree, "trunkCollider", treeCollider);
            Ref(tree, "obstacle", obstacle);

            GameObject bench = new GameObject("Workbench");
            bench.transform.SetParent(root.transform, false);
            bench.transform.position = new Vector3(-1.6f, 0, -1.1f);
            Primitive("Table", PrimitiveType.Cube, bench.transform, new Vector3(0,.52f,0),
                new Vector3(1.05f,.20f,.62f), woodMat);
            Primitive("Tool Block", PrimitiveType.Cube, bench.transform, new Vector3(0,.70f,0),
                new Vector3(.36f,.18f,.28f), axeMat);
            GameObject rest = new GameObject("Rest Point");
            rest.transform.SetParent(root.transform, false);
            rest.transform.position = new Vector3(1.5f, 0, -1.05f);
            Primitive("Bedroll", PrimitiveType.Cube, rest.transform, new Vector3(0,.09f,0),
                new Vector3(.9f,.15f,1.5f), bedMat);

            GameObject chestObject = new GameObject("Storage Chest");
            chestObject.transform.SetParent(root.transform, false);
            Renderer chestRenderer = ChestShape(chestObject.transform, woodMat, axeMat);
            StorageChest chest = chestObject.AddComponent<StorageChest>();
            Ref(chest, "definition", chestDefinition);
            Ref(chest, "chestRenderer", chestRenderer);
            GameObject preview = new GameObject("Chest Placement Preview");
            preview.transform.SetParent(root.transform, false);
            Renderer previewRenderer = ChestShape(preview.transform, ghostMat, ghostMat);
            preview.SetActive(false);

            Transform facing = player.transform.Find("Facing Visual");
            var axePivot = new GameObject("Axe Pivot");
            axePivot.transform.SetParent(facing, false);
            axePivot.transform.localPosition = new Vector3(.39f, 1.04f, .18f);
            Primitive("Handle", PrimitiveType.Cube, axePivot.transform,
                new Vector3(0,0,.66f), new Vector3(.12f,.12f,1.25f), woodMat);
            Primitive("Head", PrimitiveType.Cube, axePivot.transform,
                new Vector3(0,0,1.23f), new Vector3(.52f,.20f,.22f), axeMat);
            axePivot.SetActive(false);

            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            FeelStudyPlayer movement = player.GetComponent<FeelStudyPlayer>();
            WorldSession oldSession = player.GetComponent<WorldSession>();
            if (oldSession != null) UnityEngine.Object.DestroyImmediate(oldSession);
            WorldSession session = player.AddComponent<WorldSession>();
            Ref(combat, "axeAttack", axe);
            Ref(combat, "tree", tree);
            Ref(combat, "axePivot", axePivot.transform);
            Ref(combat, "swordArcMaterial", AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Topaz/CombatStudy/Materials/SwordArc.mat"));
            Ref(combat, "axeArcMaterial", arcMat);

            LoopHud hud = CreateHud(root.transform);
            Ref(session, "wood", wood);
            Ref(session, "tree", tree);
            Ref(session, "chestRecipe", recipe);
            Ref(session, "chestDefinition", chestDefinition);
            Ref(session, "chest", chest);
            Ref(session, "home", home);
            Ref(session, "workbench", bench.transform);
            Ref(session, "restPoint", rest.transform);
            Ref(session, "movement", movement);
            Ref(session, "combat", combat);
            Ref(session, "controls", controls);
            Ref(session, "hud", hud);
            Ref(session, "chestPreview", preview);
            Ref(session, "previewRenderer", previewRenderer);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] World loop study authored.");
        }

        static void EnsureBindings()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            InputActionMap map = asset.FindActionMap("Player", true);
            Add(map, "EquipSword", "<Keyboard>/1");
            Add(map, "EquipAxe", "<Keyboard>/2");
            Add(map, "CycleTool", "<Keyboard>/tab", "<Gamepad>/buttonNorth");
            Add(map, "Place", "<Mouse>/leftButton", "<Gamepad>/buttonSouth");
            Add(map, "Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            File.WriteAllText(ControlsPath, asset.ToJson());
            AssetDatabase.ImportAsset(ControlsPath, ImportAssetOptions.ForceSynchronousImport);
        }

        static void Add(InputActionMap map, string name, params string[] paths)
        {
            if (map.FindAction(name) != null) return;
            InputAction action = map.AddAction(name, InputActionType.Button);
            foreach (string path in paths) action.AddBinding(path);
        }

        static LoopHud CreateHud(Transform parent)
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null) throw new InvalidOperationException("Import TMP Essential Resources first.");
            var canvasObject = new GameObject("Loop HUD", typeof(RectTransform),
                typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080);
            scaler.matchWidthOrHeight = .5f;
            var eventObject = new GameObject("Loop UI Event System", typeof(UnityEngine.EventSystems.EventSystem),
                typeof(InputSystemUIInputModule));
            eventObject.transform.SetParent(parent, false);
            eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            RectTransform top = Box("Resources", canvasObject.transform, new Vector2(0,1),
                new Vector2(0,1), new Vector2(32,-28), new Vector2(410,205), new Color(.08f,.12f,.15f,.78f));
            var vertical = top.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vertical.padding = new RectOffset(18,14,12,12);
            vertical.spacing = 3;
            vertical.childForceExpandHeight = false;
            vertical.childControlHeight = true;
            TMP_Text day = Text("Day", top, font, 30);
            TMP_Text wood = Text("Wood", top, font, 25);
            TMP_Text logging = Text("Logging", top, font, 24);
            TMP_Text tool = Text("Tool", top, font, 23);

            TMP_Text context = Text("Context", canvasObject.transform, font, 27);
            Anchor(context.rectTransform, new Vector2(.5f,0), new Vector2(.5f,0),
                new Vector2(0,38), new Vector2(1250,50));
            context.alignment = TextAlignmentOptions.Center;
            TMP_Text status = Text("Status", canvasObject.transform, font, 26);
            Anchor(status.rectTransform, new Vector2(.5f,1), new Vector2(.5f,1),
                new Vector2(0,-33), new Vector2(1100,50));
            status.alignment = TextAlignmentOptions.Center;
            status.color = new Color(.87f,.94f,.72f);

            RectTransform craft = Panel("Workbench", canvasObject.transform, font,
                out TMP_Text craftDescription, out UnityEngine.UI.Button craftButton,
                out UnityEngine.UI.Button craftClose, "Craft chest", "Close");
            RectTransform storage = Panel("Storage Chest", canvasObject.transform, font,
                out TMP_Text storageDescription, out UnityEngine.UI.Button deposit,
                out UnityEngine.UI.Button storageClose, "Deposit all Wood", "Close");
            UnityEngine.UI.Button withdraw = Button("Withdraw all Wood", storage, font);
            withdraw.transform.SetSiblingIndex(3);
            LoopHud hud = canvasObject.AddComponent<LoopHud>();
            Ref(hud, "dayLabel", day);
            Ref(hud, "woodLabel", wood);
            Ref(hud, "loggingLabel", logging);
            Ref(hud, "toolLabel", tool);
            Ref(hud, "contextLabel", context);
            Ref(hud, "statusLabel", status);
            Ref(hud, "craftDescription", craftDescription);
            Ref(hud, "chestDescription", storageDescription);
            Ref(hud, "craftPanel", craft.gameObject);
            Ref(hud, "chestPanel", storage.gameObject);
            Ref(hud, "craftButton", craftButton);
            Ref(hud, "depositButton", deposit);
            Ref(hud, "withdrawButton", withdraw);
            Ref(hud, "craftCloseButton", craftClose);
            Ref(hud, "chestCloseButton", storageClose);
            craft.gameObject.SetActive(false);
            storage.gameObject.SetActive(false);
            return hud;
        }

        static RectTransform Panel(string title, Transform parent, TMP_FontAsset font,
            out TMP_Text description, out UnityEngine.UI.Button primary,
            out UnityEngine.UI.Button close, string primaryName, string closeName)
        {
            RectTransform box = Box(title, parent, new Vector2(.5f,.5f), new Vector2(.5f,.5f),
                Vector2.zero, new Vector2(510,345), new Color(.09f,.15f,.18f,.96f));
            var layout = box.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(30,30,24,24);
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            TMP_Text heading = Text(title, box, font, 34);
            heading.color = new Color(.77f,.92f,.97f);
            description = Text("Description", box, font, 26);
            primary = Button(primaryName, box, font);
            close = Button(closeName, box, font);
            return box;
        }

        static UnityEngine.UI.Button Button(string name, Transform parent, TMP_FontAsset font)
        {
            RectTransform rect = Box(name, parent, Vector2.zero, Vector2.zero,
                Vector2.zero, new Vector2(430,55), new Color(.22f,.47f,.53f,1));
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>();
            TMP_Text label = Text(name + " Label", rect, font, 26);
            label.text = name;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.color = Color.white;
            label.text = name;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            var element = go.AddComponent<UnityEngine.UI.LayoutElement>();
            element.preferredHeight = size * 1.5f;
            return label;
        }

        static RectTransform Box(string name, Transform parent, Vector2 anchorsMin, Vector2 anchorsMax,
            Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorsMin;
            rect.anchorMax = anchorsMax;
            rect.pivot = new Vector2(anchorsMin.x == 0 ? 0 : .5f, anchorsMin.y == 1 ? 1 : .5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            go.GetComponent<UnityEngine.UI.Image>().color = color;
            return rect;
        }

        static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        static Renderer ChestShape(Transform parent, Material body, Material accent)
        {
            Renderer renderer = Primitive("Body", PrimitiveType.Cube, parent,
                new Vector3(0,.30f,0), new Vector3(.72f,.55f,.5f), body);
            Primitive("Lid", PrimitiveType.Cube, parent,
                new Vector3(0,.62f,0), new Vector3(.80f,.15f,.57f), accent);
            return renderer;
        }

        static Renderer Primitive(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go.GetComponent<Renderer>();
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            T result = AssetDatabase.LoadAssetAtPath<T>(path);
            if (result != null) return result;
            result = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        static Material Mat(string name, Color color, bool lit = true)
        {
            string path = $"{Materials}/{name}.mat";
            Material result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result != null) return result;
            Shader shader = Shader.Find(lit ? "Universal Render Pipeline/Lit" :
                "Universal Render Pipeline/Unlit");
            result = new Material(shader) { name = name };
            result.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        static void Ref(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var objectView = new SerializedObject(target);
            SerializedProperty field = objectView.FindProperty(name);
            if (field == null) throw new InvalidOperationException($"Missing {name} on {target.name}");
            field.objectReferenceValue = value;
            objectView.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Value(UnityEngine.Object target, string name, string value)
        {
            var objectView = new SerializedObject(target);
            objectView.FindProperty(name).stringValue = value;
            objectView.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Value(UnityEngine.Object target, string name, float value)
        {
            var objectView = new SerializedObject(target);
            objectView.FindProperty(name).floatValue = value;
            objectView.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
