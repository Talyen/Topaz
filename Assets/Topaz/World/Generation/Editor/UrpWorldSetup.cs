using System;
using System.Collections.Generic;
using System.Linq;
using Topaz.Gameplay;
using Topaz.Menus;
using Topaz.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace Topaz.Generation.Editor
{
    public static class UrpWorldSetup
    {
        const string Root="Assets/Topaz/Presentation/Rendering/Environment/";
        [MenuItem("Topaz/Rendering/Configure Modern URP")]
        public static void Apply()
        {
            if(AssetDatabase.IsValidFolder("Assets/Topaz/Presentation/HDRP"))
            {
                string error=AssetDatabase.MoveAsset("Assets/Topaz/Presentation/HDRP","Assets/Topaz/Presentation/Rendering/Environment");if(!string.IsNullOrEmpty(error))throw new Exception(error);
            }
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"Woodland Renderer.asset");
            if(renderer==null){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,Root+"Woodland Renderer.asset");}
            renderer.renderingMode=RenderingMode.ForwardPlus;
            var ao=renderer.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().FirstOrDefault();
            if(ao==null){ao=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();ao.name="Woodland Ambient Occlusion";AssetDatabase.AddObjectToAsset(ao,renderer);renderer.rendererFeatures.Add(ao);}
            var decal=renderer.rendererFeatures.OfType<DecalRendererFeature>().FirstOrDefault();
            if(decal==null){decal=ScriptableObject.CreateInstance<DecalRendererFeature>();decal.name="World Decals";AssetDatabase.AddObjectToAsset(decal,renderer);renderer.rendererFeatures.Add(decal);}
            var asset=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"Desktop URP.asset");
            if(asset==null){asset=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(asset,Root+"Desktop URP.asset");}
            asset.supportsHDR=true;asset.msaaSampleCount=1;asset.renderScale=1;asset.upscalingFilter=UpscalingFilterSelection.STP;
            asset.gpuResidentDrawerMode=GPUResidentDrawerMode.InstancedDrawing;asset.gpuResidentDrawerEnableOcclusionCullingInCameras=false;
            asset.shadowDistance=70;asset.shadowCascadeCount=4;
            asset.supportsCameraDepthTexture=true;asset.supportsCameraOpaqueTexture=true;
            var serialized=new SerializedObject(asset);serialized.FindProperty("m_ReflectionProbeBlending").boolValue=true;serialized.FindProperty("m_ReflectionProbeBoxProjection").boolValue=true;serialized.FindProperty("m_SoftShadowsSupported").boolValue=true;serialized.FindProperty("m_LightProbeSystem").intValue=1;serialized.ApplyModifiedPropertiesWithoutUndo();
            GraphicsSettings.defaultRenderPipeline=asset;
            for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i);QualitySettings.renderPipeline=asset;QualitySettings.vSyncCount=1;}
            QualitySettings.SetQualityLevel(0);PlayerSettings.colorSpace=ColorSpace.Linear;
            // Stock renderer resources and Render Graph; no legacy orthographic shader overrides.
            var sky=AssetDatabase.LoadAssetAtPath<Material>(Root+"Procedural Sky.mat");
            if(sky==null){sky=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(sky,Root+"Procedural Sky.mat");}
            sky.SetFloat("_AtmosphereThickness",1.1f);sky.SetFloat("_SunSize",.035f);
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"URP Environment.asset");
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"URP Environment.asset");}
            T Add<T>() where T:VolumeComponent{if(profile.TryGet(out T c))return c;c=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(c,profile);return c;}
            Add<Tonemapping>().mode.Override(TonemappingMode.ACES);Add<Bloom>().intensity.Override(.2f);Add<Bloom>().threshold.Override(1.1f);
            Add<ColorAdjustments>().contrast.Override(8);Add<WhiteBalance>().temperature.Override(3);Add<Vignette>().intensity.Override(.1f);Add<DepthOfField>().active=false;
            EditorUtility.SetDirty(asset);EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
            ConvertMaterials();
            foreach(var name in new[]{"Bootstrap","Woodland","RenderingLab"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/"+name+".unity");
                foreach(var root in scene.GetRootGameObjects())foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if(behaviour==null)continue;
                    string ns=behaviour.GetType().Namespace??"";
                    if(ns=="UnityEngine.Rendering.HighDefinition" || behaviour.GetType().Name=="HDDynamicResolution")Object.DestroyImmediate(behaviour);
                }
                foreach(var volume in All<Volume>(scene)){volume.enabled=false;volume.sharedProfile=null;}
                var env=All<Volume>(scene).FirstOrDefault(v=>v.gameObject.name=="HDRP Environment"||v.gameObject.name=="URP Environment"||v.gameObject.name=="HDRP Reference Volume");
                if(env==null)env=new GameObject("URP Environment").AddComponent<Volume>();env.name="URP Environment";env.isGlobal=true;env.sharedProfile=profile;env.enabled=true;
                foreach(var cam in All<Camera>(scene))
                {
                    var data=cam.GetComponent<UniversalAdditionalCameraData>();if(data==null)data=cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.TemporalAntiAliasing;data.dithering=true;data.volumeLayerMask=~0;
                    cam.allowHDR=true;cam.allowMSAA=false;cam.clearFlags=CameraClearFlags.Skybox;
                }
                foreach(var light in All<Light>(scene))
                {
                    light.intensity=light.type==LightType.Directional?(light.name=="Moon"?0:1.6f):2;
                    var data=light.GetComponent<UniversalAdditionalLightData>();if(data==null)light.gameObject.AddComponent<UniversalAdditionalLightData>();
                }
                RenderSettings.skybox=sky;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.004f;RenderSettings.fogColor=new Color(.5f,.59f,.65f);
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.45f,.55f,.7f);RenderSettings.ambientEquatorColor=new Color(.3f,.34f,.35f);RenderSettings.ambientGroundColor=new Color(.15f,.17f,.12f);
                if(name=="Bootstrap")
                {
                    var look=All<VisualLookController>(scene).Single();Set(look,"cameraData",Camera.main.GetComponent<UniversalAdditionalCameraData>());Set(look,"painterlyVolume",env);Set(look,"ambientOcclusionFeature",ao);
                    if(Camera.main.GetComponent<UrpRenderScaleController>()==null)Camera.main.gameObject.AddComponent<UrpRenderScaleController>();
                    var stage=All<MainMenuStage>(scene).First();Set(stage,"menuCameraData",Get<Camera>(stage,"menuCamera").GetComponent<UniversalAdditionalCameraData>());Set(stage,"gameplayCameraData",Camera.main.GetComponent<UniversalAdditionalCameraData>());
                    var lantern=All<Topaz.Player.PlayerLantern>(scene).First();var so=new SerializedObject(lantern);so.FindProperty("lightIntensity").floatValue=3;so.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach(var go in scene.GetRootGameObjects())if(go.name=="Water Reference")Object.DestroyImmediate(go);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            foreach(string path in new[]{"Desktop HDRP.asset","Woodland Environment.asset"})AssetDatabase.DeleteAsset(Root+path);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
        }
        static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static T Get<T>(Object o,string f)where T:Object=>new SerializedObject(o).FindProperty(f).objectReferenceValue as T;
        static void Set(Object o,string f,Object value){var s=new SerializedObject(o);s.FindProperty(f).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static void ConvertMaterials()
        {
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Topaz"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null||mat.shader==null||!mat.shader.name.StartsWith("HDRP/"))continue;
                Color color=mat.HasProperty("_BaseColor")?mat.GetColor("_BaseColor"):Color.white;
                string baseName=mat.HasProperty("_BaseColorMap")?"_BaseColorMap":"_BaseMap";
                var texture=mat.HasProperty(baseName)?mat.GetTexture(baseName):null;
                var scale=mat.HasProperty(baseName)?mat.GetTextureScale(baseName):Vector2.one;var offset=mat.HasProperty(baseName)?mat.GetTextureOffset(baseName):Vector2.zero;
                string shader=mat.shader.name.Contains("Terrain")?"Universal Render Pipeline/Terrain/Lit":mat.shader.name.Contains("Decal")?"Shader Graphs/Decal":"Universal Render Pipeline/Lit";
                var target=Shader.Find(shader);if(target==null)target=Shader.Find("Universal Render Pipeline/Lit");mat.shader=target;
                if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color);
                if(mat.HasProperty("_BaseMap")){mat.SetTexture("_BaseMap",texture);mat.SetTextureScale("_BaseMap",scale);mat.SetTextureOffset("_BaseMap",offset);}
                mat.enableInstancing=true;EditorUtility.SetDirty(mat);
            }
        }
    }
}
