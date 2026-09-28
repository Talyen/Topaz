using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz.Tests
{
    public sealed class SurfaceCacheTests
    {
        [Test]
        public void DefaultAndWorldProfilesExcludeVisualOnlyGeometryBeforeRuntimeSetup()
        {
#if SURFACE_CACHE
            foreach(string path in new[]{"Assets/Topaz/Presentation/Rendering/Settings/DefaultVolumeProfile.asset",
                "Assets/Topaz/Presentation/Rendering/Environment/URP Environment.asset"})
            {
                var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                Assert.That(profile.TryGet<SurfaceCacheGIVolumeOverride>(out var gi),Is.True,path);
                Assert.That(gi.renderingLayerMask.overrideState,Is.True,path);
                Assert.That(((uint)gi.renderingLayerMask.value & 2u),Is.Zero,"Transparent water must stay outside GI during scene transitions: "+path);
            }
#else
            Assert.Fail("Native surface cache is required.");
#endif
        }
        [Test]
        public void DesktopBuildsRetainNativeSurfaceCacheResources()
        {
            var setup = Type.GetType("Topaz.Generation.Editor.SurfaceCacheSetup, Assembly-CSharp-Editor", true);
            foreach (var target in new[] { BuildTarget.StandaloneOSX, BuildTarget.StandaloneWindows64 })
                setup.GetMethod("Validate").Invoke(null, new object[] { target });
        }

        [Test]
        public void PresetChangesKeepGiEnabledAndCacheDimensionsStable()
        {
#if SURFACE_CACHE
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            try
            {
                var apply = Type.GetType("Topaz.Rendering.SurfaceCacheLighting, Assembly-CSharp", true).GetMethod("Apply");
                apply.Invoke(null, new object[] { profile, true });
                Assert.That(profile.TryGet<SurfaceCacheGIVolumeOverride>(out var first), Is.True);
                Assert.That(first.GetPresetQuality(), Is.EqualTo(SurfaceCacheGIVolumeOverride.PresetQuality.Medium));
                foreach (bool high in new[] { false, true, false })
                {
                    apply.Invoke(null, new object[] { profile, high });
                    profile.TryGet<SurfaceCacheGIVolumeOverride>(out var current);
                    Assert.That(current, Is.SameAs(first));
                    Assert.That(current.enabled.value && current.active, Is.True);
                    Assert.That(current.GetPresetQuality(), Is.EqualTo(high ? SurfaceCacheGIVolumeOverride.PresetQuality.Medium : SurfaceCacheGIVolumeOverride.PresetQuality.Low));
                    Assert.That(current.volumeSize.value, Is.EqualTo(128));
                    Assert.That(current.volumeResolution.value, Is.EqualTo(16));
                    Assert.That(current.volumeCascadeCount.value, Is.EqualTo(4));
                    Assert.That(current.cascadeMovement.value, Is.True);
                }
            }
            finally
            {
                foreach (var component in profile.components) UnityEngine.Object.DestroyImmediate(component);
                UnityEngine.Object.DestroyImmediate(profile);
            }
#else
            Assert.Fail("Topaz requires SURFACE_CACHE for desktop builds.");
#endif
        }
    }
}
