using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Topaz.Generation
{
    /// <summary>World-space sampling is independent of chunk load order and decoration density.</summary>
    public sealed partial class WildernessPlan
    {
        public const int Version = 8;
        public const int ChunkSize = 128;
        public const int WorldSize = 1024;
        public const int HalfSize = WorldSize / 2;
        public readonly int Seed;
        public readonly WoodlandSettings Settings;
        readonly float[,] heightSamples;
        readonly bool[,] sampledHeights;
        const float SampleSpacing=2;
        readonly List<Site> discoveries = new List<Site>();
        public readonly IReadOnlyList<Site> Discoveries;
        public int Extent => Settings.worldSize / 2;
        public int MinChunk => (int)Math.Floor(-Extent / (float)ChunkSize);
        public int MaxChunk => (int)Math.Ceiling(Extent / (float)ChunkSize);
        public bool Contains(float x,float z) => x>=-Extent && x<Extent && z>=-Extent && z<Extent;
        public bool ContainsChunk(Chunk chunk) => chunk.X>=MinChunk && chunk.X<MaxChunk && chunk.Z>=MinChunk && chunk.Z<MaxChunk;

        public readonly struct Site
        {
            public readonly string Id;
            public readonly float X, Z;
            public readonly int Kind;
            public Chunk Owner => Chunk.At(X,Z);
            public Site(string id, float x, float z, int kind) { Id=id; X=x; Z=z; Kind=kind; }
        }
        public readonly struct Chunk : IEquatable<Chunk>
        {
            public readonly int X, Z;
            public Chunk(int x, int z) { X=x; Z=z; }
            public string Id => CoordinateId("chunk",X,Z);
            public bool Equals(Chunk other) => X==other.X && Z==other.Z;
            public override bool Equals(object obj) => obj is Chunk other && Equals(other);
            public override int GetHashCode() => unchecked(X*397 ^ Z);
            public static Chunk At(float x, float z) => new Chunk((int)Math.Floor(x/ChunkSize),(int)Math.Floor(z/ChunkSize));
        }
        public WildernessPlan(int seed) : this(seed,new WoodlandSettings()) { }
        public WildernessPlan(int seed,WoodlandSettings settings)
        {
            if(settings==null)throw new ArgumentNullException(nameof(settings));
            settings.Validate();Settings=settings.Copy();Seed=seed;
            Discoveries=discoveries.AsReadOnly();
            if(Settings.boundedAreas)BuildAreaGeography();else BuildGeography();
            heightRoutes=IndexHeightRoutes();
            int samples=Settings.worldSize/(int)SampleSpacing+1;
            heightSamples=new float[samples,samples];sampledHeights=new bool[samples,samples];
        }
        void BuildGeography()
        {
            float angle=Random(0,0,20)*(float)Math.PI*2;
            Vector2 Rotate(float x,float z) => new Vector2(x*(float)Math.Cos(angle)-z*(float)Math.Sin(angle),x*(float)Math.Sin(angle)+z*(float)Math.Cos(angle));
            var ridgeA=Rotate(110,-135);var ridgeB=Rotate(300,115);
            landforms.Add(new Landform("ridge.0",ridgeA,ridgeB,110,Settings.relief*1.35f));
            landforms.Add(new Landform("valley.0",Rotate(-250,-160),Rotate(-40,270),95,-Settings.relief*.3f));
            BuildAlpineLandforms(Rotate);
            reservations.Add(new Reservation("start",0,0,9));
            var destinations=new[]{new Vector2(225,15),new Vector2(-75,150),new Vector2(-145,-35),new Vector2(70,-310),new Vector2(320,255),new Vector2(-300,260)};
            for(int i=0;i<destinations.Length;i++)
            {
                float scale=i<3?1:Settings.worldSize/1024f;
                var p=Rotate(destinations[i].x*scale+(Random(i,0,21)-.5f)*16,destinations[i].y*scale+(Random(i,0,22)-.5f)*16);
                var site=new Site(i<3?new[]{"landmark.0","camp.0","resource-site.0"}[i]:"discovery."+i,p.x,p.y,i%3);
                discoveries.Add(site);reservations.Add(new Reservation(site.Id,p.x,p.y,18));
            }
            foreach(var site in Discoveries)
            {
                Vector3 from=Vector3.zero;
                if(routes.Count>0)
                {
                    float nearest=float.MaxValue;
                    foreach(var existing in routes)foreach(var point in existing.Points)
                    {
                        if(new Vector2(point.x,point.z).magnitude<64)continue;
                        float distance=Distance(site.X,site.Z,point.x,point.z);
                        if(distance<nearest){nearest=distance;from=point;}
                    }
                }
                var delta=new Vector2(site.X-from.x,site.Z-from.z);
                var points=new List<Vector3>{from};
                // A bounded three-candidate search bends each approach toward lower traversal cost.
                int routeSteps=Math.Max(8,(int)Math.Ceiling(delta.magnitude/20));
                for(int i=1;i<=routeSteps;i++)
                {
                    float t=i/(float)routeSteps;var basePoint=new Vector2(from.x,from.z)+delta*t;
                    var side=new Vector2(-delta.y,delta.x).normalized;
                    float phase=delta.magnitude*t/38;
                    basePoint+=side*(Settings.trailMeander*(float)Math.Sin(phase)*(float)Math.Sin(t*Math.PI));
                    Vector2 chosen=basePoint;float best=float.MaxValue;
                    for(int candidate=-1;candidate<=1;candidate++)
                    {
                        var p=basePoint+side*(candidate*12*(float)Math.Sin(t*Math.PI));
                        float grade=Math.Abs(RawHeight(p.x,p.y)-points[points.Count-1].y)/Math.Max(1,Vector2.Distance(p,new Vector2(points[points.Count-1].x,points[points.Count-1].z)));
                        float cost=grade*8+ForestIntent(p.x,p.y)*.1f+Math.Abs(candidate)*.03f;
                        if(cost<best){chosen=p;best=cost;}
                    }
                    points.Add(new Vector3(chosen.x,RawHeight(chosen.x,chosen.y),chosen.y));
                }
                // Smooth candidate changes into bends before assigning grades. Alternating side choices
                // otherwise form hairpins whose overlapping height stamps make steep approach shoulders.
                for(int pass=0;pass<2;pass++)
                {
                    var smooth=new List<Vector3>(points);
                    for(int n=1;n<points.Count-1;n++)
                    {
                        var p=points[n-1]*.25f+points[n]*.5f+points[n+1]*.25f;
                        p.y=RawHeight(p.x,p.z);smooth[n]=p;
                    }
                    points=smooth;
                }
                // Later paths join the existing network away from the refuge. Only the first
                // departure clears the small authored furniture footprint; there is no ring hub.
                var departure=points;
                if(routes.Count==0)
                {
                    departure=new List<Vector3>{Vector3.zero,new Vector3(1,0,-1),new Vector3(1,0,8)};
                    foreach(var point in points)if(new Vector2(point.x,point.z).magnitude>12)departure.Add(point);
                }
                for(int n=1;n<departure.Count;n++)
                {var point=departure[n];var prior=departure[n-1];float grade=Vector2.Distance(new Vector2(point.x,point.z),new Vector2(prior.x,prior.z))*.25f;point.y=Mathf.Clamp(point.y,prior.y-grade,prior.y+grade);departure[n]=point;}
                routes.Add(new Route("route."+site.Id,site.Id,departure));
            }
            // Two ponds cross planned approaches. Every part remains shallow and walkable.
            for(int i=0;i<2;i++)
            {
                var p=routes[i+1].Points[routes[i+1].Points.Count-4];
                waters.Add(new WaterBody("pond."+i,routes[i+1].Id,p.x,p.z,14+i*3,p.y+.12f));
                reservations.Add(new Reservation("pond."+i,p.x,p.z,39+i*3));
            }
            BuildTrailLoop();BuildRiverCrossings();
            // Guaranteed starter nodes have their identity before any decoration is considered.
            for(int i=0;i<16;i++)
            {
                bool placed=false;
                for(int candidate=0;candidate<64;candidate++)
                {
                    double a=Random(i,candidate,301)*Math.PI*2;float radius=Mathf.Lerp(24,65,Random(i,candidate,302));
                    float x=(float)Math.Cos(a)*radius,z=(float)Math.Sin(a)*radius;
                    if(RouteDistance(x,z)<7 || NearRequired(x,z,7) || !SuitableRequiredResource(x,z))continue;
                    requiredResources.Add(new Site("starter.resource."+i,x,z,i%2==0?0:1));placed=true;break;
                }
                if(!placed)throw new InvalidOperationException($"Seed {Seed}: starter.resource.{i} exhausted 64 clearance candidates.");
            }
            var quarry=Discoveries[2];var quarryRoute=routes[2];
            var direction=quarryRoute.Points[quarryRoute.Points.Count-1]-quarryRoute.Points[quarryRoute.Points.Count-2];direction.y=0;direction.Normalize();var quarrySide=new Vector3(-direction.z,0,direction.x);
            for(int i=0;i<6;i++)
            {
                bool placed=false;
                for(int candidate=0;candidate<128;candidate++)
                {
                    int slot=i+candidate;float side=(slot%2==0?-1:1)*(8+(slot/2%3)*4);
                    var p=new Vector3(quarry.X,0,quarry.Z)+quarrySide*side-direction*(5+(slot/6)*6);
                    if(!SuitableRequiredResource(p.x,p.z))continue;
                    requiredResources.Add(new Site("quarry.resource."+i,p.x,p.z,0));placed=true;break;
                }
                if(!placed)throw new InvalidOperationException($"Seed {Seed}: no safe quarry placement for node {i}.");
            }
            BuildDestinations();
        }
        /// <summary>Bounded lazy final field on the same 2 m lattice as native Terrain; no loaded-scene query.</summary>
        public float Height(float x,float z)
        {
            if(float.IsNaN(x)||float.IsInfinity(x)||float.IsNaN(z)||float.IsInfinity(z))throw new ArgumentException("World coordinates must be finite.");
            if(x < -Extent || z < -Extent || x > Extent || z > Extent)return AnalyticHeight(x,z);
            float gx=(x+Extent)/SampleSpacing,gz=(z+Extent)/SampleSpacing;
            int ix=(int)Math.Floor(gx),iz=(int)Math.Floor(gz),max=heightSamples.GetLength(0)-1;
            float tx=gx-ix,tz=gz-iz;
            float a=Sample(ix,iz);if(tx==0 && tz==0)return a;
            float b=Sample(Math.Min(ix+1,max),iz),c=Sample(ix,Math.Min(iz+1,max)),d=Sample(Math.Min(ix+1,max),Math.Min(iz+1,max));
            return Mathf.Lerp(Mathf.Lerp(a,b,tx),Mathf.Lerp(c,d,tx),tz);
        }
        float Sample(int x,int z)
        {
            if(!sampledHeights[z,x]){heightSamples[z,x]=AnalyticHeight(x*SampleSpacing-Extent,z*SampleSpacing-Extent);sampledHeights[z,x]=true;}
            return heightSamples[z,x];
        }
        float AnalyticHeight(float x,float z,bool shoulders=true)
        {
            if(Settings.boundedAreas)return AreaHeight(x,z);
            float height=RawHeight(x,z)+(Noise(x/23,z/23,1101)-.5f)*Settings.localRelief+(Noise(x/61,z/61,1111)-.5f)*Settings.localRelief*2;
            height+=PeakRise(x,z);
            // Ordered, bounded stamps: routes, water, destination pads. All consumers query this result.
            foreach(var site in Discoveries)
            {
                float blend=1-Smooth((Distance(x,z,site.X,site.Z)-18)/16);
                if(blend>0)height=Mathf.Lerp(height,routes[discoveries.IndexOf(site)].Points[routes[discoveries.IndexOf(site)].Points.Count-1].y,blend);
            }
            float nearest=float.MaxValue,targetHeight=height,totalRouteWeight=0,weightedRouteHeight=0;
            void AccumulateRoute(Vector3 a,Vector3 b)
            {
                float distance=Segment(x,z,a,b,out float t);
                nearest=Math.Min(nearest,distance);
                if(distance>=20)return;
                float weight=1-Smooth(distance/20);weight*=weight;
                totalRouteWeight+=weight;weightedRouteHeight+=weight*Mathf.Lerp(a.y,b.y,t);
            }
            if(heightRoutes!=null)
            {
                if(heightRoutes.TryGetValue(Chunk.At(x,z),out var segments))
                    foreach(var segment in segments)AccumulateRoute(segment.A,segment.B);
            }
            else // Geography construction queries provisional routes before the immutable index exists.
                foreach(var route in routes)for(int i=1;i<route.Points.Count;i++)AccumulateRoute(route.Points[i-1],route.Points[i]);
            if(totalRouteWeight>0)targetHeight=weightedRouteHeight/totalRouteWeight;
            // Sheltered route pockets alternate with softer gaps. The inner corridor
            // stays independent of these terrain shoulders, including route junctions.
            if(shoulders && nearest<48)
                height+=RouteShoulder(x,z,nearest);
            if(nearest<14)height=Mathf.Lerp(height,targetHeight,1-Smooth((nearest-Route.HalfWidth)/7));
            foreach(var water in waters)
            {
                float distance=Distance(x,z,water.X,water.Z);
                if(distance>=water.Radius+24)continue;
                float depth=.5f*(1-Smooth((distance-water.Radius*.5f)/(water.Radius*.5f)));
                depth=Math.Min(depth,Mathf.Lerp(.22f,.5f,Smooth((RouteDistance(x,z)-3)/5)));
                float bed=water.Surface-depth;
                height=Mathf.Lerp(bed,height,Smooth((distance-water.Radius)/24));
            }
            height=CarveRiver(x,z,height);
            return Math.Max(0,height)*Smooth((Distance(x,z,0,0)-9)/18);
        }
        float RawHeight(float x,float z)
        {
            float height=Noise(x/150,z/150,1)*Settings.relief+Noise(x/48,z/48,2)*2;
            foreach(var form in landforms)
            {
                float d=Segment(x,z,new Vector3(form.Start.x,0,form.Start.y),new Vector3(form.End.x,0,form.End.y),out _);
                float weight=1-Smooth(d/form.Width);height+=weight*form.Height;
            }
            height=ShapeRiverValley(x,z,height);
            height=Math.Max(0,height)*Smooth((Distance(x,z,0,0)-9)/56);
            float edge=Smooth((Math.Max(Math.Abs(x),Math.Abs(z))-(Extent-62))/58);
            return edge<=0?height:height+edge*(42+16*Noise(x/85,z/85,31));
        }
        public float Moisture(float x,float z)
        {
            float moisture=Noise(x/110,z/110,3);
            foreach(var water in waters)moisture=Math.Max(moisture,1-Smooth((Distance(x,z,water.X,water.Z)-water.Radius)/30));
            float riverDistance=RiverDistance(x,z,out _,out float width);
            return Math.Max(moisture,1-Smooth((riverDistance-width)/40));
        }
        public float Forest(float x,float z) => Biomes(x,z).Woodland;
        public float Slope(float x,float z)
        {
            float dx=(Height(x+1,z)-Height(x-1,z))*.5f,dz=(Height(x,z+1)-Height(x,z-1))*.5f;
            return (float)Math.Sqrt(dx*dx+dz*dz);
        }
        public float WornGround(float x,float z)
        {
            float radius=(float)Math.Sqrt(x*x+z*z);
            float dirt=1-Smooth((radius-3)/3);
            foreach(var site in Discoveries)
            {
                float dx=x-site.X,dz=z-site.Z;
                dirt=Math.Max(dirt,(1-Smooth(((float)Math.Sqrt(dx*dx+dz*dz)-3)/5))*.7f);
            }
            foreach(var site in destinations)
                dirt=Math.Max(dirt,(1-Smooth((Distance(x,z,site.X,site.Z)-site.Radius*.5f)/6))*.55f);
            return Math.Max(dirt,(1-Smooth((RouteDistance(x,z)-TrailHalfWidth(x,z))/(1.1f+Noise(x/15,z/15,1102))))*.8f);
        }
        public float ClearingDistance(float x,float z)
        {
            float d=(float)Math.Sqrt(x*x+z*z)-7;
            foreach(var s in Discoveries) d=Math.Min(d,(float)Math.Sqrt((x-s.X)*(x-s.X)+(z-s.Z)*(z-s.Z))-9);
            foreach(var s in destinations)d=Math.Min(d,Distance(x,z,s.X,s.Z)-s.Radius);
            return d;
        }
        public IReadOnlyList<Site> Resources(Chunk chunk)
        {
            if(resourceCache.TryGetValue(chunk,out var cached))return cached;
            var result=new List<Site>();
            if(!ContainsChunk(chunk))return result.AsReadOnly();
            foreach(var site in requiredResources)if(site.Owner.Equals(chunk))result.Add(site);
            for(int z=0;z<4;z++)for(int x=0;x<4;x++)
            {
                int gx=chunk.X*4+x,gz=chunk.Z*4+z;
                float px=gx*32+6+Random(gx,gz,101)*20,pz=gz*32+6+Random(gx,gz,102)*20;
                if(Reserved(px,pz,4) || Math.Abs(px)>Extent-64 || Math.Abs(pz)>Extent-64 || Slope(px,pz)>.35f || WaterDepth(px,pz)>0 || NearRequired(px,pz,9))continue;
                result.Add(new Site(LogicalId(CoordinateId("resource",gx,gz)),px,pz,(int)(Random(gx,gz,103)*4)));
            }
            return result.AsReadOnly();
        }
        bool NearRequired(float x,float z,float radius)
        { foreach(var site in requiredResources)if(Distance(x,z,site.X,site.Z)<radius)return true;return false; }
        Site DecorationCandidate(int gx,int gz)
        {
            float x=gx*8+2+Random(gx,gz,701)*4,z=gz*8+2+Random(gx,gz,702)*4;
            var biome=Biomes(x,z);float choice=Random(gx,gz,703);float grove=Canopy(x,z);
            int kind=choice<grove*.9f+.015f?0:choice>1-biome.Highland*.65f-.04f?1:2;
            return new Site(CoordinateId("decor",gx,gz),x,z,kind);
        }
        public IReadOnlyList<Site> Decorations(Chunk chunk,int count)
        {
            var result=new List<Site>();
            if(!ContainsChunk(chunk)||count<=0)return result.AsReadOnly();
            if(count==Settings.decorationCount && decorationCache.TryGetValue(chunk,out var cached))return cached;
            var resources=new List<Site>();
            // Halo protects resource footprints on the other side of a cell boundary.
            for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)resources.AddRange(Resources(new Chunk(chunk.X+x,chunk.Z+z)));
            for(int z=0;z<16;z++)for(int x=0;x<16;x++)
            {
                int gx=chunk.X*16+x,gz=chunk.Z*16+z;var site=DecorationCandidate(gx,gz);
                float grouping=site.Kind==0?1:
                    site.Kind==1?Mathf.Lerp(.25f,1,Noise(site.X/35,site.Z/35,1105)):
                    Mathf.Lerp(.08f,.7f,Noise(site.X/18,site.Z/18,1106));
                if(Random(gx,gz,704)>Math.Min(1,count/400f)*grouping || LandmarkApproach(site.X,site.Z)>.2f)continue;
                float radius=site.Kind==0?3:site.Kind==1?3:1;
                if(WaterDepth(site.X,site.Z)>0||Reserved(site.X,site.Z,radius+3)||Math.Abs(site.X)>Extent-17||Math.Abs(site.Z)>Extent-17||NearRequired(site.X,site.Z,radius+5)||Slope(site.X,site.Z)>(site.Kind==1?.8f:.35f))continue;
                bool occupied=false;
                foreach(var r in resources)if(Distance(r.X,r.Z,site.X,site.Z)<radius+5){occupied=true;break;}
                if(occupied)continue;
                // Resolve neighbor conflicts before filtering density, using stable coordinate priority.
                for(int dz=-1;dz<=1&&!occupied;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dz==0)continue;
                    var other=DecorationCandidate(gx+dx,gz+dz);
                    float otherRadius=other.Kind==0?3:other.Kind==1?3:1;
                    float priority=Random(gx,gz,705),otherPriority=Random(gx+dx,gz+dz,705);
                    if(Distance(site.X,site.Z,other.X,other.Z)<radius+otherRadius &&
                        (otherPriority<priority || (otherPriority==priority && (dz<0||dz==0&&dx<0)))){occupied=true;break;}
                }
                if(!occupied && site.Kind==1 && !SupportsFootprint(site.X,site.Z,3,.8f))continue;
                if(!occupied)result.Add(site);
            }
            return result.AsReadOnly();
        }
        public bool SupportsFootprint(float x,float z,float radius,float maximumSlope)
        {
            float center=Height(x,z),dx=(Height(x+1,z)-Height(x-1,z))*.5f,dz=(Height(x,z+1)-Height(x,z-1))*.5f;
            for(int iz=-1;iz<=1;iz+=2)for(int ix=-1;ix<=1;ix+=2)
            {
                float px=x+ix*radius,pz=z+iz*radius;
                if(Slope(px,pz)>maximumSlope || Math.Abs(Height(px,pz)-(center+dx*ix*radius+dz*iz*radius))>.5f)return false;
            }
            return true;
        }
        static string CoordinateId(string prefix,int x,int z) => prefix+"."+x.ToString(CultureInfo.InvariantCulture)+"."+z.ToString(CultureInfo.InvariantCulture);
        public float[,] Heights(Chunk chunk,int resolution)
        {
            var values=new float[resolution,resolution];
            for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
                values[z,x]=Height(chunk.X*ChunkSize+x*ChunkSize/(float)(resolution-1),chunk.Z*ChunkSize+z*ChunkSize/(float)(resolution-1));
            return values;
        }
        public void Validate()
        {
            Settings.Validate();
            var ids=new HashSet<string>();
            for(int z=MinChunk;z<MaxChunk;z++)for(int x=MinChunk;x<MaxChunk;x++)foreach(var site in Resources(new Chunk(x,z)))
                if(!ids.Add(site.Id)||!Contains(site.X,site.Z)||Slope(site.X,site.Z)>.65f)
                    throw new InvalidOperationException($"Seed {Seed}: invalid resource {site.Id} at ({site.X},{site.Z}).");
            foreach(var route in routes)for(int i=1;i<route.Points.Count;i++)
            {
                var a=route.Points[i-1];var b=route.Points[i];int steps=(int)Math.Ceiling(Vector3.Distance(a,b));
                var side=new Vector3(-(b.z-a.z),0,b.x-a.x).normalized;
                for(int n=0;n<=steps;n++)for(int lane=-1;lane<=1;lane++)
                {
                    var p=Vector3.Lerp(a,b,n/(float)steps)+side*(lane*2);
                    if(Slope(p.x,p.z)>.65f||WaterDepth(p.x,p.z)>.55f)
                        throw new InvalidOperationException($"Seed {Seed}: {route.Id} traversal failed at ({p.x},{p.z}); slope={Slope(p.x,p.z)}, water={WaterDepth(p.x,p.z)}.");
                }
            }
            foreach(var site in requiredResources)if(!ids.Contains(site.Id))throw new InvalidOperationException($"Seed {Seed}: missing required node {site.Id}.");
        }
        public float Random(int x,int z,uint stream) => (Mix(Mix(unchecked((uint)Seed),unchecked((uint)x)),Mix(unchecked((uint)z),stream))&0xffffff)/16777216f;
        float Noise(float x,float z,uint stream)
        {
            int ix=(int)Math.Floor(x),iz=(int)Math.Floor(z);float tx=Smooth(x-ix),tz=Smooth(z-iz);
            return (Random(ix,iz,stream)*(1-tx)+Random(ix+1,iz,stream)*tx)*(1-tz)+(Random(ix,iz+1,stream)*(1-tx)+Random(ix+1,iz+1,stream)*tx)*tz;
        }
        static float Smooth(float x) {x=Math.Clamp(x,0,1);return x*x*(3-2*x);}
        static uint Mix(uint a,uint b) {unchecked {uint v=a^(b*0x9e3779b9u);v^=v>>16;v*=0x7feb352du;v^=v>>15;v*=0x846ca68bu;return v^(v>>16);}}
    }
}
