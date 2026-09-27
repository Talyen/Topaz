using System;
using System.Collections.Generic;

namespace Topaz.Generation
{
    /// <summary>World-space sampling is independent of chunk load order and decoration density.</summary>
    public sealed class WildernessPlan
    {
        public const int Version = 2;
        public const int ChunkSize = 128;
        public const int WorldSize = 1024;
        public const int HalfSize = WorldSize / 2;
        public readonly int Seed;
        public readonly WoodlandSettings Settings;
        public readonly List<Site> Discoveries = new List<Site>();

        public readonly struct Site
        {
            public readonly string Id;
            public readonly float X, Z;
            public readonly int Kind;
            public Site(string id, float x, float z, int kind) { Id=id; X=x; Z=z; Kind=kind; }
        }
        public readonly struct Chunk : IEquatable<Chunk>
        {
            public readonly int X, Z;
            public Chunk(int x, int z) { X=x; Z=z; }
            public string Id => "chunk."+X+"."+Z;
            public bool Valid => X >= -4 && X < 4 && Z >= -4 && Z < 4;
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
            // One candidate per macro cell keeps discovery spacing predictable across chunk borders.
            for(int z=-2;z<2;z++) for(int x=-2;x<2;x++)
            {
                float px=x*224+112+(Random(x,z,11)-.5f)*80;
                float pz=z*224+112+(Random(x,z,12)-.5f)*80;
                if(px*px+pz*pz<70*70) continue;
                Discoveries.Add(new Site("discovery."+x+"."+z,px,pz,(int)(Random(x,z,13)*3)));
            }
        }
        public float Height(float x,float z)
        {
            float height=BaseHeight(x,z);
            float origin=Smooth(((float)Math.Sqrt(x*x+z*z)-18)/52f);
            height*=origin;
            foreach(var site in Discoveries)
            {
                float dx=x-site.X,dz=z-site.Z;
                float blend=Smooth(((float)Math.Sqrt(dx*dx+dz*dz)-9)/12);
                if(blend<1) height=BaseHeight(site.X,site.Z)*(1-blend)+height*blend;
            }
            // Rocked outer slopes form a visible finite boundary.
            float edge=Smooth((Math.Max(Math.Abs(x),Math.Abs(z))-450)/58);
            return height+edge*42;
        }
        float BaseHeight(float x,float z) => Noise(x/150,z/150,1)*Settings.relief + Noise(x/48,z/48,2)*3;
        public float Moisture(float x,float z) => Noise(x/110,z/110,3);
        public float Forest(float x,float z) => Smooth((Noise(x/90,z/90,4)-.32f)*2.8f);
        public float Slope(float x,float z) => Math.Max(Math.Abs(Height(x+1,z)-Height(x-1,z)),Math.Abs(Height(x,z+1)-Height(x,z-1)))*.5f;
        public float WornGround(float x,float z)
        {
            float radius=(float)Math.Sqrt(x*x+z*z);
            float dirt=1-Smooth((radius-3)/3);
            float trail=1-Smooth((Math.Abs(x-(float)Math.Sin(z*.06)*2)-.7f)/1.4f);
            dirt=Math.Max(dirt,trail*(1-Smooth((radius-45)/35))*.8f);
            foreach(var site in Discoveries)
            {
                float dx=x-site.X,dz=z-site.Z;
                dirt=Math.Max(dirt,(1-Smooth(((float)Math.Sqrt(dx*dx+dz*dz)-3)/5))*.7f);
            }
            return dirt;
        }
        public float ClearingDistance(float x,float z)
        {
            float d=(float)Math.Sqrt(x*x+z*z)-16;
            foreach(var s in Discoveries) d=Math.Min(d,(float)Math.Sqrt((x-s.X)*(x-s.X)+(z-s.Z)*(z-s.Z))-9);
            return d;
        }
        public List<Site> Resources(Chunk chunk)
        {
            var result=new List<Site>();
            for(int z=0;z<4;z++) for(int x=0;x<4;x++)
            {
                int gx=chunk.X*4+x,gz=chunk.Z*4+z;
                float px=gx*32+6+Random(gx,gz,101)*20,pz=gz*32+6+Random(gx,gz,102)*20;
                if(ClearingDistance(px,pz)<3 || Math.Abs(px)>448 || Math.Abs(pz)>448 || Slope(px,pz)>.45f) continue;
                result.Add(new Site("resource."+gx+"."+gz,px,pz,(int)(Random(gx,gz,103)*4)));
            }
            return result;
        }
        public List<Site> Decorations(Chunk chunk,int count)
        {
            var result=new List<Site>();
            var resources=Resources(chunk);
            for(int i=0;i<count;i++)
            {
                float x=chunk.X*ChunkSize+Random(chunk.X*4096+i,chunk.Z,701)*ChunkSize;
                float z=chunk.Z*ChunkSize+Random(chunk.X*4096+i,chunk.Z,702)*ChunkSize;
                if(ClearingDistance(x,z)<3 || Math.Abs(x)>495 || Math.Abs(z)>495) continue;
                bool occupied=false;
                foreach(var r in resources) if((r.X-x)*(r.X-x)+(r.Z-z)*(r.Z-z)<16) {occupied=true;break;}
                if(occupied)continue;
                float choice=Random(chunk.X*4096+i,chunk.Z,703);
                int kind=choice<Forest(x,z)*.55f?0:choice>.92f?1:2;
                result.Add(new Site(chunk.Id+".decor."+i,x,z,kind));
            }
            // A sheltered starting camp with four open approaches; protection still stops at 12 m.
            for(int i=0;i<32;i++)
            {
                double angle=i*Math.PI*2/32;
                if(i%8==0 || i%8==1)continue;
                float radius=30+Random(i,0,711)*17;
                float x=(float)Math.Cos(angle)*radius,z=(float)Math.Sin(angle)*radius;
                if(Chunk.At(x,z).Equals(chunk))result.Add(new Site("start.grove."+i,x,z,0));
            }
            return result;
        }
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
            for(int z=-4;z<4;z++)for(int x=-4;x<4;x++)foreach(var site in Resources(new Chunk(x,z)))
                if(!ids.Add(site.Id)||Math.Abs(site.X)>448||Math.Abs(site.Z)>448||Slope(site.X,site.Z)>.45f)
                    throw new InvalidOperationException("Invalid generated resource "+site.Id);
            foreach(var site in Discoveries)
            {
                float length=(float)Math.Sqrt(site.X*site.X+site.Z*site.Z);
                int steps=(int)Math.Ceiling(length/2);
                float previous=Height(0,0);
                for(int n=1;n<=steps;n++)
                {
                    float t=n/(float)steps;
                    float current=Height(site.X*t,site.Z*t);
                    if(Math.Abs(current-previous)/(length/steps)>.8f)
                        throw new InvalidOperationException("Discovery approach is too steep: "+site.Id);
                    previous=current;
                }
            }
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
