using System;

namespace Topaz.Generation
{
    /// <summary>Versioned generation inputs copied into each World. Art bindings live in WoodlandPreset.</summary>
    [Serializable]
    public sealed class WoodlandSettings
    {
        public int version = WildernessPlan.Version;
        public string profileId = "alpine-1024-v6";
        public string contentId = "viking-alpine-v6";
        public int worldSize = 1024;
        public int resolution = 65;
        public float size = WildernessPlan.ChunkSize;
        public float relief = 13f;
        public int decorationCount = 560;
        public float groveScale = 48f;
        public float groveCoverage = .72f;
        public float groundCoverDensity = 1.2f;
        public float localRelief = 3.5f;
        public float trailMeander = 14f;
        public float trailMinimumHalfWidth = 1f;
        public float trailMaximumHalfWidth = 1.8f;
        public float landmarkApproachLength = 44f;
        public float landmarkApproachWidth = 8f;
        public static WoodlandSettings LargeWorld() => new WoodlandSettings {profileId="alpine-2048-v6",worldSize=2048};
        public WoodlandSettings Copy() => (WoodlandSettings)MemberwiseClone();
        public void Validate()
        {
            if(version!=WildernessPlan.Version)throw new ArgumentException("Unsupported wilderness generator version.");
            if(!((profileId=="alpine-1024-v6" && worldSize==1024)||(profileId=="alpine-2048-v6" && worldSize==2048)) || contentId!="viking-alpine-v6")
                throw new ArgumentException("Unsupported wilderness profile or content mapping.");
            if(resolution!=65 || size!=WildernessPlan.ChunkSize || float.IsNaN(relief) || float.IsInfinity(relief) ||
                relief<0 || relief>20 || decorationCount<0 || decorationCount>1000)
                throw new ArgumentException("Invalid wilderness generation settings.");
            bool Valid(float v,float min,float max)=>!float.IsNaN(v)&&!float.IsInfinity(v)&&v>=min&&v<=max;
            if(!Valid(groveScale,32,160)||!Valid(groveCoverage,.2f,.9f)||!Valid(groundCoverDensity,0,2)||
                !Valid(localRelief,0,4)||!Valid(trailMeander,0,20)||!Valid(trailMinimumHalfWidth,1,3)||!Valid(trailMaximumHalfWidth,trailMinimumHalfWidth,4)||
                !Valid(landmarkApproachLength,20,70)||!Valid(landmarkApproachWidth,4,12))
                throw new ArgumentException("Invalid Alpine composition settings.");
        }
    }
}
