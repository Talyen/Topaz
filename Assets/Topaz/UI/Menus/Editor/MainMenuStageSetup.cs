using System;
using System.Linq;
using Topaz.AnimationStudy;
using Topaz.Menus;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Composes an authored stage prefab with the current scene's player and camera.</summary>
    public static class MainMenuStageSetup
    {
        internal static MainMenuStage Configure(Scene scene, GameObject player, PlayerAppearance appearance)
        {
            MainMenuStage stage = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MainMenuStage>(true))
                .FirstOrDefault();
            if (stage == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Topaz/UI/Menus/Prefabs/Main Menu Stage.prefab");
                if (prefab == null) throw new InvalidOperationException("The authored menu stage prefab is missing.");
                stage = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<MainMenuStage>();
            }
            Camera camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true))
                .First(c => c.CompareTag("MainCamera"));
            var data = new SerializedObject(stage);
            var session = player.GetComponent<Topaz.LoopStudy.WorldSession>();
            var workbench = session != null ? new SerializedObject(session).FindProperty("workbench").objectReferenceValue : null;
            if (workbench == null) throw new InvalidOperationException("Menu stage requires the scene's home volume anchor.");
            data.FindProperty("volumeTrigger").objectReferenceValue = workbench;
            data.FindProperty("appearance").objectReferenceValue = appearance;
            data.FindProperty("gameplayCamera").objectReferenceValue = camera;
            data.FindProperty("gameplayCameraData").objectReferenceValue = camera.GetComponent<UniversalAdditionalCameraData>();
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(stage);
            return stage;
        }
    }
}
