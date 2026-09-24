using System;
using UnityEditor;
using UnityEngine;

namespace Topaz.Editor
{
    /// <summary>Keep the official URP particle shader referenced in player builds.</summary>
    public static class VisualEffectsComparisonSetup
    {
        const string Folder = "Assets/Topaz/VisualStudy/Resources";
        const string MaterialPath = Folder + "/TopazEffectsParticles.mat";
        const string SourcePath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";

        [MenuItem("Topaz/Create Effects Comparison Material")]
        public static void Configure()
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>(SourcePath);
            if (source == null) throw new InvalidOperationException("URP particle material is missing.");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Topaz/VisualStudy", "Resources");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(source) { name = "TopazEffectsParticles" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Effects comparison particle material ready.");
        }
    }
}
