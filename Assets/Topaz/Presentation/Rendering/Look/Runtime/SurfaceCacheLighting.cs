using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz.Rendering
{
    /// <summary>Topaz policy for Unity's native, transient diffuse GI cache.</summary>
    public static class SurfaceCacheLighting
    {
        public const uint VisualOnlyRenderingLayer=2u;
        public static bool DiagnosticDisabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return Array.IndexOf(Environment.GetCommandLineArgs(), "--topaz-scgi-off") >= 0;
#else
                return false;
#endif
            }
        }

        public static void Apply(VolumeProfile profile, bool highQuality)
        {
#if SURFACE_CACHE
            if (!profile.TryGet<SurfaceCacheGIVolumeOverride>(out var gi))
                gi = profile.Add<SurfaceCacheGIVolumeOverride>(true);
            gi.active = true;
            gi.enabled.Override(!DiagnosticDisabled);
            gi.ApplyPreset(highQuality ? SurfaceCacheGIVolumeOverride.PresetQuality.Medium : SurfaceCacheGIVolumeOverride.PresetQuality.Low);
            if(Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-gi-medium")>=0)
                gi.ApplyPreset(SurfaceCacheGIVolumeOverride.PresetQuality.Medium);
            gi.volumeSize.Override(128);
            // Native VoxelMinSize = size / (resolution * 2^(cascades-1)).
            // 16^3 x 4 keeps 1 m finest cells and the 128 m outer extent with six times fewer cells than 32^3 x 3.
            gi.volumeResolution.Override(16);gi.volumeCascadeCount.Override(4);
            if(Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-gi-detailed")>=0)
            {gi.volumeResolution.Override(32);gi.volumeCascadeCount.Override(3);}
            gi.cascadeMovement.Override(true);
            gi.renderingLayerMask.Override((RenderingLayerMask)~VisualOnlyRenderingLayer);
#else
            throw new InvalidOperationException("[Topaz/SCGI] SURFACE_CACHE must be enabled for desktop players.");
#endif
        }

        [Serializable]
        public sealed class Status
        {
            public bool compiled, enabled, computeSupported, hardwareRayTracing;
            public string preset, backend;
            public float volumeSize;
            public int volumeResolution, cascades;
        }

        public static Status Describe(VolumeProfile profile)
        {
            var status = new Status { computeSupported = SystemInfo.supportsComputeShaders,
                hardwareRayTracing = SystemInfo.supportsRayTracing,
                backend = SystemInfo.supportsRayTracing ? "Hardware" : "Compute" };
#if SURFACE_CACHE
            status.compiled = true;
            if (profile != null && profile.TryGet<SurfaceCacheGIVolumeOverride>(out var gi))
            {
                status.enabled = gi.active && gi.enabled.value;
                status.preset = gi.GetPresetQuality().ToString();
                status.volumeSize = gi.volumeSize.value;
                status.volumeResolution = gi.volumeResolution.value;
                status.cascades = gi.volumeCascadeCount.value;
            }
#endif
            return status;
        }
    }
}
