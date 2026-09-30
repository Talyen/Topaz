using UnityEngine;

namespace Topaz.Generation
{
    /// <summary>Optional authored recipe overrides; the existing WoodlandPreset supplies art bindings.</summary>
    [CreateAssetMenu(menuName="Topaz/Area Recipe")]
    public sealed class AreaDefinition : ScriptableObject
    {
        public AreaKind kind;
        [Range(256,384)] public int footprint=256;
        [Range(0,4)] public float localRelief=2;
        [Range(0,20)] public float trailMeander=10;
        public void Apply(WoodlandSettings settings)
        {settings.worldSize=footprint==384?384:256;settings.localRelief=localRelief;settings.trailMeander=trailMeander;}
    }
    [System.Serializable]
    public struct AreaLocation
    {
        public string areaId;
        public Vector3 position;
        public AreaLocation(string id,Vector3 point){areaId=id;position=point;}
    }
    public sealed class AreaPlan
    {
        public readonly AreaRecord Record;
        public readonly WildernessPlan Landscape;
        public AreaPlan(AreaRecord record,WoodlandSettings recordedSettings)
        {Record=record;Landscape=new WildernessPlan(record.seed,ForArea(record,recordedSettings));}
        public static WoodlandSettings ForArea(AreaRecord record,WoodlandSettings source)
        {
            var settings=(record.generationSettings??source).Copy();settings.areaId=record.id;settings.areaKind=record.kind;settings.areaLandmark=record.landmark;
            settings.areaExits=new System.Collections.Generic.List<AreaConnection>(record.exits);
            if(record.generationSettings==null)settings.worldSize=record.kind==AreaKind.Landmark?384:256;
            return settings;
        }
    }
}
