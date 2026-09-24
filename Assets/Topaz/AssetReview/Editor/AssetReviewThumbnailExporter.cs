using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Topaz.AssetReview.Editor
{
    /// <summary>Creates disposable, orthographic review images outside Unity's Assets folder.</summary>
    public static class AssetReviewThumbnailExporter
    {
        const string SourceRoot = "Assets/ThirdParty/KayKit";
        const int Width = 360;
        const int Height = 270;

        [MenuItem("Topaz/Asset Review/Generate Forest Previews")]
        public static void GenerateForest() => Generate("Forest");

        [MenuItem("Topaz/Asset Review/Generate All Previews")]
        public static void GenerateAll() => Generate(null);

        [MenuItem("Topaz/Asset Review/Generate Animation Previews")]
        public static void GenerateAnimations() => Generate(null, true);

        static void Generate(string selectedPack, bool animationsOnly = false)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, "AssetReview", "Previews");
            Directory.CreateDirectory(output);
            string source = Path.Combine(Application.dataPath, "ThirdParty", "KayKit");
            string[] files = Directory.GetFiles(source, "*.fbx", SearchOption.AllDirectories)
                .Where(path => selectedPack == null || path.StartsWith(
                    Path.Combine(source, selectedPack) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Where(path => !animationsOnly || path.Replace(Path.DirectorySeparatorChar, '/').Contains("/Animations/"))
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            int generated = 0;
            try
            {
                for (int index = 0; index < files.Length; index++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Topaz asset previews",
                        Path.GetFileName(files[index]), (float)index / files.Length)) break;
                    string assetPath = "Assets" + files[index].Substring(Application.dataPath.Length)
                        .Replace(Path.DirectorySeparatorChar, '/');
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    if (string.IsNullOrEmpty(guid)) continue;
                    try
                    {
                        if (GenerateOne(assetPath, guid, output)) generated++;
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning($"[Topaz] Review preview skipped for {assetPath}: {error.Message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            Debug.Log($"[Topaz] Generated {generated} asset review previews in {output}.");
        }

        static bool GenerateOne(string assetPath, string guid, string output)
        {
            string[] parts = assetPath.Substring(SourceRoot.Length + 1).Split('/');
            string pack = parts[0];
            bool animation = pack == "CharacterAnimations" ||
                (parts.Length > 1 && parts[1] == "Animations");
            string modelPath = animation ? AnimationModel(pack, assetPath) : assetPath;
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) return false;

            var preview = new PreviewRenderUtility();
            GameObject instance = null;
            Material material = null;
            var bakedMeshes = new List<Mesh>();
            try
            {
                instance = UnityEngine.Object.Instantiate(model);
                instance.hideFlags = HideFlags.HideAndDontSave;
                preview.AddSingleGO(instance);
                foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
                    animator.enabled = false;
                Texture2D atlas = Atlas(pack, assetPath);
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                if (atlas != null)
                {
                    if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", atlas);
                    else material.mainTexture = atlas;
                }
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) return false;
                foreach (Renderer renderer in renderers)
                {
                    int count = Mathf.Max(1, renderer.sharedMaterials.Length);
                    renderer.sharedMaterials = Enumerable.Repeat(material, count).ToArray();
                }

                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                Configure(preview, bounds);
                AnimationClip clip = animation ? RepresentativeClip(assetPath) : null;
                int frames = clip == null ? 1 : 8;
                List<BoundCurve> curves = clip == null ? null : BindCurves(instance, clip);
                List<SkinnedMeshRenderer> skins = clip == null ? null :
                    instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList();
                if (skins != null)
                    foreach (SkinnedMeshRenderer skin in skins)
                    {
                        var baked = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                        bakedMeshes.Add(baked);
                        var visual = new GameObject(skin.name + " Review Mesh", typeof(MeshFilter), typeof(MeshRenderer));
                        visual.hideFlags = HideFlags.HideAndDontSave;
                        visual.transform.SetParent(skin.transform, false);
                        visual.GetComponent<MeshFilter>().sharedMesh = baked;
                        visual.GetComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                        skin.enabled = false;
                    }
                for (int frame = 0; frame < frames; frame++)
                {
                    if (clip != null) ApplyCurves(curves, clip.length * frame / frames);
                    if (skins != null)
                        for (int index = 0; index < skins.Count; index++)
                            skins[index].BakeMesh(bakedMeshes[index]);
                    string name = clip == null ? $"{guid}.png" : $"{guid}-{frame}.png";
                    WriteFrame(preview, Path.Combine(output, name));
                }
                if (clip != null) File.Copy(Path.Combine(output, $"{guid}-0.png"),
                    Path.Combine(output, $"{guid}.png"), true);
                if (clip != null) File.WriteAllText(Path.Combine(output, $"{guid}.txt"), clip.name);
                return true;
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                foreach (Mesh mesh in bakedMeshes) UnityEngine.Object.DestroyImmediate(mesh);
                if (material != null) UnityEngine.Object.DestroyImmediate(material);
                preview.Cleanup();
            }
        }

        static void Configure(PreviewRenderUtility preview, Bounds bounds)
        {
            Camera camera = preview.camera;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(0.45f, bounds.extents.magnitude * 1.28f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.18f, 0.15f, 1f);
            camera.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            camera.transform.position = bounds.center - camera.transform.forward *
                Mathf.Max(12f, bounds.extents.magnitude * 4f);
            preview.lights[0].intensity = 1.3f;
            preview.lights[0].transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            preview.lights[1].intensity = 0.5f;
            preview.lights[1].transform.rotation = Quaternion.Euler(35f, 140f, 0f);
            preview.ambientColor = new Color(0.55f, 0.59f, 0.54f);
        }

        static void WriteFrame(PreviewRenderUtility preview, string file)
        {
            preview.BeginStaticPreview(new Rect(0, 0, Width, Height));
            preview.Render(true, false);
            Texture2D image = preview.EndStaticPreview();
            try { File.WriteAllBytes(file, image.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        sealed class BoundCurve
        {
            public Transform target;
            public string property;
            public AnimationCurve curve;
        }

        struct Pose
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        static List<BoundCurve> BindCurves(GameObject instance, AnimationClip clip)
        {
            var result = new List<BoundCurve>();
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(Transform)) continue;
                Transform bone = string.IsNullOrEmpty(binding.path) ? instance.transform :
                    instance.transform.Find(binding.path);
                if (bone == null) continue;
                result.Add(new BoundCurve {
                    target = bone, property = binding.propertyName,
                    curve = AnimationUtility.GetEditorCurve(clip, binding)
                });
            }
            return result;
        }

        static AnimationClip RepresentativeClip(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__") && clip.length >= 0.1f)
                .OrderByDescending(MotionScore).FirstOrDefault();
        }

        static int MotionScore(AnimationClip clip)
        {
            int score = 0;
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(Transform)) continue;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                float start = curve.Evaluate(0f);
                if (Mathf.Abs(start - curve.Evaluate(clip.length * 0.25f)) > 0.03f ||
                    Mathf.Abs(start - curve.Evaluate(clip.length * 0.5f)) > 0.03f ||
                    Mathf.Abs(start - curve.Evaluate(clip.length * 0.75f)) > 0.03f)
                    score++;
            }
            return score;
        }

        static void ApplyCurves(List<BoundCurve> curves, float time)
        {
            var poses = new Dictionary<Transform, Pose>();
            foreach (BoundCurve binding in curves)
            {
                if (!poses.TryGetValue(binding.target, out Pose pose))
                    pose = new Pose { position = binding.target.localPosition,
                        rotation = binding.target.localRotation, scale = binding.target.localScale };
                float value = binding.curve.Evaluate(time);
                switch (binding.property)
                {
                    case "m_LocalPosition.x": pose.position.x = value; break;
                    case "m_LocalPosition.y": pose.position.y = value; break;
                    case "m_LocalPosition.z": pose.position.z = value; break;
                    case "m_LocalRotation.x": pose.rotation.x = value; break;
                    case "m_LocalRotation.y": pose.rotation.y = value; break;
                    case "m_LocalRotation.z": pose.rotation.z = value; break;
                    case "m_LocalRotation.w": pose.rotation.w = value; break;
                    case "m_LocalScale.x": pose.scale.x = value; break;
                    case "m_LocalScale.y": pose.scale.y = value; break;
                    case "m_LocalScale.z": pose.scale.z = value; break;
                }
                poses[binding.target] = pose;
            }
            foreach (var entry in poses)
            {
                entry.Key.localPosition = entry.Value.position;
                entry.Key.localRotation = Quaternion.Normalize(entry.Value.rotation);
                entry.Key.localScale = entry.Value.scale;
            }
        }

        static string AnimationModel(string pack, string source)
        {
            if (pack == "Skeletons") return SourceRoot + "/Skeletons/Characters/Skeleton_Minion.fbx";
            if (pack == "CharacterAnimations") return source.Contains("Rig_Large")
                ? SourceRoot + "/CharacterAnimations/Characters/Mannequin_Large.fbx"
                : SourceRoot + "/CharacterAnimations/Characters/Mannequin_Medium.fbx";
            return SourceRoot + "/Adventurers/Characters/Rogue.fbx";
        }

        static Texture2D Atlas(string pack, string source)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SourceRoot + "/" + pack + "/Textures" });
            string[] paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
            if (paths.Length == 0) return null;
            if (pack == "Adventurers")
            {
                string name = Path.GetFileNameWithoutExtension(source).ToLowerInvariant();
                string selected = paths.FirstOrDefault(path => name.Contains(Path.GetFileName(path).Split('_')[0]));
                if (selected != null) return AssetDatabase.LoadAssetAtPath<Texture2D>(selected);
                string rogue = paths.FirstOrDefault(path => path.Contains("rogue_texture"));
                if (rogue != null) return AssetDatabase.LoadAssetAtPath<Texture2D>(rogue);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(paths[0]);
        }
    }
}
