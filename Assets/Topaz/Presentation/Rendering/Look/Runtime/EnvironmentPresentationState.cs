using UnityEngine;
namespace Topaz.Rendering
{
    /// <summary>Derived presentation only. WorldSession owns elapsed time and weather.</summary>
    public readonly struct EnvironmentPresentationState
    {
        public readonly float Daylight,Cloudiness,Rain,Wetness;
        public readonly double WorldHours;
        public readonly float SunIntensity,MoonIntensity,Moisture,WaterDepth;
        public readonly Vector3 Biomes;
        public readonly Color Sun,AmbientSky,AmbientEquator,AmbientGround;
        public readonly Vector4 Wind,PreviousWind;
        // Artist-facing sRGB palette; shader adapters upload linear colors explicitly.
        public readonly Color Sky,Horizon,Fog;
        public readonly Vector4 FogParameters;
        public EnvironmentPresentationState(double hours,float clouds,float rain,float wetness,Vector4 wind,Vector4 previousWind,float fogDistance,Vector3 biomes,float moisture,float waterDepth)
            :this(hours,clouds,rain,wetness,wind,previousWind,fogDistance,biomes,moisture,waterDepth,false){}
        public EnvironmentPresentationState(double hours,float clouds,float rain,float wetness,Vector4 wind,Vector4 previousWind,float fogDistance,Vector3 biomes,float moisture,float waterDepth,bool golden)
        {
            WorldHours=hours;Biomes=biomes;Moisture=moisture;WaterDepth=waterDepth;
            float elevation=Mathf.Sin(((float)(hours%24)-6)/24*Mathf.PI*2);
            Daylight=Mathf.SmoothStep(0,1,Mathf.Clamp01(elevation*2.2f));
            SunIntensity=(golden?2.1f:2.4f)*Daylight*Mathf.Lerp(1,.5f,clouds);MoonIntensity=Mathf.Lerp(golden?1.1f:.95f,0,Daylight);
            Sun=Color.Lerp(new Color(1,.78f,.57f),golden?new Color(1,.94f,.82f):Color.white,Mathf.Clamp01(elevation*2));
            AmbientSky=Color.Lerp(new Color(.55f,.64f,.78f),golden?new Color(.76f,.84f,.96f):new Color(.70f,.81f,.95f),Daylight);
            AmbientEquator=Color.Lerp(new Color(.40f,.48f,.59f),golden?new Color(.57f,.65f,.65f):new Color(.53f,.62f,.62f),Daylight);
            AmbientGround=Color.Lerp(new Color(.27f,.32f,.40f),golden?new Color(.40f,.46f,.36f):new Color(.37f,.43f,.34f),Daylight);
            Cloudiness=clouds;Rain=rain;Wetness=wetness;Wind=wind;PreviousWind=previousWind;
            Sky=Color.Lerp(new Color(.12f,.19f,.32f),new Color(.32f,.56f,.79f),Daylight);
            Horizon=Color.Lerp(new Color(.27f,.34f,.45f),golden?new Color(.76f,.83f,.84f):new Color(.74f,.83f,.89f),Daylight);
            float sunset=Mathf.Clamp01(1-Mathf.Abs(elevation-.22f)/.28f)*(1-clouds);
            Horizon=Color.Lerp(Horizon,new Color(1,.65f,.43f),sunset*.6f);
            Sky=Color.Lerp(Sky,Color.Lerp(new Color(.12f,.16f,.23f),new Color(.32f,.39f,.43f),Daylight),clouds*.85f);
            Horizon=Color.Lerp(Horizon,Color.Lerp(new Color(.20f,.26f,.34f),new Color(.48f,.56f,.58f),Daylight),clouds*.8f);
            Fog=Color.Lerp(Horizon,new Color(.56f,.68f,.73f),Daylight*.55f*(1-clouds));
            FogParameters=new Vector4(Mathf.Lerp(golden?.0032f:.0028f,.006f,rain)*240/Mathf.Max(80,fogDistance),.07f,12,10);
        }
        public static float AdvanceWetness(float current,float precipitation,float activeSeconds)
        {
            if(activeSeconds<=0 || float.IsNaN(activeSeconds))return current;
            float rate=precipitation>current?1/90f:1/300f;
            return Mathf.Lerp(precipitation,current,Mathf.Exp(-Mathf.Min(activeSeconds,86400)*rate));
        }
    }
}
