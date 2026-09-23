using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Idempotent setup for the project's intentionally empty bootstrap scene.</summary>
    public static class FoundationSetup
    {
        const string SampleScene = "Assets/Scenes/SampleScene.unity";
        const string BootstrapScene = "Assets/Scenes/Bootstrap.unity";

        [MenuItem("Topaz/Configure Foundation")]
        public static void Configure()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScene) == null)
            {
                string moveError = AssetDatabase.MoveAsset(SampleScene, BootstrapScene);
                if (!string.IsNullOrEmpty(moveError))
                    throw new InvalidOperationException($"Could not create bootstrap scene: {moveError}");
            }

            // The template's tutorial is not part of the game or its public asset set.
            if (AssetDatabase.IsValidFolder("Assets/TutorialInfo"))
                AssetDatabase.DeleteAsset("Assets/TutorialInfo");
            AssetDatabase.DeleteAsset("Assets/Readme.asset");

            Scene scene = EditorSceneManager.OpenScene(BootstrapScene, OpenSceneMode.Single);
            if (UnityEngine.Object.FindFirstObjectByType<FramePacingAndCapture>() == null)
                new GameObject("Topaz Foundation").AddComponent<FramePacingAndCapture>();

            Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null) throw new InvalidOperationException("The template scene has no camera.");
            if (GameObject.Find("Feel Study") == null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 10f;
                camera.transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
                camera.transform.position = -camera.transform.forward * 20f;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootstrapScene, true) };
            PlayerSettings.companyName = "Talyen";
            PlayerSettings.productName = "Topaz";

            int originalQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.vSyncCount = 1;
            }
            QualitySettings.SetQualityLevel(originalQuality, false);

            CreateProfile("Mac", "macOS");
            CreateProfile("Windows", "Windows");
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Foundation configured.");
        }

        static void CreateProfile(string platformName, string profileName)
        {
            string path = $"Assets/Settings/Build Profiles/{profileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<BuildProfile>(path) != null) return;

            var match = BuildProfile.GetInstalledPlatformModules().FirstOrDefault(info =>
                info.displayName.IndexOf(platformName, StringComparison.OrdinalIgnoreCase) >= 0 &&
                info.displayName.IndexOf("Server", StringComparison.OrdinalIgnoreCase) < 0);
            if (string.IsNullOrEmpty(match.displayName))
                throw new InvalidOperationException($"{platformName} build support is not installed.");

            BuildProfile.CreateBuildProfile(match.platformGuid, profileName);
            Debug.Log($"[Topaz] Created {profileName} build profile from {match.displayName}.");
        }
    }
}
