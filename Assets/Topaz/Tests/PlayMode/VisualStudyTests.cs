using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
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
        sealed class SavedValue { public float bloomIntensity; }

        [UnityTest]
        public IEnumerator VisualLabOpensAndSavesSelectedValuesOutsidePlayerSaveDirectory()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject root = GameObject.Find("Visual Study");
            GameObject hud = GameObject.Find("Loop HUD");
            Assert.That(root, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Component controller = root.GetComponent("VisualLookController");
            Transform panel = hud.transform.Find("Visual Lab");
            Assert.That(controller, Is.Not.Null);
            Assert.That(panel, Is.Not.Null);

            Press(keyboard.f7Key);
            yield return null;
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Release(keyboard.f7Key);

            MethodInfo set = controller.GetType().GetMethod("SetSetting");
            set.Invoke(controller, new object[] { 4, .63f });
            float bloom = (float)controller.GetType().GetMethod("GetSetting")
                .Invoke(controller, new object[] { 4 });
            Assert.That(bloom, Is.EqualTo(.63f).Within(.001f));
            set.Invoke(controller, new object[] { 7, 36f });
            set.Invoke(controller, new object[] { 8, 24f });
            float focusStart = (float)controller.GetType().GetMethod("GetSetting")
                .Invoke(controller, new object[] { 7 });
            Assert.That(focusStart, Is.LessThan(24f));

            string path = (string)controller.GetType().GetProperty("SettingsPath").GetValue(controller);
            Assert.That(path, Does.StartWith(Application.temporaryCachePath));
            controller.GetType().GetMethod("SaveSelection").Invoke(controller, null);
            SavedValue saved = JsonUtility.FromJson<SavedValue>(File.ReadAllText(path));
            Assert.That(saved.bloomIntensity, Is.EqualTo(.63f).Within(.001f));

            Press(keyboard.f7Key);
            yield return null;
            Assert.That(panel.gameObject.activeSelf, Is.False);
            Release(keyboard.f7Key);
        }

        [UnityTest]
        public IEnumerator VisualPolishButtonsSwitchEffectsAndFocusControls()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject root = GameObject.Find("Visual Study");
            Transform panel = GameObject.Find("Loop HUD").transform.Find("Visual Lab");
            Component controller = root.GetComponent("VisualLookController");
            Assert.That(panel.Find("Focus mode"), Is.Not.Null);
            Assert.That(panel.Find("Ambient occlusion"), Is.Not.Null);
            Assert.That(panel.Find("Ground detail"), Is.Not.Null);
            Assert.That(root.transform.Find("Ground Details").GetComponentsInChildren<Component>(true)
                .Count(component => component.GetType().Name == "DecalProjector"), Is.EqualTo(3));

            panel.Find("Focus mode").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That((int)controller.GetType().GetProperty("CurrentFocusMode").GetValue(controller),
                Is.EqualTo(1));
            float aperture = (float)controller.GetType().GetMethod("GetSetting")
                .Invoke(controller, new object[] { 8 });
            Assert.That(aperture, Is.EqualTo(1.25f));

            panel.Find("Ambient occlusion").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That((bool)controller.GetType().GetProperty("AmbientOcclusionEnabled")
                .GetValue(controller), Is.False);
            panel.Find("Ground detail").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(root.transform.Find("Ground Details").gameObject.activeSelf, Is.False);
        }
    }
}
