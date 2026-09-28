using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Topaz.Generation
{
    public sealed partial class WildernessPlan
    {
        public const float TerrainHeight = 256;
        public sealed class River
        {
            public readonly string Id;
            public readonly IReadOnlyList<Vector3> Points;
            public readonly float HalfWidth;
            public River(string id, List<Vector3> points, float halfWidth)
            { Id = id; Points = points.AsReadOnly(); HalfWidth = halfWidth; }
        }
        public readonly struct RiverCrossing
        {
            public readonly string Id;
            public readonly Vector3 Position, Forward;
            public readonly bool Bridge;
            public RiverCrossing(string id,Vector3 position,Vector3 forward,bool bridge)
            {Id=id;Position=position;Forward=forward;Bridge=bridge;}
        }
        readonly List<RiverCrossing> crossings = new List<RiverCrossing>();
        public IReadOnlyList<RiverCrossing> Crossings => crossings;
        void BuildTrailLoop()
        {
            // Bounded A* chooses a gentle contour connector instead of cutting across a ridge.
            var from=routes[0].Points[routes[0].Points.Count-1];var to=routes[1].Points[routes[1].Points.Count-1];
            const int spacing=8;int side=Settings.worldSize/spacing+1,count=side*side;
            int Cell(Vector3 p)=>Mathf.RoundToInt((p.z+Extent)/spacing)*side+Mathf.RoundToInt((p.x+Extent)/spacing);
            Vector3 Point(int cell)=>new Vector3(cell%side*spacing-Extent,0,cell/side*spacing-Extent);
            var heights=new float[count];var sampled=new bool[count];
            float HeightAt(int cell)
            {if(!sampled[cell]){var p=Point(cell);heights[cell]=ConnectorHeight(p.x,p.z);sampled[cell]=true;}return heights[cell];}
            var cost=new float[count];var previous=new int[count];var closed=new bool[count];var opened=new bool[count];
            for(int i=0;i<count;i++){cost[i]=float.MaxValue;previous[i]=-1;}
            int start=Cell(from),goal=Cell(to);var open=new List<int>{start};cost[start]=0;opened[start]=true;
            float Estimate(int cell){var p=Point(cell);return Vector2.Distance(new Vector2(p.x,p.z),new Vector2(to.x,to.z));}
            bool found=false;
            while(open.Count>0)
            {
                int pick=0;float best=float.MaxValue;
                for(int i=0;i<open.Count;i++){float score=cost[open[i]]+Estimate(open[i]);if(score<best){best=score;pick=i;}}
                int cell=open[pick];open.RemoveAt(pick);opened[cell]=false;
                if(cell==goal){found=true;break;}closed[cell]=true;
                int x=cell%side,z=cell/side;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dz==0)continue;int nx=x+dx,nz=z+dz;if(nx<2||nz<2||nx>=side-2||nz>=side-2)continue;
                    int next=nz*side+nx;if(closed[next])continue;
                    float length=dx!=0&&dz!=0?spacing*1.41421356f:spacing;
                    float grade=Math.Abs(HeightAt(next)-HeightAt(cell))/length;if(grade>.2f)continue;
                    var p=Point(next);float water=WaterSurface(p.x,p.z);if(!float.IsNaN(water)&&water-HeightAt(next)>.5f)continue;
                    float candidate=cost[cell]+length*(1+grade*grade*12);
                    if(candidate>=cost[next])continue;
                    cost[next]=candidate;previous[next]=cell;if(!opened[next]){opened[next]=true;open.Add(next);}
                }
            }
            if(!found)throw new InvalidOperationException($"Seed {Seed}: no gentle valley loop was found.");
            var cells=new List<int>();for(int cell=goal;cell!=-1;cell=previous[cell])cells.Add(cell);cells.Reverse();
            var points=new List<Vector3>();from.y=ConnectorHeight(from.x,from.z);points.Add(from);
            foreach(int cell in cells){var p=Point(cell);p.y=HeightAt(cell);if(Vector3.Distance(points[points.Count-1],p)>.5f)points.Add(p);}
            to.y=ConnectorHeight(to.x,to.z);if(Vector3.Distance(points[points.Count-1],to)>.5f)points.Add(to);
            routes.Add(new Route("route.valley-loop",Discoveries[1].Id,points));
        }
        float ConnectorHeight(float x,float z)
        {
            float nearest=float.MaxValue,closest=0,total=0,weighted=0;
            foreach(var route in routes)for(int i=1;i<route.Points.Count;i++)
            {
                float distance=Segment(x,z,route.Points[i-1],route.Points[i],out float t);
                float height=Mathf.Lerp(route.Points[i-1].y,route.Points[i].y,t);
                if(distance<nearest){nearest=distance;closest=height;}
                if(distance>=20)continue;float weight=1-Smooth(distance/20);weight*=weight;total+=weight;weighted+=weight*height;
            }
            float target=total>0?weighted/total:closest;
            return Mathf.Lerp(target,AnalyticHeight(x,z),Smooth((nearest-20)/20));
        }
        bool SuitableRequiredResource(float x,float z)
        {
            float height=AnalyticHeight(x,z),surface=WaterSurface(x,z);
            if(!float.IsNaN(surface)&&surface-height>.05f)return false;
            float dx=(AnalyticHeight(x+1,z)-AnalyticHeight(x-1,z))*.5f,dz=(AnalyticHeight(x,z+1)-AnalyticHeight(x,z-1))*.5f;
            return dx*dx+dz*dz<.09f && !NearRequired(x,z,6) && RouteDistance(x,z)>6;
        }
        void BuildRiverCrossings()
        {
            foreach(var route in routes)
            {
                float best=float.MaxValue;Vector3 location=default,forward=default;
                for(int i=1;i<route.Points.Count;i++)
                {
                    var a=route.Points[i-1];var b=route.Points[i];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)));
                    for(int n=0;n<=steps;n++)
                    {
                        var p=Vector3.Lerp(a,b,n/(float)steps);float d=RiverDistance(p.x,p.z,out float surface,out float width);
                        if(d>=best || d>width)continue;
                        best=d;location=new Vector3(p.x,surface,p.z);forward=(b-a);forward.y=0;forward.Normalize();
                    }
                }
                if(best<float.MaxValue)crossings.Add(new RiverCrossing("crossing."+route.Id,location,forward,crossings.Count%2==0));
            }
        }
        readonly List<River> rivers = new List<River>();
        public IReadOnlyList<River> Rivers => rivers;
        readonly Dictionary<Chunk, IReadOnlyList<Site>> resourceCache = new Dictionary<Chunk, IReadOnlyList<Site>>();
        readonly Dictionary<Chunk, IReadOnlyList<Site>> decorationCache = new Dictionary<Chunk, IReadOnlyList<Site>>();
        public bool Precomputed { get; private set; }

        void BuildAlpineLandforms(Func<float, float, Vector2> rotate)
        {
            float e = Extent;
            // Broad playable ridges frame sheltered valleys; narrow rock meshes add silhouette detail.
            for (int i = 0; i < 5; i++)
            {
                float z = (-.7f + i * .34f) * e;
                float side = i % 2 == 0 ? 1 : -1;
                landforms.Add(new Landform("alpine.ridge." + i,
                    rotate(side * e * .48f, z), rotate(side * e * .76f, z + e * .22f),
                    e * .27f, (42 + Random(i, 0, 1001) * 46) * Settings.worldSize / 2048f));
            }
            var points = new List<Vector3>();
            for (int i = 0; i <= 48; i++)
            {
                float z = Mathf.Lerp(-e + 40, e - 40, i / 48f);
                float x = -e * .34f + (float)Math.Sin(z / 145 + Random(0, 0, 1002) * 6) * e * .07f;
                var p = rotate(x, z);
                points.Add(new Vector3(p.x, 8 + i * .22f, p.y));
            }
            rivers.Add(new River("river.alpine", points, 5));
        }
        public float RiverDistance(float x, float z, out float surface, out float width)
        {
            float nearest = float.MaxValue; surface = 0; width = 0;
            foreach (var river in rivers)
                for (int i = 1; i < river.Points.Count; i++)
                {
                    float d = Segment(x, z, river.Points[i - 1], river.Points[i], out float t);
                    if (d >= nearest) continue;
                    nearest = d; surface = Mathf.Lerp(river.Points[i - 1].y, river.Points[i].y, t); width = river.HalfWidth;
                }
            return nearest;
        }
        float ShapeRiverValley(float x, float z, float height)
        {
            float distance = RiverDistance(x, z, out float surface, out float width);
            if (distance > width + 100) return height;
            return Mathf.Lerp(surface + .4f, height, Smooth((distance - width - 12) / 88));
        }
        float CarveRiver(float x, float z, float height)
        {
            float distance = RiverDistance(x, z, out float surface, out float width);
            if (distance >= width + 24) return height;
            float depth = Mathf.Lerp(.2f, 1.3f, Smooth((RouteDistance(x, z) - 4) / 8));
            if(distance<=width)
                return Mathf.Lerp(surface-depth,surface+.08f,Smooth((distance-Mathf.Max(0,width-3))/3));
            return Mathf.Lerp(surface+.08f,height,Smooth((distance-width-4)/20));
        }
        public bool WalkableWater(float x, float z) => WaterDepth(x, z) <= .55f;

        /// <summary>Called on an exclusively owned worker plan before publishing it to the scene.</summary>
        public void Precompute(CancellationToken cancellation)
        {
            for (int z = 0; z < heightSamples.GetLength(0); z++)
            {
                cancellation.ThrowIfCancellationRequested();
                for (int x = 0; x < heightSamples.GetLength(1); x++) Sample(x, z);
            }
            for (int z = MinChunk; z < MaxChunk; z++)
                for (int x = MinChunk; x < MaxChunk; x++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var key = new Chunk(x, z); resourceCache[key] = Resources(key);
                }
            for (int z = MinChunk; z < MaxChunk; z++)
                for (int x = MinChunk; x < MaxChunk; x++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var key = new Chunk(x, z); decorationCache[key] = Decorations(key, Settings.decorationCount);
                }
            Precomputed = true;
        }
    }
}
