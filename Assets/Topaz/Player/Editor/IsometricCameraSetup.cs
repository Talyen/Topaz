using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Topaz.Generation.Editor;

namespace Topaz.Player.Editor
{
    /// <summary>Targeted authoring for the fixed camera; preserves existing scene/world tuning.</summary>
    public static class IsometricCameraSetup
    {
        const string Root="Assets/Topaz/Presentation/Rendering/Environment/";
        [MenuItem("Topaz/Camera/Configure Isometric Camera")]
        public static void Configure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before authoring camera assets.");
            var camera=UnityEngine.Object.FindAnyObjectByType<PlayerCamera>();
            if(camera==null)throw new InvalidOperationException("Open Bootstrap before configuring the gameplay camera.");
            var so=new SerializedObject(camera);
            Set(so,"minimumZoom",24);Set(so,"maximumZoom",38);Set(so,"worldYaw",45);Set(so,"downwardPitch",50);Set(so,"fieldOfView",35);
            so.ApplyModifiedPropertiesWithoutUndo();
            var player=so.FindProperty("target").objectReferenceValue as PlayerController;
            if(camera.TryGetComponent<AudioListener>(out var oldListener))UnityEngine.Object.DestroyImmediate(oldListener);
            var hearing=player.transform.Find("Player Hearing");
            if(hearing==null){hearing=new GameObject("Player Hearing").transform;hearing.SetParent(player.transform,false);hearing.localPosition=Vector3.up*1.5f;}
            if(!hearing.TryGetComponent<AudioListener>(out _))hearing.gameObject.AddComponent<AudioListener>();
            // Explicit layer is visual-only; player/enemy root collision layers are untouched.
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers=tags.FindProperty("layers");
            if(!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(30).stringValue) && layers.GetArrayElementAtIndex(30).stringValue!="Character Visibility")
                throw new InvalidOperationException("Layer 30 is already occupied.");
            layers.GetArrayElementAtIndex(30).stringValue="Character Visibility";tags.ApplyModifiedPropertiesWithoutUndo();
            GenerateCutawayPasses();
            PatchShader(Root+"ShelteredLit.shader");PatchShader(Root+"ShelteredTerrain.shader");
            AssetDatabase.Refresh();
            WorldShaderSetup.NativeFoliageLods(false);WorldShaderSetup.NativeFoliageLods(true);
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"Woodland Renderer.asset");
            var feature=renderer.rendererFeatures.OfType<RenderObjects>().FirstOrDefault(f=>f.name=="Hidden Characters");
            if(feature==null)
            {
                feature=ScriptableObject.CreateInstance<RenderObjects>();feature.name="Hidden Characters";
                AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);
            }
            string materialPath=Root+"Hidden Character.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Topaz/Hidden Character"));AssetDatabase.CreateAsset(material,materialPath);}
            feature.settings.filterSettings.LayerMask=1<<30;
            feature.settings.filterSettings.PassNames=new[]{"UniversalForward","UniversalForwardOnly","SRPDefaultUnlit"};
            feature.settings.Event=RenderPassEvent.AfterRenderingOpaques;
            feature.settings.overrideMaterial=material;
            feature.settings.overrideDepthState=true;feature.settings.depthCompareFunction=CompareFunction.Greater;feature.settings.enableWrite=false;
            feature.Create();EditorUtility.SetDirty(feature);EditorUtility.SetDirty(renderer);
            // Keep native renderer feature subasset IDs in sync with the authored list.
            var rs=new SerializedObject(renderer);var map=rs.FindProperty("m_RendererFeatureMap");
            map.arraySize=renderer.rendererFeatures.Count;
            for(int i=0;i<renderer.rendererFeatures.Count;i++)
            {AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i],out string _,out long id);map.GetArrayElementAtIndex(i).longValue=id;}
            rs.ApplyModifiedPropertiesWithoutUndo();
            const string capPath=Root+"Cutaway Ground.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(capPath)==null)
            {
                var ground=new Material(Shader.Find("Topaz/Sheltered Lit"));ground.SetColor("_BaseColor",new Color(.24f,.3f,.22f));
                ground.SetFloat("_Smoothness",0);AssetDatabase.CreateAsset(ground,capPath);
            }
            var cutaway=camera.GetComponent<SceneryCutaway>() ?? camera.gameObject.AddComponent<SceneryCutaway>();
            var cs=new SerializedObject(cutaway);cs.FindProperty("groundMaterial").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>(capPath);cs.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);EditorSceneManager.SaveScene(camera.gameObject.scene);
            Debug.Log("[Topaz] Fixed isometric camera, cutaway passes and hidden-character feature authored.");
        }
        static void Set(SerializedObject so,string name,float value)=>so.FindProperty(name).floatValue=value;
        public static void PatchShader(string path)
        {
            string text=File.ReadAllText(path);
            // Only gameplay passes are extended: shadow, Meta and GI keep the original geometry.
            text=Regex.Replace(text,@"Name ""(ForwardLit|DepthOnly|DepthNormals|MotionVectors|XRMotionVectors)""[\s\S]*?ENDHLSL",m=>
            {
                string block=m.Value;
                foreach(var include in new[]{"Shaders/LitForwardPass.hlsl","Shaders/DepthOnlyPass.hlsl","Shaders/LitDepthNormalsPass.hlsl",
                    "Shaders/Terrain/TerrainLitPasses.hlsl","Shaders/Terrain/TerrainLitDepthNormalsPass.hlsl","ShaderLibrary/ObjectMotionVectors.hlsl"})
                {
                    string source="Packages/com.unity.render-pipelines.universal/"+include;
                    string local=Root+"Cutaway"+Path.GetFileName(include);
                    block=block.Replace(source,local);
                }
                return block;
            });
            if(File.ReadAllText(path)!=text)File.WriteAllText(path,text);
        }
        static void GenerateCutawayPasses()
        {
            string package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.render-pipelines.universal").resolvedPath;
            foreach(var include in new[]{"Shaders/LitForwardPass.hlsl","Shaders/DepthOnlyPass.hlsl","Shaders/LitDepthNormalsPass.hlsl",
                "Shaders/Terrain/TerrainLitPasses.hlsl","Shaders/Terrain/TerrainLitDepthNormalsPass.hlsl","ShaderLibrary/ObjectMotionVectors.hlsl"})
            {
                string source=File.ReadAllText(Path.Combine(package,include));
                string directory=Path.GetDirectoryName(include).Replace("\\","/");
                source=Regex.Replace(source,@"#include ""(?!Packages/|Assets/)([^""]+)""",m=>"#include \"Packages/com.unity.render-pipelines.universal/"+directory+"/"+m.Groups[1].Value+"\"");
                string name=Path.GetFileName(include);
                string fragment=name=="LitForwardPass.hlsl"?"LitPassFragment":name=="LitDepthNormalsPass.hlsl"?"DepthNormalsFragment":
                    name=="TerrainLitDepthNormalsPass.hlsl"?"DepthNormalOnlyFragment":name=="ObjectMotionVectors.hlsl"?"frag":
                    name=="TerrainLitPasses.hlsl"?"SplatmapFragment":"DepthOnlyFragment";
                string variable=name.StartsWith("Terrain")?"IN.clipPos":"input.positionCS";
                source=Inject(source,fragment,variable);
                if(name=="TerrainLitPasses.hlsl")source=Inject(source,"DepthOnlyFragment","IN.clipPos");
                source="// Derived from installed URP 17.6; Unity Companion License. Regenerated by IsometricCameraSetup.\n"+
                    "#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"\n"+
                    "#include \"Assets/Topaz/Presentation/Rendering/Environment/TopazCutaway.hlsl\"\n"+source;
                string destination=Root+"Cutaway"+name;
                if(!File.Exists(destination)||File.ReadAllText(destination)!=source)File.WriteAllText(destination,source);
            }
        }
        static string Inject(string source,string fragment,string position)
        {
            var match=Regex.Match(source,@"\b"+fragment+@"\s*\(");
            if(!match.Success)throw new InvalidOperationException("Installed URP fragment changed: "+fragment);
            int body=source.IndexOf('{',match.Index);
            int insertion=body+1;
            int stereo=source.IndexOf("UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX",body);
            if(stereo>=body && stereo<body+250)insertion=source.IndexOf(';',stereo)+1;
            else {int instance=source.IndexOf("UNITY_SETUP_INSTANCE_ID",body);if(instance>=body && instance<body+250)insertion=source.IndexOf(';',instance)+1;}
            return source.Insert(insertion,"\n    TopazClipScreen("+position+");\n");
        }
    }
}
