using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Topaz.Generation.Editor
{
    public static partial class WorldShaderSetup
    {
        [MenuItem("Topaz/Rendering/Configure Spatial Shelter")]
        public static void ConfigureShelter()
        {
            // Keep native passes, Meta, SCGI, CBUFFER, GRD/DOTS and motion variants. Regenerate on URP upgrades.
            string package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.render-pipelines.universal").resolvedPath;
            WriteShell(package, "Lit.shader", "ShelteredLit.shader", "Universal Render Pipeline/Lit", "Topaz/Sheltered Lit", "LitForwardPass.hlsl");
            WriteShell(package, "Terrain/TerrainLit.shader", "ShelteredTerrain.shader", "Universal Render Pipeline/Terrain/Lit", "Topaz/Sheltered Terrain", "Terrain/TerrainLitPasses.hlsl");
            AssetDatabase.Refresh();
            var lit = AssetDatabase.LoadAssetAtPath<Shader>(Root + "ShelteredLit.shader");
            var terrain = AssetDatabase.LoadAssetAtPath<Shader>(Root + "ShelteredTerrain.shader");
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Topaz" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material.shader.name == "Universal Render Pipeline/Lit") { material.shader = lit; EditorUtility.SetDirty(material); }
                else if (material.shader.name == "Universal Render Pipeline/Terrain/Lit") { material.shader = terrain; EditorUtility.SetDirty(material); }
            }
            foreach (string name in new[] { "Foliage", "Wet Surface", "Emission Dissolve" }) AddShelterOcclusion(name);
            AlignFoundationSurface();
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Shelter shaders and owned materials configured.");
        }
        public static void AlignFoundationSurface()
        {
            const string path=SyntySampleSetup.WorldRoot+"Stone Foundation.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var collider=root.GetComponent<BoxCollider>();
                var renderers=root.GetComponentsInChildren<Renderer>();
                float top=renderers.Max(r=>r.bounds.max.y);
                float walking=collider.transform.TransformPoint(collider.center+Vector3.up*collider.size.y*.5f).y;
                Debug.Log($"[Topaz] Foundation rendered top={top:F6}, walking surface={walking:F6}");
                float offset=walking-top;
                if(Mathf.Abs(offset)<.001f)return;
                foreach(Transform child in root.transform)child.position+=Vector3.up*offset;
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static void WriteShell(string package, string source, string destination, string oldName, string newName, string forward)
        {
            string text = File.ReadAllText(Path.Combine(package, "Shaders", source));
            text = text.Replace("Shader \"" + oldName + "\"", "Shader \"" + newName + "\"");
            string include = "#include \"Packages/com.unity.render-pipelines.universal/Shaders/" + forward + "\"";
            // Only the Forward+ pass is extended; other native passes are preserved verbatim.
            int index = text.IndexOf(include, StringComparison.Ordinal);
            if (index < 0) throw new InvalidOperationException("Installed URP shader forward include changed: " + source);
            if (!text.Contains("_SCREEN_SPACE_IRRADIANCE"))
                throw new InvalidOperationException("Installed URP shader is missing SCGI variants: " + source);
            text = text.Insert(index, "#include \"TopazShelterLighting.hlsl\"\n            ");
            File.WriteAllText(Root + destination, "// Derived from installed URP 17.6 shader shell; Unity Companion License. Native includes remain package-owned.\n" + text);
            if(File.Exists(Root+"CutawayLitForwardPass.hlsl"))Topaz.Player.Editor.IsometricCameraSetup.PatchShader(Root+destination);
        }
        static void AddShelterOcclusion(string name)
        {
            string path = Root + name + ".shadergraph";
            var graph = New("GraphData");
            T("Serialization.MultiJson").GetMethod("Deserialize").MakeGenericMethod(T("GraphData")).Invoke(null,new[]{graph,File.ReadAllText(path),null,(object)true});
            var nodes = ((IEnumerable)graph.GetType().GetMethod("GetNodes").MakeGenericMethod(T("CustomFunctionNode")).Invoke(graph,null)).Cast<object>();
            if (nodes.Any(n => (string)n.GetType().GetProperty("functionName").GetValue(n) == "TopazShelter")) return;
            var node = New("CustomFunctionNode"); Set(node,"functionName","TopazShelter"); Set(node,"sourceType",Enum.Parse(T("Drawing.HlslSourceType"),"File"));
            Set(node,"functionSource",AssetDatabase.AssetPathToGUID(Root+"TopazShelter.hlsl"));
            Slot(node,0,"World",false,true); Slot(node,1,"Exposure",true,false); Slot(node,2,"Fill",true,false); Call(graph,"AddNode",node,true);
            var position = New("PositionNode"); Call(graph,"AddNode",position,true); Call(graph,"Connect",Ref(position,0),Ref(node,0));
            var blocks = ((IEnumerable)graph.GetType().GetMethod("GetNodes").MakeGenericMethod(T("BlockNode")).Invoke(graph,null)).Cast<object>();
            var block = blocks.First(b => (string)b.GetType().GetProperty("name").GetValue(b) == "SurfaceDescription.Occlusion");
            Call(graph,"Connect",Ref(node,2),Ref(block,0));
            File.WriteAllText(path,(string)T("Serialization.MultiJson").GetMethod("Serialize").Invoke(null,new[]{graph})); AssetDatabase.ImportAsset(path);
        }
    }
}
