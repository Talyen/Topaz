using System;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Topaz;
using Topaz.Generation;
public static class ReviewLab {
 static string Dir;
 public static string Main(string directory){return Capture(directory,null);}
 public static string Textured(string directory){return Capture(directory,directory);}
 public static string Capture(string directory,string projectionDirectory){
  Dir=Path.GetFullPath(directory);
  if(EditorApplication.isPlaying)throw new Exception("Stop play mode first.");
  Directory.CreateDirectory(Dir);
  var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var oldPipeline=QualitySettings.renderPipeline;var pipeline=UnityEngine.Object.Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);pipeline.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;QualitySettings.renderPipeline=pipeline;
  var capture=new VisualLabFixture();EnvironmentTextureSet projected=null;
  var target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);target.Create();
  try{

   var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Topaz/Core/Editor/VisualLab/TopazVisualLab.shader");if(shader==null)throw new Exception("Review shader not imported");
   var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
   capture.BuildLabFixture(preset,shader);
   var root=capture.Root;
   if(projectionDirectory!=null){projected=new EnvironmentTextureSet(projectionDirectory);projected.Apply(capture);}
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonVikingRealm/Prefabs/Characters/SM_Chr_Warrior_Male_01.prefab");
   var actor=UnityEngine.Object.Instantiate(prefab,root.transform);actor.transform.position=new Vector3(1,0,-6);
   foreach(var r in actor.GetComponentsInChildren<Renderer>())capture.RegisterLabRenderer(r,"actor");
   if(projected!=null)projected.ApplyCharacter(capture,actor,Path.Combine(Application.dataPath,"../TestResults/artifacts/character-projection"));
   foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
   var cam=new GameObject("Review camera").AddComponent<Camera>();cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.23f,.3f,.35f);cam.fieldOfView=35;cam.nearClipPlane=.1f;cam.farClipPlane=200;
   var rot=Quaternion.Euler(50,45,0);cam.transform.SetPositionAndRotation(new Vector3(0,1.7f,2)-rot*Vector3.forward*35,rot);cam.targetTexture=target;
   var data=cam.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=1<<30;
   var sun=new GameObject("Review sun").AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.cullingMask=1<<30;sun.shadowBias=.03f;sun.shadowNormalBias=.3f;
   var volume=new GameObject("Review grading").AddComponent<Volume>();volume.gameObject.layer=30;volume.isGlobal=true;volume.priority=100;var profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
   #if SURFACE_CACHE
   profile.Add<SurfaceCacheGIVolumeOverride>(true).enabled.Override(false);
#endif
   var texture=new Texture2D(2,2,TextureFormat.RGB24,true,false);if(projectionDirectory==null){texture.LoadImage(File.ReadAllBytes(Path.Combine(Dir,"terrain-material-sheet.png")));texture.Apply(true,false);}
   var color=profile.Add<ColorAdjustments>(true);var tone=profile.Add<Tonemapping>(true);var bloom=profile.Add<Bloom>(true);var dof=profile.Add<DepthOfField>(true);
   RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.fog=false;
   Shader.SetGlobalFloat("_TopazRevealEnabled",0);Shader.SetGlobalVector("_TopazWind",Vector4.zero);
   var recipes=VisualLabLook.All;
   for(int i=0;i<recipes.Length;i++){
    var r=recipes[i];sun.transform.rotation=Quaternion.Euler(r.Angle);sun.color=r.Sun;sun.intensity=r.Light;sun.shadowStrength=.85f;
    RenderSettings.ambientSkyColor=r.Ambient*.7f;RenderSettings.ambientEquatorColor=r.Ambient*.48f;RenderSettings.ambientGroundColor=r.Shadow*.3f;
    color.postExposure.Override(r.Exposure);color.contrast.Override(r.Contrast);color.saturation.Override(r.Saturation);
    tone.mode.Override(i==0||i==9?TonemappingMode.Neutral:TonemappingMode.ACES);bloom.intensity.Override(r.Bloom);bloom.threshold.Override(1.2f);
    dof.active=r.DoF;dof.mode.Override(DepthOfFieldMode.Bokeh);dof.focusDistance.Override(42);dof.aperture.Override(8);dof.focalLength.Override(40);
    capture.ApplyLabMaterials(r,shader);
    bool painted=projectionDirectory==null&&(i==2||i==3||i==7||i==8||i==10||i==11);
    foreach(var renderer in root.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
     if(material.shader==shader){material.SetTexture("_GroundTextures",texture);material.SetFloat("_PaintedGround",painted?(renderer.name=="Terraces"?1:renderer.name.StartsWith("rock ")?2:0):0);}
    data.resetHistory=true;
    for(int f=0;f<3;f++)RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=target});
    Save(target,Path.Combine(Dir,(i+1).ToString("D2")+".png"));
    if(projected!=null){
     cam.transform.position=new Vector3(4,2,7)-rot*Vector3.forward*26;dof.focusDistance.Override(26);data.resetHistory=true;
     for(int f=0;f<3;f++)RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=target});
     Save(target,Path.Combine(Dir,(i+1).ToString("D2")+"-detail.png"));
     cam.transform.position=new Vector3(0,1.7f,2)-rot*Vector3.forward*35;
    }
   }
   File.WriteAllText(Path.Combine(Dir,"looks.json"),"{\"capture\":\"Unity URP Editor explicit render requests; isolated review geometry, GI disabled; no FPS claim\",\"names\":["+string.Join(",",recipes.Select(r=>"\""+r.Name+"\""))+"]}");
   if(projected!=null)File.WriteAllText(Path.Combine(Dir,"application.json"),"{\"environmentGroups\":"+projected.GroupCount+",\"environmentRenderers\":"+projected.RendererCount+",\"sharedAcrossAllLooks\":true,\"characterProjectedParts\":3}");
   UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(profile);
   return "12 captures complete: "+Dir;
  }finally{capture.Dispose();projected?.Dispose();QualitySettings.renderPipeline=oldPipeline;UnityEngine.Object.DestroyImmediate(pipeline);EditorSceneManager.CloseScene(scene,true);target.Release();UnityEngine.Object.DestroyImmediate(target);SceneManager.SetActiveScene(original);}
 }
 public static void Save(RenderTexture rt,string path){var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,t.width,t.height),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=old;}
}
