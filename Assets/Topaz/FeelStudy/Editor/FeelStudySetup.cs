using System;
using System.IO;
using System.Linq;
using Topaz.FeelStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Builds the small, fully replaceable movement and camera graybox.</summary>
    public static class FeelStudySetup
    {
        const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        const string ControlsPath = "Assets/Topaz/Input/TopazControls.inputactions";
        const string MaterialsPath = "Assets/Topaz/FeelStudy/Materials";

        [MenuItem("Topaz/Build Feel Study")]
        public static void Configure()
        {
            EnsureFolder(MaterialsPath);
            CreateControls();
            Material floor = CreateMaterial("Floor", new Color(0.19f, 0.23f, 0.26f));
            Material grid = CreateMaterial("Grid", new Color(0.30f, 0.35f, 0.38f));
            Material playerMaterial = CreateMaterial("Player", new Color(0.24f, 0.76f, 0.84f));
            Material nodeMaterial = CreateMaterial("Node", new Color(0.83f, 0.58f, 0.29f));
            Material marker = CreateMaterial("Marker", new Color(0.72f, 0.95f, 0.58f));
            Material barrier = CreateMaterial("Barrier", new Color(0.43f, 0.49f, 0.52f));

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject prior = GameObject.Find("Feel Study");
            if (prior != null) UnityEngine.Object.DestroyImmediate(prior);

            GameObject root = new GameObject("Feel Study");
            CreateGround(root.transform, floor, grid);
            CreateBarriers(root.transform, barrier);

            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform, false);
            player.transform.position = Vector3.zero;
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.height = 1.8f;
            characterController.radius = 0.36f;
            characterController.stepOffset = 0.25f;

            GameObject visual = new GameObject("Facing Visual");
            visual.transform.SetParent(player.transform, false);
            GameObject body = Primitive("Body", PrimitiveType.Capsule, visual.transform,
                new Vector3(0f, 0.9f, 0f), new Vector3(0.45f, 0.85f, 0.45f), playerMaterial, false);
            Primitive("Facing Notch", PrimitiveType.Cube, visual.transform,
                new Vector3(0f, 1.15f, 0.45f), new Vector3(0.18f, 0.15f, 0.55f), marker, false);
            GameObject aimMarker = Primitive("Aim Marker", PrimitiveType.Sphere, root.transform,
                new Vector3(0f, 0.08f, 4.2f), new Vector3(0.38f, 0.05f, 0.38f), marker, false);

            PracticeNode[] nodes =
            {
                CreateNode(root.transform, new Vector3(4f, 0f, 2f), nodeMaterial, marker),
                CreateNode(root.transform, new Vector3(-5f, 0f, -4f), nodeMaterial, marker),
                CreateNode(root.transform, new Vector3(0f, 0f, 7f), nodeMaterial, marker)
            };

            Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null) throw new InvalidOperationException("Bootstrap scene has no camera.");
            camera.orthographic = true;
            camera.orthographicSize = 9f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.15f, 0.18f);
            camera.transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
            camera.transform.position = -camera.transform.forward * 22f;

            GameObject volume = GameObject.Find("Global Volume");
            if (volume != null) volume.SetActive(false);

            // Asset creation above can reimport .inputactions; acquire its final persistent instance.
            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null) throw new InvalidOperationException("Topaz controls asset is unavailable.");

            FeelStudyPlayer playerController = player.AddComponent<FeelStudyPlayer>();
            SetReference(playerController, "controls", controls);
            SetReference(playerController, "viewCamera", camera);
            SetReference(playerController, "visualRoot", visual.transform);
            SetReference(playerController, "bodyRenderer", body.GetComponent<Renderer>());
            SetReference(playerController, "aimMarker", aimMarker.transform);
            var serializedPlayer = new SerializedObject(playerController);
            SerializedProperty nodeArray = serializedPlayer.FindProperty("practiceNodes");
            nodeArray.arraySize = nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
                nodeArray.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            FeelStudyCamera cameraRig = camera.GetComponent<FeelStudyCamera>();
            if (cameraRig == null) cameraRig = camera.gameObject.AddComponent<FeelStudyCamera>();
            SetReference(cameraRig, "target", playerController);
            SetReference(cameraRig, "controls", controls);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Feel study scene is ready.");
        }

        static InputActionAsset CreateControls()
        {
            InputActionAsset existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "TopazControls";
            InputActionMap map = asset.AddActionMap("Player");

            InputAction move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");

            map.AddAction("AimPointer", InputActionType.Value, "<Mouse>/position", expectedControlLayout: "Vector2");
            map.AddAction("AimStick", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
            InputAction dodge = map.AddAction("Dodge", InputActionType.Button);
            dodge.AddBinding("<Keyboard>/space");
            dodge.AddBinding("<Gamepad>/buttonEast");
            InputAction interact = map.AddAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");
            map.AddAction("ZoomWheel", InputActionType.Value, "<Mouse>/scroll/y", expectedControlLayout: "Axis");
            map.AddAction("ZoomIn", InputActionType.Button, "<Gamepad>/rightShoulder");
            map.AddAction("ZoomOut", InputActionType.Button, "<Gamepad>/leftShoulder");

            File.WriteAllText(ControlsPath, asset.ToJson());
            UnityEngine.Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(ControlsPath, ImportAssetOptions.ForceSynchronousImport);
            existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (existing == null) throw new InvalidOperationException("Could not import Topaz input actions.");
            return existing;
        }

        static void CreateGround(Transform parent, Material floor, Material grid)
        {
            Primitive("Ground", PrimitiveType.Cube, parent,
                new Vector3(0f, -0.12f, 0f), new Vector3(32f, 0.24f, 32f), floor, true);
            for (int step = -6; step <= 6; step++)
            {
                float offset = step * 2.5f;
                Primitive($"Grid X {step}", PrimitiveType.Cube, parent,
                    new Vector3(offset, 0.005f, 0f), new Vector3(0.025f, 0.01f, 30f), grid, false);
                Primitive($"Grid Z {step}", PrimitiveType.Cube, parent,
                    new Vector3(0f, 0.005f, offset), new Vector3(30f, 0.01f, 0.025f), grid, false);
            }
        }

        static void CreateBarriers(Transform parent, Material material)
        {
            Vector3[] positions =
            {
                new Vector3(-3f, 0.6f, 4f), new Vector3(3f, 0.6f, -4f),
                new Vector3(7f, 0.6f, -1f), new Vector3(-7f, 0.6f, 1f)
            };
            for (int i = 0; i < positions.Length; i++)
                Primitive($"Obstacle {i + 1}", PrimitiveType.Cube, parent, positions[i],
                    new Vector3(1.5f, 1.2f, 1.5f), material, true);
        }

        static PracticeNode CreateNode(Transform parent, Vector3 position, Material body, Material ring)
        {
            GameObject root = new GameObject("Practice Resource");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            GameObject pillar = Primitive("Pillar", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, 0.65f, 0f), new Vector3(0.55f, 0.65f, 0.55f), body, true);
            GameObject highlight = Primitive("Interaction Range", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, 0.025f, 0f), new Vector3(1f, 0.012f, 1f), ring, false);
            highlight.SetActive(false);
            PracticeNode node = root.AddComponent<PracticeNode>();
            SetReference(node, "nodeRenderer", pillar.GetComponent<Renderer>());
            SetReference(node, "rangeRing", highlight);
            return node;
        }

        static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position,
            Vector3 scale, Material material, bool keepCollider)
        {
            GameObject result = GameObject.CreatePrimitive(type);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Collider collider = result.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            }
            return result;
        }

        static Material CreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0f);
            AssetDatabase.CreateAsset(material, path);
            return material;
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
            if (field == null) throw new InvalidOperationException($"Missing field {property} on {target.GetType().Name}.");
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (new SerializedObject(target).FindProperty(property).objectReferenceValue != value)
                throw new InvalidOperationException($"Could not assign {property} on {target.GetType().Name}.");
        }
    }
}
