using System;
using System.Collections.Generic;
using Topaz.Generation;
using UnityEngine;

namespace Topaz
{
    /// <summary>Editor-only comparison fixture. Never participates in game state or production generation.</summary>
    public sealed class VisualLabFixture : IDisposable
    {
        sealed class LabMaterial
        {public Renderer Renderer;public Material[] Original;public string Role;public bool Projected;}
        readonly List<Material> labOwned=new List<Material>();
        readonly List<LabMaterial> labRenderers=new List<LabMaterial>();
        readonly List<Material> labSources=new List<Material>();
        GameObject labRoot;
        Mesh labGround;
        public void RegisterLabRenderer(Renderer renderer,string role)
        {labRenderers.Add(new LabMaterial{Renderer=renderer,Original=renderer.sharedMaterials,Role=role});}
        public void RebindLabRenderer(Renderer renderer)
        {
            var binding=labRenderers.Find(item=>item.Renderer==renderer);
            if(binding==null)throw new InvalidOperationException("Unregistered review renderer");
            binding.Original=renderer.sharedMaterials;binding.Projected=true;
        }
        public void ApplyLabMaterials(VisualLabLook look,Shader shader)
        {
            foreach(var material in labOwned)if(material!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(material);else UnityEngine.Object.DestroyImmediate(material);}labOwned.Clear();
            foreach(var binding in labRenderers)
            {
                if(binding.Renderer==null)continue;var materials=new Material[binding.Original.Length];
                for(int i=0;i<materials.Length;i++)
                {
                    var source=binding.Original[i];var material=look.Shading==0 && binding.Role=="prop" && source!=null?new Material(source):new Material(shader);
                    Texture texture=source!=null && source.HasProperty("_Albedo_Map")?source.GetTexture("_Albedo_Map"):source!=null && source.HasProperty("_Base_Texture")?source.GetTexture("_Base_Texture"):source!=null && source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source!=null && source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):Texture2D.whiteTexture;
                    material.SetTexture("_BaseMap",texture!=null?texture:Texture2D.whiteTexture);
                    Color tint=binding.Projected?look.Tint:binding.Role=="ground"?look.Ground:binding.Role=="rock"?look.Rock:binding.Role=="water"?look.Water:look.Tint;
                    material.SetColor("_BaseColor",tint);material.SetColor("_ShadowColor",look.Shadow);material.SetFloat("_Wrap",look.Wrap);material.SetFloat("_Bands",look.Bands);material.SetFloat("_Mode",look.Shading);material.SetFloat("_VertexColor",binding.Role=="ground"&&!binding.Projected?1:0);material.SetFloat("_Gloss",binding.Role=="water"?.5f:.035f);
                    material.enableInstancing=true;materials[i]=material;labOwned.Add(material);
                }
                binding.Renderer.sharedMaterials=materials;
            }
        }
        static float LabHeight(float x,float z)
        {
            float shelf=2.8f*Mathf.SmoothStep(0,1,(z-1)/8)+2.5f*Mathf.SmoothStep(0,1,(x-10)/8);
            float river=Mathf.Abs(x+9+Mathf.Sin(z*.13f)*1.2f);
            return Mathf.Lerp(-.7f,shelf,Mathf.SmoothStep(0,1,(river-2.5f)/3));
        }
        public void BuildLabFixture(WoodlandPreset preset,Shader shader)
        {
            labRoot=new GameObject("URP Synty visual lab");
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
            for(int z=-24;z<27;z+=2)for(int x=-27;x<27;x+=2)
            {
                Vector3 Point(float px,float pz)=>new Vector3(px,LabHeight(px,pz),pz);
                var a=Point(x,z);var b=Point(x,z+2);var c=Point(x+2,z);var d=Point(x+2,z+2);
                void Triangle(Vector3 p,Vector3 q,Vector3 r)
                {
                    int first=vertices.Count;var normal=Vector3.Cross(q-p,r-p).normalized;
                    foreach(var v in new[]{p,q,r})
                    {
                        vertices.Add(v);normals.Add(normal);
                        float path=DistanceToLabPath(v.x,v.z);
                        Color color=path<2?new Color(1.7f,1.32f,.9f):normal.y<.8f?new Color(.92f,.92f,.95f):new Color(.8f,.98f,.8f);
                        colors.Add(color);triangles.Add(first++);
                    }
                }
                Triangle(a,b,c);Triangle(c,b,d);
            }
            labGround=new Mesh{name="Faceted terraced lab ground"};labGround.SetVertices(vertices);labGround.SetNormals(normals);labGround.SetColors(colors);labGround.SetTriangles(triangles,0);labGround.RecalculateBounds();
            var ground=new GameObject("Terraces",typeof(MeshFilter),typeof(MeshRenderer));ground.transform.SetParent(labRoot.transform,false);ground.GetComponent<MeshFilter>().sharedMesh=labGround;var groundSource=new Material(shader);labSources.Add(groundSource);ground.GetComponent<MeshRenderer>().sharedMaterial=groundSource;RegisterLabRenderer(ground.GetComponent<Renderer>(),"ground");
            void Prop(GameObject source,Vector3 p,float size,string role,float yaw=0)
            {
                if(source==null)return;p.y=LabHeight(p.x,p.z);
                var go=UnityEngine.Object.Instantiate(source,p,Quaternion.Euler(0,yaw,0),labRoot.transform);go.name=role+" "+source.name;
                var all=go.GetComponentsInChildren<Renderer>();if(all.Length>0){var bounds=all[0].bounds;foreach(var r in all)bounds.Encapsulate(r.bounds);float span=Mathf.Max(bounds.size.x,bounds.size.z);if(span>size)go.transform.localScale*=size/span;}
                var binding=preset.Binding(source);if(binding!=null)go.transform.position-=go.transform.TransformVector(binding.groundAnchor);
                foreach(var r in all)RegisterLabRenderer(r,role);
            }
            var cabin=preset.destinations.Length>(int)DestinationKind.Cabin?preset.destinations[(int)DestinationKind.Cabin]:preset.discoveries[1];
            Prop(cabin,new Vector3(6,0,12),12,"prop",200);
            var arch=preset.destinations.Length>(int)DestinationKind.RuinArch?preset.destinations[(int)DestinationKind.RuinArch]:preset.discoveries[0];
            Prop(arch,new Vector3(4,0,2),6,"rock",0);
            if(preset.bridge!=null)Prop(preset.bridge,new Vector3(-9,0,-4),13,"prop",90);
            var random=new System.Random(71);
            foreach(var cluster in new[]{new Vector2(-18,-10),new Vector2(-17,17),new Vector2(16,-2),new Vector2(18,21)})
                for(int i=0;i<5;i++){float a=i*2.4f;var p=cluster+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*(2+i*.6f);Prop(preset.trees[i%preset.trees.Length],new Vector3(p.x,0,p.y),6+(float)random.NextDouble()*2,"prop",i*31);}
            for(int i=0;i<7;i++){float x=12+i%3*3,z=7+i/3*5;var rocks=preset.cliffs.Length>0?preset.cliffs:preset.rocks;Prop(rocks[i%rocks.Length],new Vector3(x,0,z),6,"rock",i*47);}
            // A quiet, geometric water ribbon; no dense grass carpet obscures the ground experiment.
            var water=GameObject.CreatePrimitive(PrimitiveType.Plane);water.name="River";water.transform.SetParent(labRoot.transform,false);water.transform.position=new Vector3(-9,.05f,1);water.transform.localScale=new Vector3(.5f,1,5.2f);var waterSource=new Material(shader);labSources.Add(waterSource);water.GetComponent<Renderer>().sharedMaterial=waterSource;RegisterLabRenderer(water.GetComponent<Renderer>(),"water");
        }
        static float DistanceToLabPath(float x,float z)
        {float a=Mathf.Abs(z+4-Mathf.Sin(x*.16f)*1.8f),b=Mathf.Abs(x-4-Mathf.Sin(z*.15f)*1.5f);return Mathf.Min(a, b);}
        public GameObject Root => labRoot;
        public void Dispose()
        {
            foreach(var material in labOwned)if(material!=null)UnityEngine.Object.DestroyImmediate(material);
            foreach(var material in labSources)if(material!=null)UnityEngine.Object.DestroyImmediate(material);
            if(labGround!=null)UnityEngine.Object.DestroyImmediate(labGround);
            if(labRoot!=null)UnityEngine.Object.DestroyImmediate(labRoot);
        }
    }
}
