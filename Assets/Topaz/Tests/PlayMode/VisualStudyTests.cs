using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class VisualStudyTests : InputTestFixture
    {
        [System.Serializable]
        sealed class SavedValue
        {
            public int antiAliasing;
            public int focusMode;
            public bool bloomEnabled;
            public bool ambientOcclusion;
            public float bokehAperture;
            public float bokehFocalLength;
        }

        [UnityTest]
        public IEnumerator GraphicsMenuAppliesAndSavesSelectionsImmediately()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject root = GameObject.Find("Visual Study");
            GameObject hud = GameObject.Find("Loop HUD");
            Assert.That(root, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Component controller = root.GetComponent("VisualLookController");
            Transform panel = hud.transform.Find("Graphics");
            Assert.That(controller, Is.Not.Null);
            Assert.That(panel, Is.Not.Null);

            Component graphics = hud.GetComponent("VisualOptionsMenu");
            graphics.GetType().GetMethod("Toggle").Invoke(graphics, null);
            yield return null;
            Assert.That(panel.gameObject.activeSelf, Is.True);

            var depth = panel.Find("Depth of Field/Depth of Field Dropdown")
                .GetComponent("TMP_Dropdown");
            var aa = panel.Find("Anti-aliasing/Anti-aliasing Dropdown")
                .GetComponent("TMP_Dropdown");
            var zoom = panel.Find("Camera Zoom/Camera Zoom Dropdown")
                .GetComponent("TMP_Dropdown");
            depth.GetType().GetProperty("value").SetValue(depth, 2);
            aa.GetType().GetProperty("value").SetValue(aa, 5);
            zoom.GetType().GetProperty("value").SetValue(zoom, 2);
            panel.Find("Bloom").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
            panel.Find("Ambient Occlusion").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
            Assert.That((int)controller.GetType().GetProperty("CurrentDepthMode").GetValue(controller),
                Is.EqualTo(2));
            Assert.That((int)controller.GetType().GetProperty("CurrentAa").GetValue(controller),
                Is.EqualTo(5));
            object urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            Assert.That(urp, Is.Not.Null);
            Assert.That(urp.GetType().GetProperty("msaaSampleCount").GetValue(urp), Is.EqualTo(4));
            Component menus = hud.GetComponent("GameMenus");
            Assert.That((int)menus.GetType().GetProperty("CurrentCameraZoomIndex").GetValue(menus),
                Is.EqualTo(2));

            string path = (string)controller.GetType().GetProperty("SettingsPath").GetValue(controller);
            Assert.That(path, Does.StartWith(Application.temporaryCachePath));
            Assert.That(File.Exists(path), Is.True);
            SavedValue saved = JsonUtility.FromJson<SavedValue>(File.ReadAllText(path));
            Assert.That(saved.antiAliasing, Is.EqualTo(5));
            Assert.That(saved.focusMode, Is.EqualTo(1));
            Assert.That(saved.bloomEnabled, Is.False);
            Assert.That(saved.ambientOcclusion, Is.False);
            Assert.That(saved.bokehAperture, Is.EqualTo(2.8f).Within(.001f));
            Assert.That(saved.bokehFocalLength, Is.EqualTo(120f).Within(.001f));

            panel.Find("Reset to Default").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(depth.GetType().GetProperty("value").GetValue(depth), Is.EqualTo(1));
            Assert.That(aa.GetType().GetProperty("value").GetValue(aa), Is.EqualTo(3));
            Assert.That(urp.GetType().GetProperty("msaaSampleCount").GetValue(urp), Is.EqualTo(1));
            Assert.That(zoom.GetType().GetProperty("value").GetValue(zoom), Is.EqualTo(1));
            Assert.That(panel.Find("Bloom").GetComponent<UnityEngine.UI.Toggle>().isOn, Is.True);
            Assert.That(panel.Find("Ambient Occlusion").GetComponent<UnityEngine.UI.Toggle>().isOn,
                Is.True);

            graphics.GetType().GetMethod("Close").Invoke(graphics, null);
            yield return null;
            Assert.That(panel.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator GraphicsMenuUsesDropdownsAndTogglesWithoutSliders()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject root = GameObject.Find("Visual Study");
            Transform panel = GameObject.Find("Loop HUD").transform.Find("Graphics");
            Assert.That(panel.Find("Camera Zoom"), Is.Not.Null);
            Assert.That(panel.Find("Anti-aliasing"), Is.Not.Null);
            Assert.That(panel.Find("Depth of Field"), Is.Not.Null);
            Assert.That(panel.Find("Bloom"), Is.Not.Null);
            Assert.That(panel.Find("Ambient Occlusion"), Is.Not.Null);
            Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Slider>(true).Length,
                Is.EqualTo(0));
            Assert.That(panel.Find("Save selection"), Is.Null);
            Assert.That(panel.Find("Copy values"), Is.Null);
            Assert.That(root.transform.Find("Ground Details"), Is.Null);
            Assert.That(GameObject.Find("Loop HUD").transform.Find("Context"), Is.Null);
        }

        [UnityTest]
        public IEnumerator NightKeepsAReadableKeyLightAndRestFadeRecovers()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component controller = GameObject.Find("Visual Study").GetComponent("VisualLookController");
            Light sun = GameObject.Find("Directional Light").GetComponent<Light>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(sun, Is.Not.Null);
            var setHours = controller.GetType().GetMethod("SetWorldHours");
            var setFade = controller.GetType().GetMethod("SetRestFade");
            setHours.Invoke(controller, new object[] { 12d });
            float noonIntensity = sun.intensity;
            setHours.Invoke(controller, new object[] { 23d });
            Assert.That(sun.intensity, Is.GreaterThan(.5f).And.LessThan(noonIntensity));
            Assert.That(RenderSettings.ambientSkyColor.b,
                Is.GreaterThan(RenderSettings.ambientSkyColor.r));

            setFade.Invoke(controller, new object[] { 1f });
            Assert.That(sun.intensity, Is.Zero);
            setFade.Invoke(controller, new object[] { 0f });
            Assert.That(sun.intensity, Is.GreaterThan(.5f));
        }

        [UnityTest]
        public IEnumerator GameplayEffectsStartEnabledAndDodgeEmitsDust()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            Assert.That(GameObject.Find("Forest Patterned Light"), Is.Null);
            Assert.That(GameObject.Find("Visual Study Label"), Is.Null);
            Assert.That(GameObject.Find("Resources"), Is.Null);
            ParticleSystem particles = GameObject.Find("Effects Comparison Particles")
                ?.GetComponent<ParticleSystem>();
            Assert.That(particles, Is.Not.Null);
            Assert.That(particles.GetComponent<ParticleSystemRenderer>().sharedMaterial, Is.Not.Null);
            Assert.That(GameObject.Find("Effects Comparison Sparks"), Is.Not.Null);
            Component menus = GameObject.Find("Loop HUD").GetComponent("GameMenus");
            menus.GetType().GetMethod("Continue").Invoke(menus, null);
            Press(keyboard.leftShiftKey);
            yield return null;
            Release(keyboard.leftShiftKey);
            Assert.That(particles.particleCount, Is.GreaterThan(0),
                "A dodge should emit dust without enabling any visual study toggle.");
        }

        [UnityTest]
        public IEnumerator SwordTrailAndGlintsFollowTheActiveSwing()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject hud = GameObject.Find("Loop HUD");
            hud.GetComponent("GameMenus").GetType().GetMethod("Continue")
                .Invoke(hud.GetComponent("GameMenus"), null);
            ParticleSystem sparks = GameObject.Find("Effects Comparison Sparks")
                .GetComponent<ParticleSystem>();
            TrailRenderer trail = null;
            foreach (TrailRenderer candidate in GameObject.Find("Player")
                .GetComponentsInChildren<TrailRenderer>(true))
                if (candidate.name == "Sword Slash Trail") trail = candidate;
            Assert.That(trail, Is.Not.Null);

            Set(gamepad.rightStick, Vector2.right);
            yield return null;
            Set(gamepad.rightTrigger, 1f);
            yield return new WaitForSeconds(.25f);
            Set(gamepad.rightTrigger, 0f);
            Component combat = GameObject.Find("Player").GetComponent("PlayerCombat");
            Assert.That((bool)combat.GetType().GetProperty("IsAttackLocked").GetValue(combat),
                Is.True, "The combat swing must be running before checking presentation.");
            Assert.That(sparks.particleCount, Is.GreaterThan(0));
            Assert.That(trail.positionCount, Is.GreaterThan(1),
                "The trail should be drawn by blade movement during the strike.");
        }
    }
}
