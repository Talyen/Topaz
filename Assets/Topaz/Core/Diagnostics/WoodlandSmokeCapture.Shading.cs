using System;
using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Topaz
{
    public sealed partial class WoodlandSmokeCapture
    {
        // Review-only alternatives. Normal play never changes a character's authored mesh/material here.
        sealed class ShadingStudy : IDisposable
        {
            readonly SkinnedMeshRenderer body;
            readonly Mesh original,soft;
            readonly Material[] materials;
            readonly Dictionary<TerrainLayer,float> terrainNormals=new Dictionary<TerrainLayer,float>();
            public ShadingStudy(GameObject player)
            {
                body=(SkinnedMeshRenderer)player.GetComponentInChildren<Topaz.Characters.CharacterVisual>().BodyRenderer;
                original=body.sharedMesh;materials=body.sharedMaterials;soft=Object.Instantiate(original);
                var vertices=soft.vertices;var normals=soft.normals;var smoothed=new Vector3[normals.Length];
                var groups=new Dictionary<Vector3Int,List<int>>();
                for(int i=0;i<vertices.Length;i++)
                {
                    var p=vertices[i]*10000;var key=new Vector3Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y),Mathf.RoundToInt(p.z));
                    if(!groups.TryGetValue(key,out var group)){group=new List<int>();groups.Add(key,group);}group.Add(i);
                }
                float cosine=Mathf.Cos(35*Mathf.Deg2Rad);
                foreach(var group in groups.Values)foreach(int index in group)
                {
                    Vector3 normal=Vector3.zero;
                    foreach(int other in group)if(Vector3.Dot(normals[index],normals[other])>=cosine)normal+=normals[other];
                    smoothed[index]=normal.normalized;
                }
                soft.normals=smoothed;soft.RecalculateTangents();
            }
            public void Apply(int variant)
            {
                body.sharedMesh=variant==0?original:soft;
                if(variant==0)body.sharedMaterials=materials;
                else
                {
                    var material=Resources.Load<Material>(variant==1?"TopazCharacterSoft":"TopazCharacterRich");
                    if(material==null)throw new InvalidOperationException("Material study assets have not been authored.");
                    var replacements=new Material[materials.Length];for(int i=0;i<replacements.Length;i++)replacements[i]=material;body.sharedMaterials=replacements;
                }
                foreach(var terrain in Terrain.activeTerrains)foreach(var layer in terrain.terrainData.terrainLayers)
                {
                    if(!terrainNormals.ContainsKey(layer))terrainNormals.Add(layer,layer.normalScale);
                    layer.normalScale=variant==0?terrainNormals[layer]:variant==1?.35f:.95f;
                }
            }
            public void Dispose()
            {
                if(body!=null){body.sharedMesh=original;body.sharedMaterials=materials;}
                foreach(var pair in terrainNormals)if(pair.Key!=null)pair.Key.normalScale=pair.Value;
                Object.Destroy(soft);
            }
        }
    }
}
