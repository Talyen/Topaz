using System;

namespace Topaz.Rendering
{
    /// <summary>Local review settings, separate from the authored URP defaults.</summary>
    [Serializable]
    public sealed class GraphicsPreferences
    {
        public const int CurrentVersion = 6;
        public int version = CurrentVersion;
        public int look = 0;
        public int antiAliasing = 3;
        public int focusMode = 0; // Gaussian; Bokeh is an optional desktop comparison.
        public bool ambientOcclusion = true;
        public bool bloomEnabled = true;
        public float temperature = 4f;
        public float exposure = .35f;
        public float contrast = 5f;
        public float saturation = 7f;
        public float bloomIntensity = .15f;
        public float bloomThreshold = .70f;
        public float vignette = 0f;
        public float depthStart = 30f;
        public float depthEnd = 40f;
        public float depthRadius = .5f;
        public float bokehFocusDistance = 22f;
        public float bokehAperture = 2.8f;
        public float bokehFocalLength = 120f;
        public float fogEnd = 240f;
        public float homeLight = 10f;
        public float shadowStrength = .78f;
        public float homeWarmth = 35f;
    }
}
