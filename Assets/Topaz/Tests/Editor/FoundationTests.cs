using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
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

        [Test]
        public void FeelStudyHasKeyboardMouseAndGamepadActions()
        {
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Topaz/Input/TopazControls.inputactions");
            Assert.That(controls, Is.Not.Null);

            InputActionMap player = controls.FindActionMap("Player", true);
            string[] required = { "Move", "AimPointer", "AimStick", "Dodge", "Interact", "ZoomWheel", "ZoomIn", "ZoomOut" };
            foreach (string name in required)
                Assert.That(player.FindAction(name), Is.Not.Null, name);

            Assert.That(player.FindAction("Move").bindings.Any(binding => binding.path == "<Keyboard>/w"), Is.True);
            Assert.That(player.FindAction("Move").bindings.Any(binding => binding.path == "<Gamepad>/leftStick"), Is.True);
            Assert.That(player.FindAction("Dodge").bindings.Any(binding => binding.path == "<Gamepad>/buttonEast"), Is.True);
            Assert.That(player.FindAction("Interact").bindings.Any(binding => binding.path == "<Gamepad>/buttonWest"), Is.True);
            Assert.That(player.FindAction("Attack").bindings.Any(binding => binding.path == "<Mouse>/leftButton"), Is.True);
            Assert.That(player.FindAction("Attack").bindings.Any(binding => binding.path == "<Gamepad>/rightTrigger"), Is.True);
            Assert.That(player.FindAction("EquipAxe").bindings.Any(binding => binding.path == "<Keyboard>/2"), Is.True);
            Assert.That(player.FindAction("CycleTool").bindings.Any(binding => binding.path == "<Gamepad>/buttonNorth"), Is.True);
            Assert.That(player.FindAction("Place").bindings.Any(binding => binding.path == "<Gamepad>/buttonSouth"), Is.True);
            Assert.That(player.FindAction("Cancel").bindings.Any(binding => binding.path == "<Keyboard>/escape"), Is.True);
        }

        [Test]
        public void CombatStudyHasAuthoredDefinitionsAndBakedNavigation()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/Topaz/CombatStudy/Definitions/PracticeSword.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/Topaz/CombatStudy/Definitions/PracticeEnemy.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<NavMeshData>(
                "Assets/Topaz/CombatStudy/Navigation/PracticeNavMesh.asset"), Is.Not.Null);
        }

        [Test]
        public void WorldLoopHasStableDefinitionAssets()
        {
            string root = "Assets/Topaz/LoopStudy/Definitions/";
            foreach (string asset in new[] { "Wood", "Tree", "StorageChest", "StorageChestRecipe", "AxeChop" })
                Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>(root + asset + ".asset"),
                    Is.Not.Null, asset);
        }
    }
}
