using System;
using System.Collections.Generic;
using UnityEngine;

namespace Topaz.Generation
{
    public sealed partial class WildernessPlan
    {
        public readonly struct BiomeWeights
        {
            public readonly float Meadow, Woodland, Highland;
            public BiomeWeights(float meadow,float woodland,float highland)
            { float total=meadow+woodland+highland;Meadow=meadow/total;Woodland=woodland/total;Highland=highland/total; }
        }
        public readonly struct Reservation
        {
            public readonly string Id;
            public readonly float X,Z,Radius;
            public Chunk Owner => Chunk.At(X,Z);
            public Reservation(string id,float x,float z,float radius){Id=id;X=x;Z=z;Radius=radius;}
            public bool Intersects(Chunk cell) => X+Radius>=cell.X*ChunkSize && X-Radius<(cell.X+1)*ChunkSize && Z+Radius>=cell.Z*ChunkSize && Z-Radius<(cell.Z+1)*ChunkSize;
        }
        public sealed class Route
        {
            public readonly string Id,DestinationId;
            public readonly IReadOnlyList<Vector3> Points;
            public const float HalfWidth=4f;
            public Route(string id,string destinationId,List<Vector3> points){Id=id;DestinationId=destinationId;Points=points.AsReadOnly();}
        }
        public readonly struct WaterBody
        {
            public readonly string Id,RouteId;
            public readonly float X,Z,Radius,Surface;
            public Chunk Owner => Chunk.At(X,Z);
            public WaterBody(string id,string route,float x,float z,float radius,float surface)
            {Id=id;RouteId=route;X=x;Z=z;Radius=radius;Surface=surface;}
        }
        public readonly struct Landform
        {
            public readonly string Id;
            public readonly Vector2 Start,End;
            public readonly float Width,Height;
            public Landform(string id,Vector2 start,Vector2 end,float width,float height){Id=id;Start=start;End=end;Width=width;Height=height;}
        }
        readonly List<Route> routes=new List<Route>();
        readonly List<Reservation> reservations=new List<Reservation>();
        readonly List<WaterBody> waters=new List<WaterBody>();
        readonly List<Landform> landforms=new List<Landform>();
        readonly List<Site> requiredResources=new List<Site>();
        public IReadOnlyList<Route> Routes => routes.AsReadOnly();
        public IReadOnlyList<Reservation> Reservations => reservations.AsReadOnly();
        public IReadOnlyList<WaterBody> Waters => waters.AsReadOnly();
        public IReadOnlyList<Landform> Landforms => landforms.AsReadOnly();
        public IReadOnlyList<Site> RequiredResources => requiredResources.AsReadOnly();
        public Vector3 Normal(float x,float z) => new Vector3(Height(x-1,z)-Height(x+1,z),2,Height(x,z-1)-Height(x,z+1)).normalized;
        public float WaterDepth(float x,float z)
        {
            foreach(var water in waters)if(Distance(x,z,water.X,water.Z)<water.Radius)
                return Math.Max(0,water.Surface-Height(x,z));
            float distance=RiverDistance(x,z,out float surface,out float width);
            return distance<width?Math.Max(0,surface-Height(x,z)):0;
        }
        public float WaterSurface(float x,float z)
        {
            foreach(var water in waters)if(Distance(x,z,water.X,water.Z)<water.Radius)return water.Surface;
            float distance=RiverDistance(x,z,out float surface,out float width);
            return distance<width?surface:float.NaN;
        }
        public bool Reserved(float x,float z,float margin=0)
        {
            foreach(var area in reservations)if(Distance(x,z,area.X,area.Z)<area.Radius+margin)return true;
            foreach(var site in requiredResources)
            {
                var from=site.Id.StartsWith("starter.")?Vector3.zero:new Vector3(Discoveries[2].X,0,Discoveries[2].Z);
                if(Segment(x,z,from,new Vector3(site.X,0,site.Z),out _)<2+margin)return true;
            }
            return RouteDistance(x,z)<Route.HalfWidth+margin;
        }
        public float RouteDistance(float x,float z)
        {
            float distance=float.MaxValue;
            foreach(var route in routes)for(int i=1;i<route.Points.Count;i++)
                distance=Math.Min(distance,Segment(x,z,route.Points[i-1],route.Points[i],out _));
            return distance;
        }
        public Vector4 SurfaceWeights(float x,float z)
        {
            var biome=Biomes(x,z);
            float rock=Mathf.Max(biome.Highland*.65f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,.65f,Slope(x,z))));
            float dirt=Mathf.Max(WornGround(x,z),WaterDepth(x,z)>0?.8f:0),forest=Canopy(x,z)*Mathf.Lerp(.48f,.85f,Noise(x/18,z/18,1104));
            return new Vector4((1-rock)*(1-dirt)*(1-forest),(1-rock)*dirt,rock,(1-rock)*(1-dirt)*forest);
        }
        public float TraversalCost(float x,float z)
        {
            if(!Contains(x,z)||Math.Abs(x)>Extent-16||Math.Abs(z)>Extent-16)return float.PositiveInfinity;
            float slope=Slope(x,z);
            return slope>.65f||!WalkableWater(x,z)?float.PositiveInfinity:1+slope*5+Forest(x,z)*.3f+WaterDepth(x,z)*2;
        }
        public BiomeWeights Biomes(float x,float z)
        {
            float high=0,wood=ForestIntent(x,z);
            foreach(var form in landforms)
                if(form.Height>0)high=Math.Max(high,1-Smooth(Segment(x,z,new Vector3(form.Start.x,0,form.Start.y),new Vector3(form.End.x,0,form.End.y),out _)/form.Width));
            float start=Smooth((Distance(x,z,0,0)-35)/60);
            high*=start;
            wood*=start*(1-high);
            return new BiomeWeights(Math.Max(.05f,1-wood-high),wood,high);
        }
        float ForestIntent(float x,float z)
        {
            float clusters=Smooth((Noise(x/100,z/100,4)-.25f)*3.6f);
            if(Discoveries.Count>1)clusters=Math.Max(clusters,1-Smooth((Distance(x,z,Discoveries[1].X,Discoveries[1].Z)-28)/95));
            return clusters;
        }
        static float Distance(float x,float z,float ax,float az) => (float)Math.Sqrt((x-ax)*(x-ax)+(z-az)*(z-az));
        static float Segment(float x,float z,Vector3 a,Vector3 b,out float t)
        {
            float dx=b.x-a.x,dz=b.z-a.z;
            t=Math.Clamp(((x-a.x)*dx+(z-a.z)*dz)/Math.Max(.001f,dx*dx+dz*dz),0,1);
            return Distance(x,z,a.x+dx*t,a.z+dz*t);
        }
    }
}
