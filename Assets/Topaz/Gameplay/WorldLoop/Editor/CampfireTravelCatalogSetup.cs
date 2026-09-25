using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Keep unloaded Campfire destinations derived from their authored scenes.</summary>
    public sealed class CampfireTravelCatalogSetup : IPreprocessBuildWithReport
    {
        const string CatalogFolder = "Assets/Topaz/Gameplay/WorldLoop/Resources";
        const string CatalogPath = CatalogFolder + "/CampfireTravelCatalog.asset";
        const string IconPath = "Assets/Topaz/UI/Resources/CampfireIcon.png";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Validate();

        [MenuItem("Topaz/Refresh Campfire Travel Catalog")]
        public static void Refresh()
        {
            List<Topaz.LoopStudy.CampfireTravelCatalog.Destination> entries = Collect();
            if (!AssetDatabase.IsValidFolder(CatalogFolder))
                AssetDatabase.CreateFolder("Assets/Topaz/Gameplay/WorldLoop", "Resources");
            var catalog = AssetDatabase.LoadAssetAtPath<Topaz.LoopStudy.CampfireTravelCatalog>(
                CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<Topaz.LoopStudy.CampfireTravelCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var serialized = new SerializedObject(catalog);
            SerializedProperty values = serialized.FindProperty("destinations");
            values.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty value = values.GetArrayElementAtIndex(i);
                value.FindPropertyRelative("stableId").stringValue = entries[i].stableId;
                value.FindPropertyRelative("regionId").stringValue = entries[i].regionId;
                value.FindPropertyRelative("sceneName").stringValue = entries[i].sceneName;
                value.FindPropertyRelative("label").stringValue = entries[i].label;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            TextureImporter importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Campfire UI icon is missing.");
            if (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
        }

        public static void Validate()
        {
            var expected = Collect();
            var catalog = AssetDatabase.LoadAssetAtPath<Topaz.LoopStudy.CampfireTravelCatalog>(
                CatalogPath);
            if (catalog == null || catalog.Destinations.Count != expected.Count)
                throw new BuildFailedException("Campfire travel catalog is missing or stale.");
            for (int i = 0; i < expected.Count; i++)
            {
                var actual = catalog.Destinations[i];
                var source = expected[i];
                if (actual.stableId != source.stableId ||
                    actual.regionId != source.regionId ||
                    actual.sceneName != source.sceneName || actual.label != source.label)
                    throw new BuildFailedException("Campfire travel catalog is stale. " +
                        "Run Topaz/Refresh Campfire Travel Catalog.");
            }
            if (AssetDatabase.LoadAssetAtPath<Sprite>(IconPath) == null)
                throw new BuildFailedException("Campfire travel icon is not imported as a Sprite.");
        }

        static List<Topaz.LoopStudy.CampfireTravelCatalog.Destination> Collect()
        {
            var entries = new List<Topaz.LoopStudy.CampfireTravelCatalog.Destination>();
            var ids = new HashSet<string>();
            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes.Where(x => x.enabled))
            {
                Scene scene = SceneManager.GetSceneByPath(buildScene.path);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(buildScene.path,
                    OpenSceneMode.Additive);
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Topaz.LoopStudy.Campfire fire in
                        root.GetComponentsInChildren<Topaz.LoopStudy.Campfire>(true))
                    {
                        if (string.IsNullOrWhiteSpace(fire.StableId) ||
                            string.IsNullOrWhiteSpace(fire.RegionId) ||
                            string.IsNullOrWhiteSpace(fire.TravelLabel) ||
                            !fire.HasArrival)
                            throw new InvalidOperationException($"Campfire {fire.name} in " +
                                $"{buildScene.path} needs an ID, region, short label, and arrival.");
                        if (!ids.Add(fire.StableId) || !labels.Add(fire.TravelLabel))
                            throw new InvalidOperationException($"Duplicate Campfire ID or label: " +
                                $"{fire.StableId} / {fire.TravelLabel}");
                        entries.Add(new Topaz.LoopStudy.CampfireTravelCatalog.Destination
                        {
                            stableId = fire.StableId,
                            regionId = fire.RegionId,
                            sceneName = Path.GetFileNameWithoutExtension(buildScene.path),
                            label = fire.TravelLabel
                        });
                    }
                }
                finally
                {
                    if (opened) EditorSceneManager.CloseScene(scene, true);
                }
            }
            if (entries.Count == 0) throw new InvalidOperationException(
                "The build scenes have no Campfire destinations.");
            return entries;
        }
    }
}
