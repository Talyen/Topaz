using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Topaz;
using Topaz.Generation;
public static class EnvironmentCapture {
 public static string Main(string directory){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before capture.");
  var dir=Path.GetFullPath(directory);Directory.CreateDirectory(dir);
  var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var previous=QualitySettings.renderPipeline;var pipeline=UnityEngine.Object.Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);pipeline.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;QualitySettings.renderPipeline=pipeline;
  var fixture=new VisualLabFixture();var owned=new List<UnityEngine.Object>();var rt=new RenderTexture(640,640,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
  try{
   var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Topaz/Core/Editor/VisualLab/TopazVisualLab.shader");
   fixture.BuildLabFixture(AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset"),shader);
   var groups=fixture.Root.transform.Cast<Transform>().GroupBy(t=>t.name).Select(g=>g.First()).ToArray();
   foreach(var child in fixture.Root.transform.Cast<Transform>())child.gameObject.SetActive(false);
   var cam=new GameObject("Calibrated environment camera").AddComponent<Camera>();cam.cullingMask=1<<29;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.5f,.5f,.5f);cam.orthographic=true;cam.nearClipPlane=.01f;cam.farClipPlane=500;cam.targetTexture=rt;
   var cameraData=cam.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=false;cameraData.volumeLayerMask=1<<29;
   var volume=new GameObject("Environment reference volume").AddComponent<Volume>();volume.gameObject.layer=29;volume.isGlobal=true;volume.priority=200;var profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;owned.Add(profile);
#if SURFACE_CACHE
   profile.Add<SurfaceCacheGIVolumeOverride>(true).enabled.Override(false);
#endif
   var light=new GameObject("Neutral reference light").AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<29;light.intensity=.85f;light.transform.rotation=Quaternion.Euler(40,-30,0);RenderSettings.sun=light;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.6f,.6f);RenderSettings.fog=false;
   var manifest=new List<object>();int index=0;
   foreach(var root in groups){
    root.gameObject.SetActive(true);foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
    var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponent<MeshFilter>()?.sharedMesh!=null).ToArray();if(renderers.Length==0){root.gameObject.SetActive(false);continue;}
    foreach(var r in renderers)if(familyName(root.name)=="terrain"){r.sharedMaterial.SetFloat("_VertexColor",1);r.sharedMaterial.SetColor("_BaseColor",new Color(.43f,.57f,.34f));}else if(familyName(root.name)=="water")r.sharedMaterial.SetColor("_BaseColor",new Color(.2f,.5f,.54f));
    var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
    string family=root.name=="Terraces"?"terrain":root.name=="River"?"water":root.name.Contains("Pine")?"trees":root.name.StartsWith("rock ")?"stone":"structures";
    string id=index.ToString("D2")+"_"+family;string folder=Path.Combine(dir,id);Directory.CreateDirectory(folder);var parts=new List<object>();
    foreach(var r in renderers){var mesh=r.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices.Select(r.transform.TransformPoint).ToArray();var normals=mesh.normals.Select(r.transform.TransformDirection).ToArray();var materials=r.sharedMaterials;var colors=mesh.colors;
     for(int sm=0;sm<mesh.subMeshCount;sm++){
      var m=materials[Math.Min(sm,materials.Length-1)];string texName=new[]{"_Albedo_Map","_Base_Texture","_BaseMap","_MainTex"}.FirstOrDefault(k=>m!=null&&m.HasProperty(k)&&m.GetTexture(k)!=null);Texture texture=texName==null?null:m.GetTexture(texName);
      parts.Add(new{name=RelativePath(root,r.transform)+"#"+sm,meshName=mesh.name,submesh=sm,vertices=vertices.SelectMany(v=>new[]{v.x,v.y,v.z}).ToArray(),normals=normals.SelectMany(v=>new[]{v.x,v.y,v.z}).ToArray(),uv=mesh.uv.SelectMany(v=>new[]{v.x,v.y}).ToArray(),colors=colors.SelectMany(c=>new[]{c.r,c.g,c.b}).ToArray(),triangles=mesh.GetTriangles(sm),texture=texture==null?null:AssetDatabase.GetAssetPath(texture)});
     }
     if(family!="terrain"&&family!="water"){var mats=materials.Select(m=>{var n=new Material(Shader.Find("Universal Render Pipeline/Lit"));owned.Add(n);var key=new[]{"_Albedo_Map","_Base_Texture","_BaseMap","_MainTex"}.FirstOrDefault(k=>m!=null&&m.HasProperty(k)&&m.GetTexture(k)!=null);if(key!=null)n.SetTexture("_BaseMap",m.GetTexture(key));else n.SetColor("_BaseColor",family=="stone"?new Color(.65f,.67f,.62f):new Color(.45f,.3f,.18f));n.SetFloat("_Smoothness",0);return n;}).ToArray();r.sharedMaterials=mats;}
    }
    cam.orthographicSize=Mathf.Max(bounds.size.y,Mathf.Max(bounds.size.x,bounds.size.z))*.64f;
    Vector3[] directions=family=="terrain"||family=="water"?new[]{Vector3.up}:new[]{new Vector3(1,.6f,1),new Vector3(-1,.65f,-1),new Vector3(-1,.2f,1)};var views=new List<object>();
    for(int k=0;k<directions.Length;k++){cam.transform.position=bounds.center+directions[k].normalized*Mathf.Max(15,bounds.size.magnitude*2);cam.transform.LookAt(bounds.center,family=="terrain"||family=="water"?Vector3.forward:Vector3.up);views.Add(new{name="view-"+k,position=Vec(cam.transform.position),right=Vec(cam.transform.right),up=Vec(cam.transform.up),forward=Vec(cam.transform.forward),center=Vec(bounds.center),scale=cam.orthographicSize});for(int f=0;f<2;f++)RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=rt});Save(rt,Path.Combine(folder,"view-"+k+".png"));}
    File.WriteAllText(Path.Combine(folder,"source.json"),JsonConvert.SerializeObject(new{parts,views,rootName=root.name,family,viewSize=640}));manifest.Add(new{id,rootName=root.name,family,views=directions.Length,parts=parts.Count});index++;root.gameObject.SetActive(false);
   }
   File.WriteAllText(Path.Combine(dir,"assets.json"),JsonConvert.SerializeObject(manifest,Formatting.Indented));return index+" unique environment groups exported with calibrated views";
  }finally{fixture.Dispose();EditorSceneManager.CloseScene(scene,true);foreach(var o in owned)UnityEngine.Object.DestroyImmediate(o);QualitySettings.renderPipeline=previous;UnityEngine.Object.DestroyImmediate(pipeline);rt.Release();UnityEngine.Object.DestroyImmediate(rt);SceneManager.SetActiveScene(original);}
 }
 static string familyName(string name)=>name=="Terraces"?"terrain":name=="River"?"water":"other";
 static float[] Vec(Vector3 v)=>new[]{v.x,v.y,v.z};
 static string RelativePath(Transform root,Transform t){if(t==root)return ".";var names=new List<string>();while(t!=root){names.Add(t.name);t=t.parent;}names.Reverse();return string.Join("/",names);}
 static void Save(RenderTexture target,string path){var old=RenderTexture.active;RenderTexture.active=target;var t=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);try{t.ReadPixels(new Rect(0,0,t.width,t.height),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=old;}}
}
