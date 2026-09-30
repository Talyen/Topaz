using System;

namespace Topaz.Rendering
{
    /// <summary>Local review settings, separate from the authored URP defaults.</summary>
    [Serializable]
    public sealed class GraphicsPreferences
    {
        public const int CurrentVersion = 9;
        public int version = CurrentVersion;
        public int look = 1;
        public int lightingStyle = 1; // Natural cycle or Golden / Silver.
        public int antiAliasing = 3;
        public bool macPerformancePresetApplied;
        // Apply the measured Mac starting preset once; later explicit selections stay authoritative.
        public bool ApplyMacStartingPreset()
        {
            if(macPerformancePresetApplied)return false;
            look=0;antiAliasing=4;macPerformancePresetApplied=true;return true;
        }
        public int focusMode = 2; // Intentional player-tracked cinematic bokeh; retain the owner's depth treatment.
        public bool ambientOcclusion = true;
        public bool bloomEnabled = true;
        public float temperature = 0f;
        public float exposure = .3f;
        public float contrast = 7f;
        public float saturation = 0f;
        public float bloomIntensity = .18f;
        public float bloomThreshold = 1.25f;
        public float vignette = 0f;
        public float depthStart = 20f;
        public float depthEnd = 65f;
        public float depthRadius = 1.1f;
        public float bokehFocusDistance = 12f;
        public float bokehAperture = 4f;
        public float bokehFocalLength = 55f;
        public float fogEnd = 320f;
        public float homeLight = 10f;
        public float shadowStrength = .9f;
        public float homeWarmth = 35f;
        public float motionBlur = 0f;
        public float chromaticAberration = 0f;
        public float grading = .3f;
        public float windStrength = 1f;
        public float foliageDensity = 1f;
        public float ambientParticles = .5f;
    }
}
