using System;
using System.Collections.Generic;
using UnityEngine;

namespace Topaz.Generation
{
    public enum DestinationKind
    {
        CliffArch, Lookout, Cabin, Jetty, Fossil, Canoe,
        Beacon, Cairn, Pyre, WreckedBoat, Dock, Crane, Statue,
        GoblinGate, MineEntrance, MineTower, TreeHouse, Effigy, Shrine, RuinArch,
        TitansGrave, SplitPeak
    }

    public sealed partial class WildernessPlan
    {
        public readonly struct Destination
        {
            public readonly string Id;
            public readonly DestinationKind Kind;
            public readonly float X, Z, Yaw, Radius, Elevation;
            public Chunk Owner => Chunk.At(X, Z);
            public Destination(string id, DestinationKind kind, float x, float z, float yaw, float radius, float elevation)
            { Id=id; Kind=kind; X=x; Z=z; Yaw=yaw; Radius=radius; Elevation=elevation; }
        }

        readonly List<Destination> destinations = new List<Destination>();
        public IReadOnlyList<Destination> Destinations => destinations.AsReadOnly();

        static float Footprint(DestinationKind kind) => kind switch
        {
            DestinationKind.TitansGrave => 38,
            DestinationKind.SplitPeak => 12,
            DestinationKind.CliffArch or DestinationKind.MineEntrance or DestinationKind.GoblinGate => 11,
            DestinationKind.Cabin or DestinationKind.Dock or DestinationKind.MineTower or DestinationKind.TreeHouse or DestinationKind.Shrine => 9,
            DestinationKind.Lookout or DestinationKind.Beacon or DestinationKind.WreckedBoat or DestinationKind.Crane or DestinationKind.RuinArch => 7,
            _ => 4
        };

        static bool Shore(DestinationKind kind) => kind is DestinationKind.Jetty or DestinationKind.Canoe or
            DestinationKind.WreckedBoat or DestinationKind.Dock;

        static bool Highland(DestinationKind kind) => kind is DestinationKind.CliffArch or DestinationKind.Lookout or
            DestinationKind.Fossil or DestinationKind.Beacon or DestinationKind.Statue or DestinationKind.MineEntrance or
            DestinationKind.MineTower or DestinationKind.TitansGrave or DestinationKind.SplitPeak;

        Vector3 NearestPrimaryRoute(float x, float z, int primaryCount)
        {
            float best=float.MaxValue;Vector3 nearest=default;
            for(int r=0;r<primaryCount;r++)
            {
                var points=routes[r].Points;
                for(int i=1;i<points.Count;i++)
                {
                    float d=Segment(x,z,points[i-1],points[i],out float t);
                    if(d>=best)continue;
                    best=d;nearest=Vector3.Lerp(points[i-1],points[i],t);
                }
            }
            return nearest;
        }

        bool FitsDestination(DestinationKind kind, float x, float z, float radius, Vector3 trail)
        {
            float influence=kind==DestinationKind.SplitPeak?200:radius;
            if(Math.Abs(x)>Extent-influence-24 || Math.Abs(z)>Extent-influence-24)return false;
            if(Distance(x,z,0,0)<(kind==DestinationKind.TitansGrave || kind==DestinationKind.SplitPeak?Settings.worldSize*.2f:95))return false;
            float routeDistance=Distance(x,z,trail.x,trail.z);
            float minimumRoute=kind==DestinationKind.SplitPeak?230:radius+7;
            if(routeDistance<minimumRoute || routeDistance>(kind==DestinationKind.SplitPeak?360:kind==DestinationKind.TitansGrave?100:65))return false;
            if(kind!=DestinationKind.SplitPeak &&
                Math.Abs(AnalyticHeight(x,z,false)-trail.y)/routeDistance>.18f)return false;
            foreach(var reservation in reservations)
                if(Distance(x,z,reservation.X,reservation.Z)<influence+reservation.Radius+7)return false;
            foreach(var resource in requiredResources)
                if(Distance(x,z,resource.X,resource.Z)<influence+8)return false;
            foreach(var prior in destinations)
                if(Distance(x,z,prior.X,prior.Z)<radius+prior.Radius+(radius>20 || prior.Radius>20?120:25))return false;
            if(kind!=DestinationKind.SplitPeak)
            {
                float center=AnalyticHeight(x,z,false);
                float reach=Math.Min(radius*.65f,8);
                foreach(var offset in new[]{new Vector2(reach,0),new Vector2(-reach,0),new Vector2(0,reach),new Vector2(0,-reach)})
                    if(Math.Abs(AnalyticHeight(x+offset.x,z+offset.y,false)-center)>(kind==DestinationKind.TitansGrave?5:2.2f))return false;
            }
            if(kind==DestinationKind.SplitPeak && AnalyticHeight(x,z,false)>115)return false;
            if(Highland(kind) && kind!=DestinationKind.SplitPeak && RawHeight(x,z)<12)return false;
            if(kind==DestinationKind.TreeHouse && ForestIntent(x,z)<.35f)return false;
            for(int i=1;i<=12;i++)
            {
                float t=i/13f,px=Mathf.Lerp(trail.x,x,t),pz=Mathf.Lerp(trail.z,z,t);
                float water=WaterSurface(px,pz);
                if(!float.IsNaN(water) && water-AnalyticHeight(px,pz,false)>.35f)return false;
                if(i>1 && Math.Abs(AnalyticHeight(px,pz,false)-AnalyticHeight(Mathf.Lerp(trail.x,x,(i-1)/13f),Mathf.Lerp(trail.z,z,(i-1)/13f),false))>3.5f)
                    return false;
            }
            return true;
        }

        Vector2 Candidate(DestinationKind kind, int attempt, uint stream)
        {
            float u=Random(attempt,(int)kind,stream),v=Random(attempt,(int)kind,stream+1);
            if(kind==DestinationKind.TitansGrave || kind==DestinationKind.SplitPeak)
            {
                float angle=u*Mathf.PI*2;
                float reach=Settings.worldSize*(kind==DestinationKind.TitansGrave?Mathf.Lerp(.22f,.36f,v):Mathf.Lerp(.31f,.43f,v));
                return new Vector2(Mathf.Cos(angle)*reach,Mathf.Sin(angle)*reach);
            }
            if(Shore(kind))
            {
                if(attempt%3!=0 && waters.Count>0)
                {
                    var pond=waters[attempt%waters.Count];float angle=u*Mathf.PI*2;
                    float distance=pond.Radius+4+v*5;
                    return new Vector2(pond.X+Mathf.Cos(angle)*distance,pond.Z+Mathf.Sin(angle)*distance);
                }
                var river=rivers[0];int segment=Math.Min(river.Points.Count-2,(int)(u*(river.Points.Count-1)));
                var a=river.Points[segment];var b=river.Points[segment+1];
                var tangent=new Vector2(b.x-a.x,b.z-a.z).normalized;
                var side=new Vector2(-tangent.y,tangent.x)*(attempt%2==0?1:-1);
                return new Vector2(Mathf.Lerp(a.x,b.x,v),Mathf.Lerp(a.z,b.z,v))+side*(river.HalfWidth+3);
            }
            float limit=Extent-100;
            return new Vector2((u*2-1)*limit,(v*2-1)*limit);
        }

        float ShoreYaw(float x,float z)
        {
            float best=float.MaxValue;Vector2 water=default;
            foreach(var pond in waters)
            {
                float distance=Distance(x,z,pond.X,pond.Z);
                float edge=Math.Abs(distance-pond.Radius);
                if(edge>=best)continue;
                best=edge;water=new Vector2(pond.X,pond.Z);
            }
            foreach(var river in rivers)
                for(int i=1;i<river.Points.Count;i++)
                {
                    float distance=Segment(x,z,river.Points[i-1],river.Points[i],out float t);
                    float edge=Math.Abs(distance-river.HalfWidth);
                    if(edge>=best)continue;
                    best=edge;
                    water=Vector2.Lerp(new Vector2(river.Points[i-1].x,river.Points[i-1].z),
                        new Vector2(river.Points[i].x,river.Points[i].z),t);
                }
            var direction=(water-new Vector2(x,z)).normalized;
            return Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg;
        }

        bool TryDestination(DestinationKind kind, int primaryCount, int attempts, uint stream)
        {
            float radius=Footprint(kind);
            var candidates=new List<(float Score,Vector2 Position,Vector3 Approach)>();
            for(int i=0;i<attempts;i++)
            {
                var candidate=Candidate(kind,i,stream);var trail=NearestPrimaryRoute(candidate.x,candidate.y,primaryCount);
                if(!FitsDestination(kind,candidate.x,candidate.y,radius,trail))continue;
                float routeDistance=Distance(candidate.x,candidate.y,trail.x,trail.z);
                float score=Random(i,(int)kind,stream+2)*.35f +
                    Mathf.Clamp01(AnalyticHeight(candidate.x,candidate.y)/70)*.3f -
                    Mathf.Abs(routeDistance-(radius+24))*.005f;
                if(Highland(kind))score+=Mathf.Clamp01(RawHeight(candidate.x,candidate.y)/90)*.45f;
                candidates.Add((score,candidate,trail));
            }
            candidates.Sort((a,b)=>
            {
                int order=b.Score.CompareTo(a.Score);
                return order!=0?order:a.Position.x!=b.Position.x?a.Position.x.CompareTo(b.Position.x):a.Position.y.CompareTo(b.Position.y);
            });
            foreach(var candidate in candidates)
            {
                CommitDestination(kind,radius,candidate.Position,candidate.Approach);
                if(kind!=DestinationKind.SplitPeak || GentleFinalApproach(routes[routes.Count-1]))return true;
                routes.RemoveAt(routes.Count-1);
                reservations.RemoveAt(reservations.Count-1);
                destinations.RemoveAt(destinations.Count-1);
            }
            return false;
        }

        void CommitDestination(DestinationKind kind,float radius,Vector2 position,Vector3 approach)
        {
            float yaw=Mathf.Atan2(approach.x-position.x,approach.z-position.y)*Mathf.Rad2Deg;
            if(Shore(kind))yaw=ShoreYaw(position.x,position.y);
            float baseHeight=AnalyticHeight(position.x,position.y,false);
            float gain=kind==DestinationKind.SplitPeak?Mathf.Clamp(155-baseHeight,60,105):0;
            string id="destination."+kind.ToString().ToLowerInvariant();
            destinations.Add(new Destination(id,kind,position.x,position.y,yaw,radius,gain));
            reservations.Add(new Reservation(id,position.x,position.y,kind==DestinationKind.SplitPeak?200:radius+7));
            var points=new List<Vector3>{approach};
            if(kind==DestinationKind.SplitPeak)
            {
                Vector2 from=new Vector2(approach.x-position.x,approach.z-position.y).normalized;
                float start=Mathf.Atan2(from.y,from.x);
                float outerX=position.x+Mathf.Cos(start)*165,outerZ=position.y+Mathf.Sin(start)*165;
                float outerHeight=AnalyticHeight(outerX,outerZ),summitHeight=AnalyticHeight(position.x,position.y);
                for(int i=0;i<=48;i++)
                {
                    float t=i/48f,angle=start+t*Mathf.PI*4,radiusAt=Mathf.Lerp(165,10,t);
                    float px=position.x+Mathf.Cos(angle)*radiusAt,pz=position.y+Mathf.Sin(angle)*radiusAt;
                    points.Add(new Vector3(px,Mathf.Lerp(outerHeight,summitHeight,Mathf.SmoothStep(0,1,t)),pz));
                }
                points.Add(new Vector3(position.x,summitHeight,position.y));
            }
            else
            {
                var middle=Vector2.Lerp(new Vector2(approach.x,approach.z),position,.5f);
                points.Add(new Vector3(middle.x,Mathf.Lerp(approach.y,baseHeight,.5f),middle.y));
                points.Add(new Vector3(position.x,baseHeight,position.y));
            }
            routes.Add(new Route("route."+id,id,points));
        }

        bool GentleFinalApproach(Route route)
        {
            for(int i=1;i<route.Points.Count;i++)
            {
                var a=route.Points[i-1];var b=route.Points[i];
                int steps=Math.Max(1,(int)Math.Ceiling(Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z))/3));
                var side=new Vector2(-(b.z-a.z),b.x-a.x).normalized;
                for(int n=0;n<=steps;n++)for(int lane=-1;lane<=1;lane++)
                {
                    float t=n/(float)steps;
                    float x=Mathf.Lerp(a.x,b.x,t)+side.x*lane*2,z=Mathf.Lerp(a.z,b.z,t)+side.y*lane*2;
                    float dx=(AnalyticHeight(x+1,z)-AnalyticHeight(x-1,z))*.5f;
                    float dz=(AnalyticHeight(x,z+1)-AnalyticHeight(x,z-1))*.5f;
                    if(dx*dx+dz*dz>.25f)return false;
                    float water=WaterSurface(x,z);
                    if(!float.IsNaN(water) && water-AnalyticHeight(x,z)>.5f)return false;
                }
            }
            return true;
        }

        void BuildDestinations()
        {
            int primaryCount=routes.Count;
            if(Settings.worldSize==2048)
            {
                if(!TryDestination(DestinationKind.TitansGrave,primaryCount,768,12001))
                    throw new InvalidOperationException($"Seed {Seed}: no suitable Titan's Grave.");
                if(!TryDestination(DestinationKind.SplitPeak,primaryCount,768,12101))
                    throw new InvalidOperationException($"Seed {Seed}: no suitable Split Peak.");
            }
            var kinds=new List<DestinationKind>();
            for(int i=0;i<(int)DestinationKind.TitansGrave;i++)kinds.Add((DestinationKind)i);
            kinds.Sort((a,b)=>Random((int)a,0,12201).CompareTo(Random((int)b,0,12201)));
            int budget=Math.Max(4,Mathf.RoundToInt(16f*Settings.worldSize*Settings.worldSize/(2048f*2048f)));
            foreach(var kind in kinds)
            {
                if(budget==0)break;
                if(TryDestination(kind,primaryCount,256,(uint)(12300+(int)kind*4)))budget--;
            }
        }

        float PeakRise(float x,float z)
        {
            foreach(var site in destinations)
            {
                if(site.Kind!=DestinationKind.SplitPeak)continue;
                float d=Distance(x,z,site.X,site.Z);
                if(d>=190)return 0;
                return site.Elevation*Mathf.Clamp01((190-d)/175);
            }
            return 0;
        }
    }
}
