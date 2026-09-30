using UnityEngine;

namespace Topaz.Generation
{
    public sealed partial class WildernessPlan
    {
        static readonly Vector2[] AreaGroveCenters={new Vector2(-31,-31),new Vector2(25,17),new Vector2(-10,47),new Vector2(59,-48),new Vector2(-68,47)};
        Vector2 AreaToWorld(Vector2 p)=>new Vector2(p.x*areaCos-p.y*areaSin,p.x*areaSin+p.y*areaCos);
        Vector2 AreaLocal(float x,float z)=>new Vector2(x*areaCos+z*areaSin,-x*areaSin+z*areaCos);
        float RiverCenter(float z)=>Mathf.Sin(z/38+Random(0,0,20072)*3)*7+Mathf.Sin(z/17)*2;
        float AreaRouteHeight(float x,float z)
        {
            float radius=Mathf.Sqrt(x*x+z*z);var p=AreaLocal(x,z);
            if(Settings.areaKind==AreaKind.RiverTerraces)
                return (4+p.y*.006f+2.5f*Smooth((p.x-16)/35))*Smooth(radius/21);
            return Mathf.Min(4,radius*.07f);
        }
        public float BoundaryRadius(float angle)
        {
            float local=angle-Mathf.Atan2(areaSin,areaCos);
            float shape=Settings.areaKind==AreaKind.RiverTerraces?
                .96f+.08f*Mathf.Cos(local*2)+.035f*Mathf.Sin(local*3):
                .94f+.07f*Mathf.Sin(local*3)+.045f*Mathf.Cos(local*2);
            return PlayRadius*shape;
        }
        float BoundaryDistance(float x,float z)=>Mathf.Sqrt(x*x+z*z)-BoundaryRadius(Mathf.Atan2(x,z));
        static bool AreaIntersection(Vector3 a,Vector3 b,Vector3 c,Vector3 d,out Vector3 result)
        {
            var r=new Vector2(b.x-a.x,b.z-a.z);var s=new Vector2(d.x-c.x,d.z-c.z);var q=new Vector2(c.x-a.x,c.z-a.z);
            float Cross(Vector2 u,Vector2 v)=>u.x*v.y-u.y*v.x;
            float denominator=Cross(r,s);result=default;if(Mathf.Abs(denominator)<.0001f)return false;
            float t=Cross(q,s)/denominator,u=Cross(q,r)/denominator;
            if(t<0 || t>1 || u<0 || u>1)return false;
            result=Vector3.Lerp(a,b,t);return true;
        }
        public Vector3 ResourceApproach(Site site)
        {
            var point=new Vector3(site.X,0,site.Z);Vector3 nearest=Vector3.zero;float best=float.MaxValue;
            foreach(var route in routes)for(int i=1;i<route.Points.Count;i++)
            {float distance=Segment(site.X,site.Z,route.Points[i-1],route.Points[i],out float t);if(distance<best){best=distance;nearest=Vector3.Lerp(route.Points[i-1],route.Points[i],t);}}
            nearest.y=0;return point+Vector3.ClampMagnitude(nearest-point,6);
        }
        float AreaGrove(float x,float z)
        {
            var p=AreaLocal(x,z);float grove=0;
            foreach(var center in AreaGroveCenters)
                grove=Mathf.Max(grove,1-Smooth(Vector2.Distance(p,center)/27));
            return Mathf.Lerp(.45f,1,grove)*Mathf.Lerp(.8f,1,Noise(x/28,z/28,20075));
        }
        public float AreaRouteLength
        {
            get{float length=0;foreach(var route in routes)for(int i=1;i<route.Points.Count;i++)length+=Vector3.Distance(route.Points[i-1],route.Points[i]);return length;}
        }
        public System.Collections.Generic.IEnumerable<Vector3> AreaRockFrames()
        {
            var points=Settings.areaKind==AreaKind.ForestBasin?
                new[]{new Vector2(-24,17),new Vector2(-8,35),new Vector2(12,19),new Vector2(-38,-39)}:
                Settings.areaKind==AreaKind.RiverTerraces?
                new[]{new Vector2(22,12),new Vector2(24,25),new Vector2(-18,41),new Vector2(60,50)}:System.Array.Empty<Vector2>();
            foreach(var local in points)
            {
                var p=AreaToWorld(local);
                if(RouteDistance(p.x,p.y)<7 || Reserved(p.x,p.y,3) || NearRequired(p.x,p.y,7))continue;
                yield return new Vector3(p.x,Height(p.x,p.y),p.y);
            }
        }
    }
}
