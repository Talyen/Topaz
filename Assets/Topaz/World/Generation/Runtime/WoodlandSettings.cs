using System;

namespace Topaz.Generation
{
    /// <summary>Versioned generation inputs copied into each World. Art bindings live in WoodlandPreset.</summary>
    [Serializable]
    public sealed class WoodlandSettings
    {
        public int version = WildernessPlan.Version;
        public int resolution = 65;
        public float size = WildernessPlan.ChunkSize;
        public float relief = 13f;
        public int decorationCount = 560;
        public WoodlandSettings Copy() => (WoodlandSettings)MemberwiseClone();
        public void Validate()
        {
            if(version!=WildernessPlan.Version)throw new ArgumentException("Unsupported wilderness generator version.");
            if(resolution!=65 || size!=WildernessPlan.ChunkSize || float.IsNaN(relief) || float.IsInfinity(relief) ||
                relief<0 || relief>20 || decorationCount<0 || decorationCount>1000)
                throw new ArgumentException("Invalid wilderness generation settings.");
        }
    }
}
