using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Topaz;

/// <summary>Private review texture/mesh ownership. Does not modify imported meshes or prefabs.</summary>
public sealed class EnvironmentTextureSet : IDisposable {
 readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
 readonly Dictionary<string,(JObject data,float[] uv,Texture2D texture)> sources=new Dictionary<string,(JObject,float[],Texture2D)>();
 public int RendererCount {get;private set;}
 public int GroupCount {get;private set;}
 public EnvironmentTextureSet(string directory){
  var manifest=JArray.Parse(File.ReadAllText(Path.Combine(directory,"assets.json")));
  foreach(var a in manifest){string folder=Path.Combine(directory,(string)a["id"]);var texture=new Texture2D(2,2,TextureFormat.RGB24,true,false);texture.LoadImage(File.ReadAllBytes(Path.Combine(folder,"baked-albedo.png")));texture.Apply(true,false);owned.Add(texture);sources.Add((string)a["rootName"],(JObject.Parse(File.ReadAllText(Path.Combine(folder,"source.json"))),JObject.Parse(File.ReadAllText(Path.Combine(folder,"atlas-uv.json")))["cornerUv"].Values<float>().ToArray(),texture));}
 }
 public void Apply(VisualLabFixture fixture){
  foreach(var root in fixture.Root.transform.Cast<Transform>()){
   if(!sources.TryGetValue(root.name,out var source))continue;GroupCount++;
   var parts=(JArray)source.data["parts"];var offsets=new Dictionary<string,int>();int cursor=0;foreach(var part in parts){offsets.Add((string)part["name"],cursor);cursor+=part["triangles"].Count();}
   if(cursor*2!=source.uv.Length)throw new InvalidOperationException("Atlas/source corner count mismatch");
   foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy)){
    var filter=renderer.GetComponent<MeshFilter>();if(filter==null)continue;var original=filter.sharedMesh;var vertices=original.vertices;var normals=original.normals;
    var positions=new List<Vector3>();var ns=new List<Vector3>();var uvs=new List<Vector2>();var ranges=new List<int[]>();
    for(int submesh=0;submesh<original.subMeshCount;submesh++){
     string key=RelativePath(root,renderer.transform)+"#"+submesh;var part=parts.First(p=>(string)p["name"]==key);var indices=original.GetTriangles(submesh);
     if(!indices.SequenceEqual(part["triangles"].Values<int>()))throw new InvalidOperationException("Mesh topology changed for "+key);
     int offset=offsets[key];int first=positions.Count;
     for(int j=0;j<indices.Length;j++){positions.Add(vertices[indices[j]]);ns.Add(normals[indices[j]]);uvs.Add(new Vector2(source.uv[(offset+j)*2],source.uv[(offset+j)*2+1]));}
     ranges.Add(Enumerable.Range(first,indices.Length).ToArray());
    }
    var mesh=new Mesh{name=original.name+" projection review atlas"};if(positions.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(positions);mesh.SetNormals(ns);mesh.SetUVs(0,uvs);mesh.subMeshCount=ranges.Count;for(int sm=0;sm<ranges.Count;sm++)mesh.SetTriangles(ranges[sm],sm);mesh.RecalculateBounds();mesh.RecalculateTangents();owned.Add(mesh);filter.sharedMesh=mesh;
    var materials=new Material[ranges.Count];for(int sm=0;sm<materials.Length;sm++){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetTexture("_BaseMap",source.texture);m.SetFloat("_Smoothness",root.name=="River"?.4f:.1f);materials[sm]=m;owned.Add(m);}renderer.sharedMaterials=materials;fixture.RebindLabRenderer(renderer);RendererCount++;
   }
  }
 }
 public void ApplyCharacter(VisualLabFixture fixture,GameObject actor,string directory){
  var map=JObject.Parse(File.ReadAllText(Path.Combine(directory,"atlas-uv.json")))["cornerUv"].Values<float>().ToArray();
  var definition=JObject.Parse(File.ReadAllText(Path.Combine(directory,"source.json")));
  var texture=new Texture2D(2,2,TextureFormat.RGB24,true,false);texture.LoadImage(File.ReadAllBytes(Path.Combine(directory,"baked-albedo.png")));texture.Apply(true,false);owned.Add(texture);
  int offset=0;var parts=(JArray)definition["parts"];
  var renderers=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
  for(int part=0;part<renderers.Length;part++){
   var original=renderers[part];var source=original.sharedMesh;var indices=source.triangles;
   if(original.name!=(string)parts[part]["name"]||!indices.SequenceEqual(parts[part]["triangles"].Values<int>()))throw new InvalidOperationException("Character bake source changed");
   if(source.GetBonesPerVertex().Any(n=>n>4))throw new InvalidOperationException("Unsupported skin influence count");
   var v=source.vertices;var n=source.normals;var w=source.boneWeights;var uv=new Vector2[indices.Length];
   for(int i=0;i<uv.Length;i++){uv[i]=new Vector2(map[offset*2],map[offset*2+1]);offset++;}
   var mesh=new Mesh{name=source.name+" review projection atlas",vertices=indices.Select(i=>v[i]).ToArray(),normals=indices.Select(i=>n[i]).ToArray(),boneWeights=indices.Select(i=>w[i]).ToArray(),bindposes=source.bindposes,uv=uv,triangles=Enumerable.Range(0,indices.Length).ToArray()};mesh.RecalculateBounds();mesh.RecalculateTangents();owned.Add(mesh);
   var r=new GameObject(original.name+" projected").AddComponent<SkinnedMeshRenderer>();r.transform.SetParent(original.transform.parent,false);r.transform.localPosition=original.transform.localPosition;r.transform.localRotation=original.transform.localRotation;r.transform.localScale=original.transform.localScale;r.sharedMesh=mesh;r.bones=original.bones;r.rootBone=original.rootBone;r.localBounds=original.localBounds;r.updateWhenOffscreen=true;original.enabled=false;
   var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetTexture("_BaseMap",texture);material.SetFloat("_Smoothness",.1f);owned.Add(material);r.sharedMaterial=material;fixture.RegisterLabRenderer(r,"actor");fixture.RebindLabRenderer(r);
  }
  if(offset*2!=map.Length)throw new InvalidOperationException("Character atlas corner mismatch");
 }
 static string RelativePath(Transform root,Transform t){if(t==root)return ".";var names=new List<string>();while(t!=root){names.Add(t.name);t=t.parent;}names.Reverse();return string.Join("/",names);}
 public void Dispose(){foreach(var value in owned)if(value!=null)UnityEngine.Object.DestroyImmediate(value);owned.Clear();}
}
