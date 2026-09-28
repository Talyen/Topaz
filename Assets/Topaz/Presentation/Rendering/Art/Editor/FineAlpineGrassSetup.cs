using System;
using UnityEditor;
using UnityEngine;

namespace Topaz.Rendering.Editor
{
    /// <summary>Owns original grass geometry; the vendor foliage material remains a separate dependency.</summary>
    public static class FineAlpineGrassSetup
    {
        const string Cover = "Assets/Topaz/Presentation/Art/World/GroundCover/";
        const string MeshFolder = "Assets/Topaz/Presentation/Art/World/FineGrass";

        [MenuItem("Topaz/Generation/Configure Fine Alpine Grass")]
        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder(MeshFolder))
                AssetDatabase.CreateFolder("Assets/Topaz/Presentation/Art/World", "FineGrass");
            Configure("Low Grass", 0, new Color(.17f, .33f, .17f));
            Configure("Upright Grass", 1, new Color(.21f, .37f, .18f));
            Configure("Shade Grass", 2, new Color(.13f, .27f, .16f));
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz/Art] Fine Alpine grass meshes and materials updated.");
        }

        static void Configure(string name, int variant, Color color)
        {
            string prefabPath = Cover + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new InvalidOperationException("Missing grass detail: " + prefabPath);

            string meshPath = MeshFolder + "/" + name + ".asset";
            var generated = CreateBladeMesh(name, variant);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                AssetDatabase.CreateAsset(generated, meshPath);
                mesh = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, mesh);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(mesh);
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var filter = root.GetComponent<MeshFilter>();
                var renderer = root.GetComponent<MeshRenderer>();
                if (filter == null || renderer == null || renderer.sharedMaterial == null)
                    throw new InvalidOperationException("Grass detail needs a root MeshFilter, MeshRenderer and material: " + prefabPath);
                filter.sharedMesh = mesh;
                var material = renderer.sharedMaterial;
                // Keep the palette and alpha treatment of the owned PNB material. The
                // new geometry provides the narrow silhouette and lean.
                string texturePath = variant == 1
                    ? "Assets/Synty/PNB_Core/Textures/Grass_01.tga"
                    : "Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/Textures/Alpine_Grass_01.tga";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null) throw new InvalidOperationException("Missing grass palette texture: " + texturePath);
                material.SetTexture("_Leaf_Texture", texture);
                material.SetFloat("_Enable_Leaf_Normal", 0);
                material.SetColor("_Leaf_Base_Color", color);
                material.SetColor("_Leaf_Noise_Color", color * 1.08f);
                material.SetColor("_Leaf_Noise_Large_Color", color * .90f);
                material.SetFloat("_Light_Wind_Strength", .16f);
                material.SetFloat("_Strong_Wind_Strength", .12f);
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static Mesh CreateBladeMesh(string name, int variant)
        {
            const int blades = 12;
            var vertices = new Vector3[blades * 6];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[blades * 12];
            for (int blade = 0; blade < blades; blade++)
            {
                // Uneven roots and a shared prevailing lean avoid the circular brush silhouette.
                float angle = (blade * 137.508f + variant * 23f) * Mathf.Deg2Rad;
                float radius = .13f + .25f * Hash(blade, variant, 1);
                var root = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                float yaw = (-35f + 70f * Hash(blade, variant, 2)) * Mathf.Deg2Rad;
                var tangent = new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw));
                var side = new Vector3(-tangent.z, 0, tangent.x);
                float height = .70f + .30f * Hash(blade, variant, 3);
                float bend = .18f + .24f * Hash(blade, variant, 4);
                float width = .09f + .05f * Hash(blade, variant, 5);
                for (int row = 0; row < 3; row++)
                {
                    float t = row * .5f;
                    var center = root + Vector3.up * (height * t) + tangent * (bend * t * t);
                    float halfWidth = width * (row == 2 ? .045f : row == 1 ? .58f : 1f);
                    for (int edge = 0; edge < 2; edge++)
                    {
                        int index = blade * 6 + row * 2 + edge;
                        vertices[index] = center + side * (edge == 0 ? -halfWidth : halfWidth);
                        normals[index] = (Vector3.up * .9f + tangent * .12f + side * (edge == 0 ? -.12f : .12f)).normalized;
                        // The source atlas is mostly transparent above V=.35 and at its
                        // sides. Keep its color variation without erasing the mesh blade.
                        uv[index] = new Vector2(edge == 0 ? .32f : .40f, Mathf.Lerp(.10f, .35f, t));
                        colors[index] = new Color(.04f + .27f * t, .98f * t, 1f, 1f);
                    }
                }
                int first = blade * 6, tri = blade * 12;
                for (int segment = 0; segment < 2; segment++)
                {
                    int v = first + segment * 2, i = tri + segment * 6;
                    triangles[i] = v; triangles[i + 1] = v + 2; triangles[i + 2] = v + 1;
                    triangles[i + 3] = v + 1; triangles[i + 4] = v + 2; triangles[i + 5] = v + 3;
                }
            }
            var mesh = new Mesh { name = name, vertices = vertices, normals = normals, uv = uv, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static float Hash(int blade, int variant, int channel)
        {
            uint value = (uint)(blade * 374761393 + variant * 668265263 + channel * 1274126177);
            value ^= value >> 13;
            value *= 1274126177;
            return (value & 0x00ffffff) / 16777216f;
        }
    }
}
