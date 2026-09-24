using System;

namespace Topaz.VisualStudy
{
    /// <summary>Local review settings, separate from the authored URP defaults.</summary>
    [Serializable]
    public sealed class VisualStudySettings
    {
        public const int CurrentVersion = 5;
        public int version = CurrentVersion;
        public int look = 2;
        public int antiAliasing = 3;
        public int focusMode = 0; // Gaussian; Bokeh is an optional desktop comparison.
        public bool ambientOcclusion = true;
        public bool bloomEnabled = true;
        public float temperature = 25f;
        public float exposure = 0f;
        public float contrast = 0f;
        public float saturation = 10f;
        public float bloomIntensity = 1f;
        public float bloomThreshold = .70f;
        public float vignette = 0f;
        public float depthStart = 30f;
        public float depthEnd = 40f;
        public float depthRadius = .5f;
        public float bokehFocusDistance = 22f;
        public float bokehAperture = 2.8f;
        public float bokehFocalLength = 120f;
        public float fogEnd = 65f;
        public float homeLight = 10f;
        public float shadowStrength = .90f;
        public float homeWarmth = 35f;
    }
}
