using System;

namespace Topaz.VisualStudy
{
    /// <summary>Local review settings, separate from the authored URP defaults.</summary>
    [Serializable]
    public sealed class VisualStudySettings
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public int look = 2;
        public int antiAliasing = 3;
        public float temperature = 40f;
        public float exposure = 0f;
        public float contrast = 0f;
        public float saturation = 10f;
        public float bloomIntensity = 1f;
        public float bloomThreshold = .70f;
        public float vignette = 0f;
        public float depthStart = 24f;
        public float depthEnd = 30f;
        public float depthRadius = 1f;
        public float fogEnd = 65f;
        public float homeLight = 10f;
        public float shadowStrength = .90f;
        public float homeWarmth = 35f;
    }
}
