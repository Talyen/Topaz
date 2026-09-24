using System;

namespace Topaz.VisualStudy
{
    /// <summary>Local review settings, separate from the authored URP defaults.</summary>
    [Serializable]
    public sealed class VisualStudySettings
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public int look = 1;
        public int antiAliasing = 2;
        public float temperature = -2f;
        public float exposure = .12f;
        public float contrast = 7f;
        public float saturation = 7f;
        public float bloomIntensity = .28f;
        public float bloomThreshold = .95f;
        public float vignette = .08f;
        public float depthStart = 23f;
        public float depthEnd = 36f;
        public float depthRadius = 1f;
        public float fogEnd = 70f;
        public float homeLight = 8f;
        public float shadowStrength = .72f;
        public float homeWarmth = 18f;
    }
}
