using UnityEngine;

namespace Topaz.Generation
{
    public sealed partial class WildernessPlan
    {
        // World-space fields keep habitat and trail transitions continuous across streaming boundaries.
        public float Canopy(float x,float z)
        {
            float grove=Noise(x/Settings.groveScale,z/Settings.groveScale,1103);
            float coverage=Smooth((grove-(1-Settings.groveCoverage))/.3f);
            var biome=Biomes(x,z);
            return Mathf.Lerp(.62f,1,biome.Woodland)*(1-biome.Highland*.55f)*Mathf.Lerp(.72f,1,coverage);
        }
        public float TrailHalfWidth(float x,float z) => Mathf.Lerp(Settings.trailMinimumHalfWidth,
            Settings.trailMaximumHalfWidth,Noise(x/38,z/38,1102));

        public float LandmarkApproach(float x,float z)
        {
            float visibility=0;
            for(int i=0;i<Discoveries.Count;i++)
            {
                var site=Discoveries[i];
                if(Distance(x,z,site.X,site.Z)>Settings.landmarkApproachLength+Settings.landmarkApproachWidth)continue;
                var points=routes[i].Points;var end=points[points.Count-1];
                float remaining=Settings.landmarkApproachLength;
                for(int n=points.Count-2;n>=0 && remaining>0;n--)
                {
                    var start=points[n];float length=Vector3.Distance(start,end);
                    if(length>remaining)start=Vector3.Lerp(end,start,remaining/length);
                    float distance=Segment(x,z,start,end,out float t);
                    float width=Mathf.Lerp(Settings.landmarkApproachWidth*.55f,Settings.landmarkApproachWidth,t);
                    visibility=Mathf.Max(visibility,1-Smooth(distance/width));
                    remaining-=length;end=start;
                }
            }
            return visibility;
        }
        /// <summary>Expected instances per 2 m detail cell: low grass, upright grass, shade grass, flowers.</summary>
        public Vector4 GroundCover(float x,float z)
        {
            if(WaterDepth(x,z)>0)return Vector4.zero;
            float slope=Slope(x,z),canopy=Canopy(x,z),moisture=Moisture(x,z);
            float trail=Smooth((RouteDistance(x,z)-TrailHalfWidth(x,z)-.15f)/1.4f);
            float clearing=Smooth((Distance(x,z,0,0)-5)/3);
            foreach(var site in Discoveries)clearing*=Smooth((Distance(x,z,site.X,site.Z)-2)/3);
            foreach(var site in destinations)clearing*=Smooth((Distance(x,z,site.X,site.Z)-site.Radius)/5);
            float patch=Smooth((Noise(x/17,z/17,1106)-.28f)/.42f);
            float quiet=Mathf.Lerp(.75f,1,Noise(x/43,z/43,1107));
            float mask=trail*clearing*(1-Smooth((slope-.22f)/.3f))*quiet*Settings.groundCoverDensity;
            float approach=1-LandmarkApproach(x,z)*.85f;
            return new Vector4((1-canopy*.25f)*Mathf.Lerp(13,24,patch),
                Mathf.Lerp(.35f,1,1-canopy)*patch*patch*5f*approach,
                canopy*moisture*patch*7f*approach,
                (1-canopy)*Smooth((Noise(x/24,z/24,1108)-.60f)/.25f)*.23f*approach)*mask;
        }
        public float TreeScale(float x,float z,float variation) => Mathf.Lerp(1.05f,1.75f,Canopy(x,z))*Mathf.Lerp(.9f,1.12f,variation);
    }
}
