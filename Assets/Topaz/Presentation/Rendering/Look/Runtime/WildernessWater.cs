using System.Collections.Generic;
using Topaz.Generation;
using UnityEngine;
namespace Topaz.Rendering
{
    /// <summary>One small mesh per global water record; no collider, refraction copy or persistent state.</summary>
    public sealed class WildernessWater : MonoBehaviour
    {
        readonly List<Mesh> meshes=new List<Mesh>();
        public void Initialize(WildernessPlan plan)
        {
            var material=Resources.Load<Material>("TopazShallowWater");
            if(material==null)throw new System.InvalidOperationException("Shallow-water material is missing.");
            BuildRivers(plan,material);
            foreach(var body in plan.Waters)
            {
                const int segments=64,rings=10;
                var vertices=new Vector3[1+segments*rings];var uv=new Vector2[vertices.Length];var indices=new List<int>();
                vertices[0]=new Vector3(body.X,body.Surface+.015f,body.Z);uv[0]=new Vector2(plan.WaterDepth(body.X,body.Z),0);
                for(int ring=1;ring<=rings;ring++)for(int n=0;n<segments;n++)
                {
                    float angle=n*Mathf.PI*2/segments,radius=body.Radius*ring/rings;
                    int index=1+(ring-1)*segments+n;
                    float x=body.X+Mathf.Cos(angle)*radius,z=body.Z+Mathf.Sin(angle)*radius;
                    vertices[index]=new Vector3(x,body.Surface+.015f,z);uv[index]=new Vector2(plan.WaterDepth(x,z),0);
                    int next=1+(ring-1)*segments+(n+1)%segments;
                    if(ring==1){indices.Add(0);indices.Add(next);indices.Add(index);}
                    else{int prior=index-segments,priorNext=next-segments;indices.Add(prior);indices.Add(next);indices.Add(index);indices.Add(prior);indices.Add(priorNext);indices.Add(next);}
                }
                var mesh=new Mesh{name=body.Id,vertices=vertices,uv=uv,triangles=indices.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
                var go=new GameObject(body.Id,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                // Transparent analytic water has no Meta pass. Keep a visible rendering layer.
                renderer.renderingLayerMask=SurfaceCacheLighting.VisualOnlyRenderingLayer;
            }
        }
        void BuildRivers(WildernessPlan plan,Material material)
        {
            foreach(var river in plan.Rivers)
            {
                const int lanes=7;
                var centers=new List<Vector3>();
                for(int i=1;i<river.Points.Count;i++)
                {
                    var a=river.Points[i-1];var b=river.Points[i];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/3));
                    for(int n=0;n<steps;n++)centers.Add(Vector3.Lerp(a,b,n/(float)steps));
                }
                centers.Add(river.Points[river.Points.Count-1]);
                var vertices=new Vector3[centers.Count*lanes];var uv=new Vector2[vertices.Length];var indices=new List<int>();
                for(int i=0;i<centers.Count;i++)
                {
                    var p=centers[i];var direction=centers[Mathf.Min(i+1,centers.Count-1)]-centers[Mathf.Max(0,i-1)];
                    var side=new Vector3(-direction.z,0,direction.x).normalized;
                    for(int lane=0;lane<lanes;lane++)
                    {
                        var point=p+side*((lane/(float)(lanes-1)*2-1)*river.HalfWidth);point.y+=.02f;
                        vertices[i*lanes+lane]=point;
                        float depth=lane==0||lane==lanes-1?0:Mathf.Max(0,p.y-plan.Height(point.x,point.z));
                        uv[i*lanes+lane]=new Vector2(depth,i);
                    }
                    if(i==0)continue;
                    for(int lane=0;lane<lanes-1;lane++)
                    {int a=(i-1)*lanes+lane,b=i*lanes+lane;indices.Add(a);indices.Add(b+1);indices.Add(b);indices.Add(a);indices.Add(a+1);indices.Add(b+1);}
                }
                for(int i=1;i<river.Points.Count;i++)
                {
                    var p=river.Points[i];var direction=p-river.Points[i-1];
                    var previous=river.Points[i-1];float length=Vector3.Distance(previous,p);int count=Mathf.CeilToInt(length/4);
                    for(int n=0;n<count;n++)
                    {
                        var center=Vector3.Lerp(previous,p,(n+.5f)/count);
                        if(plan.RouteDistance(center.x,center.z)<14)continue;
                        var exclusion=new GameObject("Deep river navigation");exclusion.transform.SetParent(transform,false);
                        exclusion.transform.SetPositionAndRotation(center,Quaternion.LookRotation(new Vector3(direction.x,0,direction.z)));
                        var volume=exclusion.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();volume.area=1;volume.size=new Vector3(river.HalfWidth*1.7f,8,length/count+.1f);
                    }
                }
                var mesh=new Mesh{name=river.Id,vertices=vertices,uv=uv,triangles=indices.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
                var go=new GameObject(river.Id,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.renderingLayerMask=SurfaceCacheLighting.VisualOnlyRenderingLayer;
            }
        }
        void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);}
    }
}
