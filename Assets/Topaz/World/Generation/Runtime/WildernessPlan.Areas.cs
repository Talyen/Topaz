using System;
using System.Collections.Generic;
using UnityEngine;

namespace Topaz.Generation
{
    public sealed partial class WildernessPlan
    {
        public string LogicalId(string local) => Settings.boundedAreas?Settings.areaId+"/"+local:local;
        public float PlayRadius => Extent-38;
        float areaSin,areaCos;
        public Vector3 ExitPosition(AreaConnection exit) => exit.Position(BoundaryRadius(exit.angle)-10);
        public Vector3 ArrivalPosition(AreaConnection exit) => exit.Position(BoundaryRadius(exit.angle)-18);
        public bool AreaBuildable(float x,float z,float radius)
        {
            if(!Settings.boundedAreas)return true;
            if(BoundaryDistance(x,z)+radius> -8)return false;
            foreach(var exit in Settings.areaExits)
                if(Segment(x,z,ArrivalPosition(exit),exit.Position(BoundaryRadius(exit.angle)+12),out _)<radius+6)return false;
            foreach(var site in Discoveries)if(Distance(x,z,site.X,site.Z)<radius+14)return false;
            return true;
        }
        void BuildAreaGeography()
        {
            float phase=Random(0,0,20001)*Mathf.PI*2;areaSin=Mathf.Sin(phase);areaCos=Mathf.Cos(phase);
            float reach=Settings.areaKind==AreaKind.Landmark?82:51;
            Vector2[] layout=Settings.areaKind==AreaKind.ForestBasin?
                new[]{new Vector2(-49,28),new Vector2(34,49),new Vector2(25,-49)}:
                Settings.areaKind==AreaKind.RiverTerraces?
                new[]{new Vector2(-53,-28),new Vector2(36,-42),new Vector2(40,51)}:null;
            for(int i=0;i<3;i++)
            {
                Vector2 p;
                if(layout!=null)p=AreaToWorld(layout[i]+new Vector2((Random(i,0,20002)-.5f)*7,(Random(i,0,20003)-.5f)*7));
                else {float a=phase+i*Mathf.PI*2/3+(Random(i,0,20002)-.5f)*.2f;p=new Vector2(Mathf.Sin(a),Mathf.Cos(a))*(reach+(Random(i,0,20003)-.5f)*10);}
                var site=new Site(LogicalId("beat."+i),p.x,p.y,i);
                discoveries.Add(site);reservations.Add(new Reservation(site.Id,p.x,p.y,12));
            }
            reservations.Add(new Reservation(LogicalId("arrival.home"),0,0,10));
            Vector3 World(float x,float z){var p=AreaToWorld(new Vector2(x,z));return new Vector3(p.x,0,p.y);}
            void RouteTo(string id,string destination,Vector3 from,Vector3 to,uint stream,params Vector3[] via)
            {
                var anchors=new List<Vector3>{from};anchors.AddRange(via);anchors.Add(to);
                var points=new List<Vector3>();
                for(int leg=1;leg<anchors.Count;leg++)
                {
                    var a=anchors[leg-1];var b=anchors[leg];int steps=Math.Max(3,(int)Math.Ceiling(Vector3.Distance(a,b)/7));
                    var side=new Vector3(-(b.z-a.z),0,b.x-a.x).normalized;
                    float bend=(Random(leg,(int)to.z,stream)-.5f)*Settings.trailMeander*.55f;
                    for(int i=leg==1?0:1;i<=steps;i++)
                    {float t=i/(float)steps;var p=Vector3.Lerp(a,b,t)+side*(Mathf.Sin(t*Mathf.PI)*bend);p.y=AreaRouteHeight(p.x,p.z);points.Add(p);}
                }
                routes.Add(new Route(LogicalId(id),destination,points));
            }
            Vector3 Point(Site s)=>new Vector3(s.X,0,s.Z);
            if(Settings.areaKind==AreaKind.ForestBasin)
            {
                RouteTo("approach.0",Discoveries[0].Id,Vector3.zero,Point(Discoveries[0]),20010,World(-24,-7),World(-49,4));
                RouteTo("approach.1",Discoveries[1].Id,Point(Discoveries[0]),Point(Discoveries[1]),20011,World(-42,56),World(3,65));
                RouteTo("approach.2",Discoveries[2].Id,Point(Discoveries[1]),Point(Discoveries[2]),20012,World(63,27),World(62,-25));
                RouteTo("loop",Discoveries[0].Id,Point(Discoveries[2]),Point(Discoveries[0]),20014,World(-17,-58),World(-63,-23));
            }
            else if(Settings.areaKind==AreaKind.RiverTerraces)
            {
                RouteTo("approach.0",Discoveries[0].Id,Vector3.zero,Point(Discoveries[0]),20010,World(-25,-9));
                RouteTo("approach.1",Discoveries[1].Id,Point(Discoveries[0]),Point(Discoveries[1]),20011,World(-31,-51),World(9,-55));
                RouteTo("approach.2",Discoveries[2].Id,Point(Discoveries[1]),Point(Discoveries[2]),20012,World(65,-6),World(62,31));
                RouteTo("loop",Discoveries[0].Id,Point(Discoveries[2]),Point(Discoveries[0]),20014,World(14,58),World(-25,49),World(-65,12));
            }
            else
            {
                for(int i=0;i<3;i++)RouteTo("approach."+i,Discoveries[i].Id,i==0?Vector3.zero:Point(Discoveries[i-1]),Point(Discoveries[i]),20010+(uint)i);
                RouteTo("loop",Discoveries[0].Id,Point(Discoveries[2]),Point(Discoveries[0]),20014);
            }
            foreach(var exit in Settings.areaExits)
            {
                var end=ExitPosition(exit);Site nearest=Discoveries[0];float best=float.MaxValue;
                foreach(var beat in Discoveries){float d=Vector3.SqrMagnitude(Point(beat)-end);if(d<best){best=d;nearest=beat;}}
                RouteTo("exit."+exit.id,exit.id,Point(nearest),end,20020);
                reservations.Add(new Reservation(exit.id,end.x,end.z,12));
                var arrival=ArrivalPosition(exit);reservations.Add(new Reservation(exit.id+".arrival",arrival.x,arrival.z,9));
            }
            if(Settings.areaKind==AreaKind.RiverTerraces || Settings.areaKind==AreaKind.Lakeshore)
            {
                var points=new List<Vector3>();
                for(int i=0;i<=32;i++)
                {
                    float z=Mathf.Lerp(-Extent,Extent,i/32f),x=Settings.areaKind==AreaKind.Lakeshore?PlayRadius+5:RiverCenter(z);
                    var p=AreaToWorld(new Vector2(x,z));points.Add(new Vector3(p.x,4+z*.006f,p.y));
                }
                var river=new River(LogicalId("river"),points,Settings.areaKind==AreaKind.Lakeshore?18:4.5f);rivers.Add(river);
                foreach(var route in routes)for(int n=1;n<route.Points.Count;n++)for(int r=1;r<points.Count;r++)
                {
                    if(!AreaIntersection(route.Points[n-1],route.Points[n],points[r-1],points[r],out var p))continue;
                    if(crossings.Exists(c=>(c.Position-p).sqrMagnitude<18*18))continue;
                    RiverDistance(p.x,p.z,out float surface,out _);p.y=surface+.12f;
                    var forward=route.Points[n]-route.Points[n-1];forward.y=0;
                    crossings.Add(new RiverCrossing(LogicalId("crossing."+crossings.Count),p,forward.normalized,crossings.Count==0 && Settings.areaKind==AreaKind.RiverTerraces));
                }
            }
            if(Settings.areaKind==AreaKind.RuinedSettlement || Settings.areaKind==AreaKind.Landmark)
            {
                var beat=Discoveries[1];DestinationKind kind=Settings.areaKind==AreaKind.Landmark?Settings.areaLandmark:DestinationKind.RuinArch;
                // The monumental prefab is fitted to a reserved precinct; summit scenery does not create an extra world-scale mountain.
                float radius=kind==DestinationKind.TitansGrave?38:kind==DestinationKind.SplitPeak?18:9;
                destinations.Add(new Destination(LogicalId("destination"),kind,beat.X,beat.Z,phase*Mathf.Rad2Deg,radius,0));
                RouteTo("destination.approach",LogicalId("destination"),Point(Discoveries[0]),Point(beat),20031);
                reservations.Add(new Reservation(LogicalId("destination"),beat.X,beat.Z,radius+5));
            }
            // Fixed candidate order and an explicit bounded fallback keep scarce nodes independent of visual density.
            for(int i=0;i<16;i++)
            {
                bool placed=false;
                for(int candidate=0;candidate<512;candidate++)
                {
                    float a=Random(i,candidate,20040)*Mathf.PI*2,r=Mathf.Lerp(22,PlayRadius-22,Random(i,candidate,20041));
                    float x=Mathf.Sin(a)*r,z=Mathf.Cos(a)*r;
                    if(Reserved(x,z,3) || NearRequired(x,z,6) || WaterDepthAnalytic(x,z)>.2f || AreaSlope(x,z)>.28f || BoundaryDistance(x,z)>-17)continue;
                    requiredResources.Add(new Site(LogicalId("starter.resource."+i),x,z,i%2==0?1:0));placed=true;break;
                }
                if(!placed)throw new InvalidOperationException("Area resource placement exhausted bounded candidates: "+Settings.areaId+"/"+i);
            }
        }
        float WaterDepthAnalytic(float x,float z)
        {float d=RiverDistance(x,z,out _,out float width);return d<width?1:0;}
        float AreaSlope(float x,float z)
        {float dx=(AreaHeight(x+1,z)-AreaHeight(x-1,z))*.5f,dz=(AreaHeight(x,z+1)-AreaHeight(x,z-1))*.5f;return Mathf.Sqrt(dx*dx+dz*dz);}
        float AreaHeight(float x,float z)
        {
            float radius=Distance(x,z,0,0);
            float baseHeight=AreaRouteHeight(x,z);
            float rough=(Noise(x/32,z/32,20060)-.5f)*Settings.localRelief;
            float hill=Settings.areaKind==AreaKind.HighlandPass?8:Settings.areaKind==AreaKind.ForestBasin?5:3;
            float height=baseHeight+(2+rough)*Smooth((radius-18)/22)+hill*Smooth((Noise(x/75,z/75,20061)-.35f)/.4f);
            var local=AreaLocal(x,z);
            if(Settings.areaKind==AreaKind.ForestBasin)height+=10*(1-Smooth(Vector2.Distance(local,new Vector2(-7,17))/27));
            if(Settings.areaKind==AreaKind.RiverTerraces)height+=3*Smooth((local.x-17)/15)+2*Smooth((Mathf.Abs(local.y)-32)/16);
            height*=Smooth((radius-9)/18);
            float nearest=float.MaxValue,weighted=0,total=0;
            foreach(var route in routes)for(int i=1;i<route.Points.Count;i++)
            {float d=Segment(x,z,route.Points[i-1],route.Points[i],out float t);nearest=Math.Min(nearest,d);float w=1-Smooth(d/22);if(w>0){w*=w;weighted+=Mathf.Lerp(route.Points[i-1].y,route.Points[i].y,t)*w;total+=w;}}
            if(total>0)height=Mathf.Lerp(height,weighted/total,1-Smooth((nearest-5)/15));
            foreach(var beat in Discoveries)height=Mathf.Lerp(height,AreaRouteHeight(beat.X,beat.Z),1-Smooth((Distance(x,z,beat.X,beat.Z)-14)/12));
            foreach(var destination in destinations)height=Mathf.Lerp(height,4,1-Smooth((Distance(x,z,destination.X,destination.Z)-destination.Radius)/12));
            height=CarveRiver(x,z,height);
            // A permanent, visible enclosing landform; selected paths lead into it at explicit thresholds.
            float edge=Smooth(BoundaryDistance(x,z)/10);
            return Math.Max(0,height)+edge*(12+Noise(x/28,z/28,20062)*7);
        }
    }
}
