using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz.Tests
{
    public sealed class FoundationTests
    {
        [Test]
        public void HomesteadAndExpeditionAreEnabledBuildScenes()
        {
            var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            Assert.That(enabled.Select(scene => scene.path), Is.EqualTo(new[]
            {
                "Assets/Topaz/World/Scenes/Bootstrap.unity", "Assets/Topaz/World/Scenes/Expedition.unity"
            }));
        }

        [Test]
        public void DesktopProfilesAndUrpAreConfigured()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<BuildProfile>("Assets/Topaz/Build/Profiles/macOS.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<BuildProfile>("Assets/Topaz/Build/Profiles/Windows.asset"), Is.Not.Null);
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

        [Test]
        public void FeelStudyHasKeyboardMouseAndGamepadActions()
        {
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Topaz/Core/Input/TopazControls.inputactions");
            Assert.That(controls, Is.Not.Null);

            InputActionMap player = controls.FindActionMap("Player", true);
            string[] required = { "Move", "AimPointer", "AimStick", "Dodge", "Jump", "Interact", "ZoomWheel", "ZoomIn", "ZoomOut" };
            foreach (string name in required)
                Assert.That(player.FindAction(name), Is.Not.Null, name);

            Assert.That(player.FindAction("Move").bindings.Any(binding => binding.path == "<Keyboard>/w"), Is.True);
            Assert.That(player.FindAction("Move").bindings.Any(binding => binding.path == "<Gamepad>/leftStick"), Is.True);
            Assert.That(player.FindAction("Dodge").bindings.Any(binding => binding.path == "<Gamepad>/buttonEast"), Is.True);
            Assert.That(player.FindAction("Dodge").bindings.Any(binding => binding.path == "<Keyboard>/leftShift"), Is.True);
            Assert.That(player.FindAction("Jump").bindings.Any(binding => binding.path == "<Keyboard>/space"), Is.True);
            Assert.That(player.FindAction("Jump").bindings.Any(binding => binding.path == "<Gamepad>/buttonSouth"), Is.True);
            Assert.That(player.FindAction("Interact").bindings.Any(binding => binding.path == "<Gamepad>/buttonWest"), Is.True);
            Assert.That(player.FindAction("Attack").bindings.Any(binding => binding.path == "<Mouse>/leftButton"), Is.True);
            Assert.That(player.FindAction("Attack").bindings.Any(binding => binding.path == "<Gamepad>/rightTrigger"), Is.True);
            Assert.That(player.FindAction("EquipAxe").bindings.Any(binding => binding.path == "<Keyboard>/2"), Is.True);
            Assert.That(player.FindAction("CycleTool").bindings.Any(binding => binding.path == "<Gamepad>/buttonNorth"), Is.True);
            Assert.That(player.FindAction("Place").bindings.Any(binding => binding.path == "<Gamepad>/buttonSouth"), Is.True);
            Assert.That(player.FindAction("Cancel").bindings.Any(binding => binding.path == "<Keyboard>/escape"), Is.True);
            Assert.That(player.FindAction("Inventory").bindings.Any(binding => binding.path == "<Keyboard>/b"), Is.True);
        }

        [Test]
        public void CombatStudyHasAuthoredDefinitionsAndBakedNavigation()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/Topaz/Gameplay/Combat/Definitions/PracticeSword.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/Topaz/Gameplay/Combat/Definitions/PracticeEnemy.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<NavMeshData>(
                "Assets/Topaz/World/Home/Navigation/PracticeNavMesh.asset"), Is.Not.Null);
        }

        [Test]
        public void WorldLoopHasStableDefinitionAssets()
        {
            string root = "Assets/Topaz/Gameplay/WorldLoop/Definitions/";
            foreach (string asset in new[] { "Wood", "Tree", "StorageChest", "StorageChestRecipe", "AxeChop" })
                Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>(root + asset + ".asset"),
                    Is.Not.Null, asset);
            var tree = AssetDatabase.LoadAssetAtPath<ScriptableObject>(root + "Tree.asset");
            Assert.That(new SerializedObject(tree).FindProperty("requiredToolId").stringValue,
                Is.EqualTo("axe"));
        }

        [Test]
        public void VisualStudyKeepsRuntimeDepthOfFieldAvailableInPlayers()
        {
            var stripping = GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>();
            Assert.That(stripping, Is.Not.Null);
            Assert.That(stripping.stripUnusedPostProcessingVariants, Is.False,
                "The Visual Lab changes Volume profiles at runtime, including depth of field.");

            var focus = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Topaz/Presentation/Graphics/Profiles/Focus Preview.asset");
            Assert.That(focus, Is.Not.Null);
            Assert.That(focus.TryGet(out DepthOfField depth), Is.True);
            Assert.That(depth.mode.value, Is.EqualTo(DepthOfFieldMode.Gaussian));
            Assert.That(depth.gaussianStart.value, Is.EqualTo(30f));
            Assert.That(depth.gaussianEnd.value, Is.EqualTo(40f));
            Assert.That(depth.gaussianMaxRadius.value, Is.EqualTo(.5f));
            Assert.That(depth.focusDistance.value, Is.EqualTo(22f));
            Assert.That(depth.aperture.value, Is.EqualTo(2.8f));
            Assert.That(depth.focalLength.value, Is.EqualTo(120f));
            Assert.That(depth.bladeCount.value, Is.EqualTo(6));

            var painterly = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Topaz/Presentation/Graphics/Profiles/Painterly Clear.asset");
            var home = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Topaz/Presentation/Graphics/Profiles/Warm Home.asset");
            Assert.That(painterly.TryGet(out WhiteBalance globalBalance), Is.True);
            Assert.That(home.TryGet(out WhiteBalance homeBalance), Is.True);
            Assert.That(globalBalance.temperature.value, Is.EqualTo(25f));
            Assert.That(homeBalance.temperature.value, Is.EqualTo(60f));

            var urp = UniversalRenderPipeline.asset;
            Assert.That(urp.hdrColorBufferPrecision, Is.EqualTo(HDRColorBufferPrecision._64Bits));
            Assert.That(urp.shadowDistance, Is.EqualTo(52f));
            Assert.That(urp.cascade2Split, Is.EqualTo(.45f));

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                "Assets/Topaz/Presentation/Rendering/Settings/PC_Renderer.asset");
            var correctedShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/Topaz/Presentation/Graphics/Shaders/OrthographicGaussianDepthOfField.shader");
            Assert.That(renderer.postProcessData, Is.Not.Null);
            Assert.That(renderer.postProcessData.shaders.gaussianDepthOfFieldPS,
                Is.SameAs(correctedShader));
            Assert.That(correctedShader.passCount, Is.EqualTo(5));

            var bokehShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/Topaz/Presentation/Graphics/Shaders/OrthographicBokehDepthOfField.shader");
            Assert.That(renderer.postProcessData.shaders.bokehDepthOfFieldPS,
                Is.SameAs(bokehShader));
            Assert.That(bokehShader.passCount, Is.EqualTo(5));
            Assert.That(renderer.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().Count(), Is.EqualTo(1));
            Assert.That(renderer.rendererFeatures.OfType<DecalRendererFeature>(), Is.Empty);
            var ao = new SerializedObject(renderer.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().Single());
            Assert.That(ao.FindProperty("m_Settings.Intensity").floatValue, Is.EqualTo(.85f));
            Assert.That(AssetDatabase.IsValidFolder("Assets/Topaz/Presentation/Graphics/Decals"), Is.False);

            MethodInfo getLightmap = typeof(PlayerSettings).GetMethod(
                "GetLightmapEncodingQualityForPlatform", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(getLightmap, Is.Not.Null);
            foreach (BuildTarget target in new[] { BuildTarget.StandaloneOSX, BuildTarget.StandaloneWindows64 })
                Assert.That(getLightmap.Invoke(null, new object[] { target }).ToString(),
                    Is.EqualTo("High"), target.ToString());
        }
    }
}
