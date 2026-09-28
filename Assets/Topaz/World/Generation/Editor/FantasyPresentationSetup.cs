using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz.Generation.Editor
{
    /// <summary>Repairs the native post stack and authors reusable, mipmapped ground color.</summary>
    public static class FantasyPresentationSetup
    {
        const string Root="Assets/Topaz/Presentation/Rendering/Environment/";
        [MenuItem("Topaz/Rendering/Configure Luminous Fantasy")]
        public static void Apply() => Configure(true);
        public static void Configure(bool updateScenes)
        {
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"Woodland Renderer.asset");
            renderer.depthPrimingMode=DepthPrimingMode.Auto;
            renderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            if(renderer.postProcessData==null)throw new InvalidOperationException("Installed URP post-processing resources are missing.");
            var ao=renderer.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().Single();
            var serialized=new SerializedObject(ao);var settings=serialized.FindProperty("m_Settings");
            settings.FindPropertyRelative("Intensity").floatValue=.75f;
            settings.FindPropertyRelative("Radius").floatValue=.18f;
            settings.FindPropertyRelative("Samples").intValue=0;
            settings.FindPropertyRelative("Downsample").boolValue=true;
            settings.FindPropertyRelative("DirectLightingStrength").floatValue=0f;
            settings.FindPropertyRelative("AOMethod").intValue=1; // Stable spatial sampling, no animated blue-noise grain.
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"Desktop URP.asset");
            pipeline.shadowDistance=70;pipeline.shadowCascadeCount=2;pipeline.colorGradingMode=ColorGradingMode.HighDynamicRange;pipeline.colorGradingLutSize=32;
            var pipe=new SerializedObject(pipeline);pipe.FindProperty("m_MainLightShadowmapResolution").intValue=2048;pipe.FindProperty("m_SoftShadowQuality").intValue=2;pipe.ApplyModifiedPropertiesWithoutUndo();
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"URP Environment.asset");
            T Effect<T>() where T:VolumeComponent
            {
                if(profile.TryGet(out T value))return value;
                value=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(value,profile);return value;
            }
            // Authored active overrides make the installed URP build stripper retain runtime-selectable effects.
            Effect<Tonemapping>().mode.Override(TonemappingMode.ACES);
            Effect<ColorLookup>().texture.Override(CreateFantasyLut());Effect<ColorLookup>().contribution.Override(.7f);
            Effect<Bloom>().intensity.Override(.18f);Effect<Bloom>().threshold.Override(1.25f);Effect<Bloom>().scatter.Override(.78f);
            Effect<DepthOfField>().active=true;Effect<DepthOfField>().mode.Override(DepthOfFieldMode.Bokeh);
            Effect<DepthOfField>().focusDistance.Override(7);Effect<DepthOfField>().focalLength.Override(55);Effect<DepthOfField>().aperture.Override(4);
            Effect<MotionBlur>().intensity.Override(.15f);Effect<ChromaticAberration>().intensity.Override(.005f);
            Effect<SplitToning>().shadows.Override(new Color(.43f,.48f,.56f));Effect<SplitToning>().highlights.Override(new Color(.57f,.53f,.45f));
            foreach(var effect in profile.components)EditorUtility.SetDirty(effect);EditorUtility.SetDirty(profile);
            var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>(Root+"Woodland.asset");
            preset.settings=WoodlandSettings.LargeWorld();EditorUtility.SetDirty(preset);
            Ground(preset.grass,"Fantasy Meadow",new Color(.20f,.31f,.20f),new Color(.32f,.43f,.27f));
            Ground(preset.path,"Fantasy Earth",new Color(.29f,.265f,.22f),new Color(.42f,.38f,.30f));
            Ground(preset.forestLayer,"Fantasy Forest",new Color(.17f,.25f,.18f),new Color(.28f,.34f,.23f));
            Ground(preset.rockLayer,"Fantasy Stone",new Color(.30f,.335f,.31f),new Color(.46f,.46f,.40f));
            if(updateScenes) foreach(var path in new[]{"Bootstrap","Woodland","RenderingLab"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/"+path+".unity");
                foreach(var root in scene.GetRootGameObjects())foreach(var camera in root.GetComponentsInChildren<Camera>(true))
                {
                    if(camera.TryGetComponent<UniversalAdditionalCameraData>(out var data)){data.dithering=false;data.renderPostProcessing=true;}
                }
                EditorSceneManager.SaveScene(scene);
            }
            EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(ao);EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();
            if(updateScenes) EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
        }
        public static Texture2D CreateFantasyLut()
        {
            const int size=32;
            const string path="Assets/Topaz/Presentation/Effects/Resources/TopazFantasyLut.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null){texture=new Texture2D(size*size,size,TextureFormat.RGBAHalf,false,true){name="Topaz Fantasy LUT"};AssetDatabase.CreateAsset(texture,path);}
            var pixels=new Color[size*size*size];
            for(int b=0;b<size;b++)for(int g=0;g<size;g++)for(int r=0;r<size;r++)
            {
                var c=new Vector3(r,g,b)/(size-1f);float l=Vector3.Dot(c,new Vector3(.2126f,.7152f,.0722f));
                float shadow=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.04f,.55f,l)))*Mathf.SmoothStep(0,1,l/.08f);
                float highlight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,l))*(1-l);
                // Gently cool the toe and warm highlights while preserving black, white and luminance order.
                c+=new Vector3(-.012f,.009f,.023f)*shadow+new Vector3(.08f,.025f,-.045f)*highlight;
                // Separate yellow-green vegetation from warm earth; protect neutral colors and skin reds.
                float green=Mathf.Clamp01((c.y-Mathf.Max(c.x,c.z))*.9f);
                c.x-=green*.025f;c.z+=green*.035f;
                pixels[g*size*size+b*size+r]=new Color(Mathf.Clamp01(c.x),Mathf.Clamp01(c.y),Mathf.Clamp01(c.z),1);
            }
            texture.SetPixels(pixels);texture.Apply(false,false);texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
            EditorUtility.SetDirty(texture);return texture;
        }
        static void Ground(TerrainLayer layer,string name,Color dark,Color light)
        {
            string path=Root+name+".asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null){texture=new Texture2D(256,256,TextureFormat.RGB24,true){name=name};AssetDatabase.CreateAsset(texture,path);}
            var pixels=new Color[256*256];
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                float u=x/256f,v=y/256f;
                // Tileable painted variation. A 16 m repeat divides the 128 m tile exactly, preserving edge phase.
                float Noise(float frequency)
                {
                    float a=Mathf.Lerp(Mathf.PerlinNoise(20+u*frequency,20+v*frequency),Mathf.PerlinNoise(20+(u-1)*frequency,20+v*frequency),u);
                    float b=Mathf.Lerp(Mathf.PerlinNoise(20+u*frequency,20+(v-1)*frequency),Mathf.PerlinNoise(20+(u-1)*frequency,20+(v-1)*frequency),u);
                    return Mathf.Lerp(a,b,v);
                }
                float n=Mathf.Clamp01((Noise(4)*.50f+Noise(13)*.28f+Noise(55)*.22f-.25f)*2);
                pixels[y*256+x]=Color.Lerp(dark,light,n);
            }
            texture.SetPixels(pixels);texture.Apply(true,false);texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Trilinear;texture.anisoLevel=8;
            layer.diffuseTexture=texture;layer.tileSize=new Vector2(16,16);layer.smoothness=.05f;
            EditorUtility.SetDirty(texture);EditorUtility.SetDirty(layer);
        }
    }
}
