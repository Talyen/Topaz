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
        public void RuntimeTerrainMaterialRetainsInstancedNormalVariant()
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Topaz/Presentation/Rendering/SyntySample/Terrain.mat");
            Assert.That(material.enableInstancing,Is.True);
            Assert.That(material.IsKeywordEnabled("_TERRAIN_INSTANCED_PERPIXEL_NORMAL"),Is.True,
                "Runtime-generated terrain must retain the same normal variant used by the Editor.");
        }
        [Test]
        public void OnlyGeneratedGameplayScenesShip()
        {
            var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            Assert.That(enabled.Select(scene => scene.path).ToArray(), Is.EqualTo(new[]
            {
                "Assets/Topaz/World/Scenes/Bootstrap.unity"
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
            Assert.That(player.FindAction("Block").bindings.Any(binding => binding.path == "<Mouse>/rightButton"), Is.True);
            Assert.That(player.FindAction("Block").bindings.Any(binding => binding.path == "<Gamepad>/leftTrigger"), Is.True);
            Assert.That(player.FindAction("Place").bindings.Any(binding => binding.path == "<Gamepad>/buttonSouth"), Is.True);
            Assert.That(player.FindAction("Cancel").bindings.Any(binding => binding.path == "<Keyboard>/escape"), Is.True);
            Assert.That(player.FindAction("Inventory").bindings.Any(binding => binding.path == "<Keyboard>/b"), Is.True);
        }

        [Test]
        public void CombatDefinitionsAndProceduralPresetAreAvailable()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/Topaz/Gameplay/Combat/Definitions/PracticeSword.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/Topaz/Gameplay/Combat/Definitions/PracticeEnemy.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset"), Is.Not.Null);
        }

        [Test]
        public void WorldLoopHasStableDefinitionAssets()
        {
            string root = "Assets/Topaz/Gameplay/Inventory/Definitions/";
            foreach (string asset in new[] { "Wood", "Tree", "StorageChest", "StorageChestRecipe", "AxeChop" })
                Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>((asset.StartsWith("StorageChest") ? "Assets/Topaz/Gameplay/Building/Definitions/" : asset=="Tree" ? "Assets/Topaz/Gameplay/Gathering/Definitions/" : root) + asset + ".asset"),
                    Is.Not.Null, asset);
            var tree = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Topaz/Gameplay/Gathering/Definitions/Tree.asset");
            Assert.That(new SerializedObject(tree).FindProperty("requiredToolId").stringValue,
                Is.EqualTo("axe"));
        }

        [Test]
        public void UrpHasModernScalableRendering()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.That(urp, Is.Not.Null);
            Assert.That(urp.supportsHDR, Is.True);
            Assert.That(urp.msaaSampleCount, Is.EqualTo(1));
            Assert.That(urp.gpuResidentDrawerMode, Is.EqualTo(GPUResidentDrawerMode.InstancedDrawing));
            Assert.That(urp.reflectionProbeBlending && urp.reflectionProbeBoxProjection, Is.True);
            Assert.That(urp.upscalingFilter, Is.EqualTo(UpscalingFilterSelection.STP));
        }
    }
}
