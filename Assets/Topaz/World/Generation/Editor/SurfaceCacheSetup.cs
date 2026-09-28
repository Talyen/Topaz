using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz.Generation.Editor
{
    public sealed class SurfaceCacheSetup : IPreprocessBuildWithReport
    {
        const string RendererPath = "Assets/Topaz/Presentation/Rendering/Environment/Woodland Renderer.asset";
        public int callbackOrder => 0;

        public static void ConfigureVolumeDefaults()
        {
#if SURFACE_CACHE
            // The default stack is used during title/loading/teardown before the world controller
            // supplies its runtime profile. Its exclusion mask must be identical in those states.
            foreach(string path in new[]{"Assets/Topaz/Presentation/Rendering/Settings/DefaultVolumeProfile.asset",
                "Assets/Topaz/Presentation/Rendering/Environment/URP Environment.asset"})
            {
                var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if(profile==null)throw new InvalidOperationException("Missing Topaz volume profile: "+path);
                if(!profile.TryGet<SurfaceCacheGIVolumeOverride>(out var gi))
                {gi=profile.Add<SurfaceCacheGIVolumeOverride>(true);AssetDatabase.AddObjectToAsset(gi,profile);}
                Topaz.Rendering.SurfaceCacheLighting.Apply(profile,true);
                EditorUtility.SetDirty(gi);EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
#endif
        }

        internal static void ConfigureModelReadability()
        {
#if SURFACE_CACHE
            foreach(string path in ContributingModels())
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if(importer.isReadable) continue;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
#endif
        }

        [MenuItem("Topaz/Rendering/Configure Surface Cache GI")]
        public static void Configure()
        {
#if SURFACE_CACHE
            ConfigureModelReadability();
            ConfigureVolumeDefaults();
            PlayerSettings.SetStaticBatchingForPlatform(BuildTarget.StandaloneOSX, false);
            PlayerSettings.SetStaticBatchingForPlatform(BuildTarget.StandaloneWindows64, false);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null) throw new InvalidOperationException("Missing Topaz renderer.");
            var feature = renderer.rendererFeatures.OfType<SurfaceCacheGIRendererFeature>().SingleOrDefault();
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<SurfaceCacheGIRendererFeature>();
                feature.name = "Surface Cache Global Illumination";
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
            }
            feature.SetActive(true);
            renderer.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(renderer);
            var globals = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>();
            EditorGraphicsSettings.PopulateRenderPipelineGraphicsSettings(globals);
            EditorUtility.SetDirty(globals);
            // Only the shipping Bootstrap scene owns the procedural-world presentation.
            const string scenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.isLoaded;
            if(opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                foreach(var root in scene.GetRootGameObjects())
                    foreach(var look in root.GetComponentsInChildren<Topaz.Rendering.VisualLookController>(true))
                    {
                        using var serialized = new SerializedObject(look);
                        serialized.FindProperty("surfaceCacheFeature").objectReferenceValue = feature;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                EditorSceneManager.SaveScene(scene);
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz/SCGI] Native renderer feature configured. Desktop static batching disabled.");
#else
            throw new InvalidOperationException("Enable SURFACE_CACHE in Standalone scripting defines, wait for compilation, then configure SCGI.");
#endif
        }

        public void OnPreprocessBuild(BuildReport report) => Validate(report.summary.platform);

        static IEnumerable<string> ContributingModels()
        {
            var roots = AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/Topaz"})
                .Select(AssetDatabase.GUIDToAssetPath)
                .Concat(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path)).ToArray();
            return AssetDatabase.GetDependencies(roots, true)
                .Where(path=>AssetImporter.GetAtPath(path) is ModelImporter).Distinct();
        }

        public static void Validate(BuildTarget target)
        {
#if SURFACE_CACHE
            if (!PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone).Split(';').Contains("SURFACE_CACHE"))
                throw new BuildFailedException("[Topaz/SCGI] Missing desktop SURFACE_CACHE define.");
            if (PlayerSettings.GetStaticBatchingForPlatform(target))
                throw new BuildFailedException("[Topaz/SCGI] Static batching must be disabled.");
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null || renderer.renderingMode != RenderingMode.ForwardPlus ||
                renderer.rendererFeatures.OfType<SurfaceCacheGIRendererFeature>().Count(f => f.isActive) != 1)
                throw new BuildFailedException("[Topaz/SCGI] Expected one active SCGI feature on the Forward+ renderer.");
            // These native resource sets are internal types. Inspect their serialized references
            // rather than accessing package internals or disabling the native resource stripper.
            var required = new HashSet<string> { "UnityEngine.Rendering.SurfaceCacheRenderPipelineResourceSet",
                "UnityEngine.Rendering.Universal.SurfaceCacheRenderPipelineResourceSet",
                "UnityEngine.Rendering.UnifiedRayTracing.RayTracingRenderPipelineResources",
                "UnityEngine.PathTracing.Core.WorldRenderPipelineResources" };
            var globals = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>();
            if (globals == null) throw new BuildFailedException("[Topaz/SCGI] Missing URP global settings.");
            using var serialized = new SerializedObject(globals);
            var property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ManagedReference || property.managedReferenceValue == null) continue;
                string type = property.managedReferenceValue.GetType().FullName;
                if (!required.Remove(type)) continue;
                var child = property.Copy();
                // Next(true) includes hidden fields; bound traversal by depth, not the
                // visible-only end iterator (which can cross into unrelated settings).
                while (child.Next(true) && child.depth > property.depth)
                    if (child.propertyType == SerializedPropertyType.ObjectReference && child.objectReferenceValue == null)
                        throw new BuildFailedException("[Topaz/SCGI] Missing resource: " + type + "." + child.name);
            }
            if (required.Count != 0) throw new BuildFailedException("[Topaz/SCGI] Missing resource sets: " + string.Join(", ", required));
            foreach(string path in ContributingModels())
                if(!((ModelImporter)AssetImporter.GetAtPath(path)).isReadable)
                    throw new BuildFailedException("[Topaz/SCGI] Compute ray tracing requires readable mesh imports. Run Configure Surface Cache GI: " + path);
#else
            throw new BuildFailedException("[Topaz/SCGI] Build requires SURFACE_CACHE; no shipping fallback exists.");
#endif
        }
    }
}
