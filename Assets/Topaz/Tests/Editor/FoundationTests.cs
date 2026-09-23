using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.Rendering;

namespace Topaz.Tests
{
    public sealed class FoundationTests
    {
        [Test]
        public void BootstrapIsTheOnlyEnabledBuildScene()
        {
            var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            Assert.That(enabled.Select(scene => scene.path), Is.EqualTo(new[] { "Assets/Scenes/Bootstrap.unity" }));
        }

        [Test]
        public void DesktopProfilesAndUrpAreConfigured()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<BuildProfile>("Assets/Settings/Build Profiles/macOS.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<BuildProfile>("Assets/Settings/Build Profiles/Windows.asset"), Is.Not.Null);
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.Not.Null);
        }

        [Test]
        public void EveryQualityLevelUsesDisplaySync()
        {
            int original = QualitySettings.GetQualityLevel();
            try
            {
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    Assert.That(QualitySettings.vSyncCount, Is.EqualTo(1), QualitySettings.names[i]);
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(original, false);
            }
        }
    }
}
