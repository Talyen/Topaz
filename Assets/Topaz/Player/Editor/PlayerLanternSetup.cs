using System;
using TMPro;
using Topaz.AnimationStudy;
using Topaz.FeelStudy;
using Topaz.LoopStudy;
using Topaz.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Wires the permanent carried lantern into the existing player and journal.</summary>
    public static class PlayerLanternSetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        [MenuItem("Topaz/Apply Player Lantern")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("Player");
            GameObject canvas = GameObject.Find("Loop HUD");
            if (player == null || canvas == null)
                throw new InvalidOperationException("Bootstrap player or journal Canvas is missing.");

            FeelStudyPlayer movement = player.GetComponent<FeelStudyPlayer>();
            PlayerAppearance appearance = player.GetComponent<PlayerAppearance>();
            WorldSession session = player.GetComponent<WorldSession>();
            LoopHud hud = canvas.GetComponent<LoopHud>();
            Transform journal = canvas.transform.Find("Backpack/Open Journal");
            TopazUiTheme theme = AssetDatabase.LoadAssetAtPath<TopazUiTheme>(
                "Assets/Topaz/UI/Themes/TopazUiTheme.asset");
            if (movement == null || appearance == null || session == null || hud == null ||
                journal == null || theme == null)
                throw new InvalidOperationException("Player lantern setup requires the existing player and backpack journal.");

            PlayerLantern lantern = player.GetComponent<PlayerLantern>();
            if (lantern == null) lantern = player.AddComponent<PlayerLantern>();
            Transform visualRoot = new SerializedObject(movement)
                .FindProperty("visualRoot").objectReferenceValue as Transform;
            if (visualRoot == null) throw new InvalidOperationException("Player visual root is missing.");
            SetRef(lantern, "visualRoot", visualRoot);
            SetRef(lantern, "movement", movement);
            SetRef(lantern, "lanternModel", AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Topaz/Player/Prefabs/Visuals/Carried Lantern.prefab"));
            SetVector(lantern, "lightOffset", new Vector3(0f, 1.05f, 0f));
            SetFloat(lantern, "lightRange", 8.5f);
            SetRef(appearance, "lantern", lantern);
            SetRef(session, "lantern", lantern);

            UiDesignSystemSetup.EnsureLanternEntry(journal, hud, theme);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Player lantern and backpack toggle are ready.");
        }

        static void SetRef(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            if (value == null) throw new InvalidOperationException("Missing lantern asset: " + field);
            var serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException("Missing lantern field: " + field);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetVector(UnityEngine.Object owner, string field, Vector3 value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).vector3Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(UnityEngine.Object owner, string field, float value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

    }
}
