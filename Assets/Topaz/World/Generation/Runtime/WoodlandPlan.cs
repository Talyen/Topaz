using System;
using System.Collections.Generic;

namespace Topaz.Generation
{
    [Serializable]
    public sealed class WoodlandSettings
    {
        public int version = 1;
        public int resolution = 129;
        public float size = 128f;
        public float relief = 9f;
        public int decorationCount = 170;
        public WoodlandSettings Copy() => (WoodlandSettings)MemberwiseClone();
        public void Validate()
        {
            if (version != WoodlandPlan.Version) throw new ArgumentException("Unsupported woodland generator version.");
            if (resolution < 33 || resolution > 513 || ((resolution - 1) & (resolution - 2)) != 0 ||
                !Finite(size) || size < 96 || size > 256 || !Finite(relief) || relief < 0 || relief > 12 ||
                decorationCount < 0 || decorationCount > 1000)
                throw new ArgumentException("Invalid woodland generation settings.");
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>Pure data; neither creation order nor Unity's global Random affects a world.</summary>
    public sealed class WoodlandPlan
    {
        public const int Version = 1;
        public readonly int Seed;
        public readonly string Region;
        public readonly WoodlandSettings Settings;
        public readonly float[,] Heights;
        public readonly List<Placement> Decorations = new List<Placement>();
        public readonly List<Placement> Resources = new List<Placement>();
        public readonly Point[] Route;
        public readonly Point Center;
        public readonly Point Entry;
        public readonly Point Exit;
        public readonly Point Encounter;

        public readonly struct Point
        {
            public readonly float x, z;
            public Point(float x, float z) { this.x = x; this.z = z; }
        }
        public readonly struct Placement
        {
            public readonly string id;
            public readonly Point point;
            public readonly float scale;
            public readonly bool rock;
            public Placement(string id, Point point, float scale, bool rock)
            { this.id = id; this.point = point; this.scale = scale; this.rock = rock; }
        }

        WoodlandPlan(int seed, string region, WoodlandSettings settings)
        {
            Seed = seed; Region = region; Settings = settings.Copy();
            // Home stays at local origin so the existing homestead grid remains meaningful.
            bool home = region == "home";
            var layout = new Stream(Mix((uint)seed, home ? 17u : 29u));
            Center = new Point(0, 0);
            Entry = new Point(0, -settings.size * .36f);
            Exit = new Point((layout.Next() - .5f) * 28, settings.size * .36f);
            Encounter = new Point((layout.Next() - .5f) * 24, 24);
            Route = new[] { Entry, Center, Encounter, Exit };
            // Gameplay placement is independent of decoration density and rejection count.
            var resources = new Stream(Mix((uint)seed, home ? 131u : 137u));
            for (int i = 0; i < 16; i++)
            {
                double angle = i * Math.PI * 2 / 16;
                float radius = (home ? 28 : 22) + resources.Next() * 5;
                var p = new Point((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
                for (int attempt=0;attempt<12 && (DistanceToRoute(p)<10 || Resources.Exists(r=>Distance(p,r.point)<10));attempt++)
                    p=new Point(p.x+(p.x<0?-8:8),p.z);
                Resources.Add(new Placement(region + ".resource." + i, p, 1, i % 4 == 0));
            }
            float BaseHeight(float px,float pz)
            {
                float n=Noise(px/27,pz/27,(uint)seed)*.72f+Noise(px/11,pz/11,(uint)seed+13)*.28f;
                float routeDistance=DistanceToRoute(new Point(px,pz));
                float clearing=(float)Math.Sqrt(px*px+pz*pz)-(home?18:9);
                return settings.relief*n*Smooth(Math.Min(routeDistance-4,clearing)/10);
            }
            Heights = new float[settings.resolution, settings.resolution];
            for (int z = 0; z < settings.resolution; z++)
            for (int x = 0; x < settings.resolution; x++)
            {
                float px = x * settings.size / (settings.resolution - 1) - settings.size / 2;
                float pz = z * settings.size / (settings.resolution - 1) - settings.size / 2;
                float height=BaseHeight(px,pz);
                foreach(var resource in Resources)
                {
                    float distance=Distance(new Point(px,pz),resource.point);
                    if(distance>=5)continue;
                    float blend=Smooth((distance-3)/2);
                    height=BaseHeight(resource.point.x,resource.point.z)*(1-blend)+height*blend;
                }
                Heights[z, x] = height;
            }
            var decor = new Stream(Mix((uint)seed, home ? 701u : 709u));
            int attempts = 0;
            while (Decorations.Count < settings.decorationCount && attempts++ < settings.decorationCount * 30)
            {
                var p = new Point((decor.Next() - .5f) * (settings.size - 8), (decor.Next() - .5f) * (settings.size - 8));
                if (DistanceToRoute(p) < 6 || Distance(p, Center) < (home ? 22 : 12) ||
                    Resources.Exists(r => Distance(p, r.point) < 3) || Decorations.Exists(r => Distance(p, r.point) < 3)) continue;
                Decorations.Add(new Placement(region + ".decor." + Decorations.Count, p, .8f + decor.Next() * .8f, decor.Next() < .2f));
            }
        }

        public static WoodlandPlan Generate(int seed, string region, WoodlandSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.Validate();
            if (region != "home" && region != "expedition.clearing") throw new ArgumentException("Unknown woodland region.");
            var plan = new WoodlandPlan(seed, region, settings);
            plan.Validate();
            return plan;
        }
        public void Validate()
        {
            var ids = new HashSet<string>();
            foreach (var p in Resources)
                if (!ids.Add(p.id) || Math.Abs(p.point.x) > Settings.size / 2 - 2 || Math.Abs(p.point.z) > Settings.size / 2 - 2)
                    throw new InvalidOperationException("Invalid resource placement: " + p.id);
            // Continuous route samples catch slope/height regressions after shaping changes.
            for (int i = 1; i < Route.Length; i++)
            for (int j = 0; j <= 100; j++)
            {
                float t = j / 100f;
                float x = Route[i - 1].x * (1 - t) + Route[i].x * t;
                float z = Route[i - 1].z * (1 - t) + Route[i].z * t;
                if (Height(x, z) > .15f) throw new InvalidOperationException("Required route is not traversable.");
            }
        }
        public float Height(float x, float z)
        {
            float fx = Math.Clamp((x / Settings.size + .5f) * (Settings.resolution - 1), 0, Settings.resolution - 1);
            float fz = Math.Clamp((z / Settings.size + .5f) * (Settings.resolution - 1), 0, Settings.resolution - 1);
            int ix = (int)fx, iz = (int)fz, nx = Math.Min(ix + 1, Settings.resolution - 1), nz = Math.Min(iz + 1, Settings.resolution - 1);
            float a = Heights[iz, ix] * (1 - (fx - ix)) + Heights[iz, nx] * (fx - ix);
            float b = Heights[nz, ix] * (1 - (fx - ix)) + Heights[nz, nx] * (fx - ix);
            return a * (1 - (fz - iz)) + b * (fz - iz);
        }
        public float DistanceToRoute(Point p)
        {
            float result = float.MaxValue;
            for (int i = 1; i < Route.Length; i++)
            {
                Point a = Route[i - 1], b = Route[i];
                float dx = b.x - a.x, dz = b.z - a.z;
                float t = Math.Clamp(((p.x - a.x) * dx + (p.z - a.z) * dz) / (dx * dx + dz * dz), 0, 1);
                result = Math.Min(result, Distance(p, new Point(a.x + t * dx, a.z + t * dz)));
            }
            return result;
        }
        static float Distance(Point a, Point b) => (float)Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
        static float Smooth(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
        static uint Mix(uint a, uint b) { unchecked { uint v = a ^ (b * 0x9e3779b9u); v ^= v >> 16; v *= 0x7feb352du; v ^= v >> 15; v *= 0x846ca68bu; return v ^ (v >> 16); } }
        static float Noise(float x, float z, uint seed)
        {
            int ix = (int)Math.Floor(x), iz = (int)Math.Floor(z);
            float tx = Smooth(x - ix), tz = Smooth(z - iz);
            float Value(int a, int b) => (Mix(Mix(seed, unchecked((uint)a)), unchecked((uint)b)) & 0xffffff) / 16777215f;
            return (Value(ix, iz) * (1 - tx) + Value(ix + 1, iz) * tx) * (1 - tz) +
                (Value(ix, iz + 1) * (1 - tx) + Value(ix + 1, iz + 1) * tx) * tz;
        }
        struct Stream
        {
            uint state;
            public Stream(uint seed) { state = seed == 0 ? 1 : seed; }
            public float Next() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return (state & 0xffffff) / 16777216f; }
        }
    }
}
