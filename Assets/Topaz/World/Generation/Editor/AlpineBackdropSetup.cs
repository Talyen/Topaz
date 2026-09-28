using System.Linq;
namespace Topaz.Generation.Editor
{
    public static class AlpineBackdropSetup
    {
        [UnityEditor.MenuItem("Topaz/Generation/Bake Alpine Sky Backdrop")]
        public static void Apply()
        {
string source=@"Shader ""Hidden/Topaz/Backdrop Encode"" { SubShader { Tags { ""RenderPipeline""=""UniversalPipeline"" ""RenderType""=""Opaque"" } Pass { Tags { ""LightMode""=""UniversalForward"" } Cull Off ZWrite On HLSLPROGRAM
#pragma vertex Vert
#pragma fragment Frag
#include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""
struct A {float3 positionOS:POSITION;float3 normalOS:NORMAL;};
struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;};
V Vert(A i){V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(i.normalOS);return o;}
half4 Frag(V i):SV_Target{return half4(saturate(.4+.6*dot(normalize(i.normalWS),normalize(float3(.5,1,-.4)))),saturate((i.positionWS.y-10)/220),1,1);}
ENDHLSL } } }";
var shader=UnityEditor.ShaderUtil.CreateShaderAsset(source);
var material=new UnityEngine.Material(shader);
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root=new UnityEngine.GameObject("Backdrop camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
var camera=root.AddComponent<UnityEngine.Camera>();camera.scene=scene;camera.cameraType=UnityEngine.CameraType.Reflection;camera.enabled=false;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=UnityEngine.Color.clear;camera.farClipPlane=4000;camera.fieldOfView=90;camera.aspect=1;camera.transform.position=UnityEngine.Vector3.up*2;
var data=UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(camera);data.renderPostProcessing=false;data.renderShadows=false;
var renderer=(UnityEngine.Rendering.Universal.UniversalRenderer)data.scriptableRenderer;var oldPriming=renderer.depthPrimingMode;renderer.depthPrimingMode=UnityEngine.Rendering.Universal.DepthPrimingMode.Disabled;
var preset=UnityEditor.AssetDatabase.LoadAssetAtPath<Topaz.Generation.WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
var target=new UnityEngine.RenderTexture(1024,1024,24,UnityEngine.RenderTextureFormat.ARGBHalf){dimension=UnityEngine.Rendering.TextureDimension.Cube};target.Create();
var face=new UnityEngine.RenderTexture(1024,1024,0,UnityEngine.RenderTextureFormat.ARGBHalf);face.Create();
var texture=new UnityEngine.Texture2D(1024,1024,UnityEngine.TextureFormat.RGBAHalf,false,true);var previous=UnityEngine.RenderTexture.active;
var cube=new UnityEngine.Cubemap(1024,UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain);
 cube.name="TopazAlpineBackdrop";cube.filterMode=UnityEngine.FilterMode.Trilinear;
 bool worked=false;
try {
 for(int i=0;i<preset.backgroundMountains.Length;i++){
  float a=i*UnityEngine.Mathf.PI*.5f;var mountain=UnityEngine.Object.Instantiate(preset.backgroundMountains[i],new UnityEngine.Vector3(UnityEngine.Mathf.Sin(a)*1100,10,UnityEngine.Mathf.Cos(a)*1100),UnityEngine.Quaternion.Euler(0,i*90,0));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(mountain,scene);
  foreach(var r in mountain.GetComponentsInChildren<UnityEngine.Renderer>(true)){r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();r.renderingLayerMask=2;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
 }
 worked=camera.RenderToCubemap(target,63);
 if(!worked)throw new System.InvalidOperationException("Native backdrop cubemap capture failed.");
 for(int i=0;i<6;i++){
  UnityEngine.Graphics.CopyTexture(target,i,0,face,0,0);UnityEngine.RenderTexture.active=face;
  texture.ReadPixels(new UnityEngine.Rect(0,0,1024,1024),0,0);texture.Apply();cube.SetPixels(texture.GetPixels(),(UnityEngine.CubemapFace)i);
 }
 cube.Apply(true,false);
 const string path="Assets/Topaz/Presentation/Effects/Resources/TopazAlpineBackdrop.asset";
 var existing=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Cubemap>(path);
 if(existing==null)UnityEditor.AssetDatabase.CreateAsset(cube,path);
 else{UnityEditor.EditorUtility.CopySerialized(cube,existing);UnityEngine.Object.DestroyImmediate(cube);cube=existing;}
 UnityEditor.EditorUtility.SetDirty(cube);UnityEditor.AssetDatabase.SaveAssets();

} finally {UnityEngine.RenderTexture.active=previous;renderer.depthPrimingMode=oldPriming;target.Release();face.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(face);UnityEngine.Object.DestroyImmediate(texture);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(shader);}

        }
    }
}
