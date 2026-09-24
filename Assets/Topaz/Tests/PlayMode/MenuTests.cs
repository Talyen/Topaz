using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class MenuTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator EscapePausesAndResumesTheGame()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            Component menus = GameObject.Find("Loop HUD").GetComponent("GameMenus");
            Assert.That(menus, Is.Not.Null);
            Assert.That((bool)menus.GetType().GetProperty("IsPaused").GetValue(menus), Is.False);
            Press(keyboard.escapeKey);
            yield return null;
            Assert.That((bool)menus.GetType().GetProperty("IsPaused").GetValue(menus), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f),
                "Editor tests keep simulation time running while checking menu navigation.");
            Release(keyboard.escapeKey);
            yield return null;
            Press(keyboard.escapeKey);
            yield return null;
            Assert.That((bool)menus.GetType().GetProperty("IsPaused").GetValue(menus), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Release(keyboard.escapeKey);
        }

        [UnityTest]
        public IEnumerator TitleAndOptionsPanelsAreWired()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            Assert.That(hud.transform.Find("Desktop Menus/Title Screen"), Is.Not.Null);
            Assert.That(hud.transform.Find("Desktop Menus/Pause Screen"), Is.Not.Null);
            Assert.That(hud.transform.Find("Desktop Menus/Options Screen"), Is.Not.Null);
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            Assert.That((bool)menus.GetType().GetProperty("IsTitle").GetValue(menus), Is.True);
            menus.GetType().GetMethod("Continue").Invoke(menus, null);
            Assert.That((bool)menus.GetType().GetProperty("IsTitle").GetValue(menus), Is.False);
        }

        [UnityTest]
        public IEnumerator VisualLabTemporarilyReplacesOptionsCard()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            Component visualLab = hud.GetComponent("VisualOptionsMenu");
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            var titleOptions = hud.transform.Find("Desktop Menus/Title Screen/Card/Options")
                .GetComponent<UnityEngine.UI.Button>();
            titleOptions.onClick.Invoke();
            GameObject options = hud.transform.Find("Desktop Menus/Options Screen").gameObject;
            Assert.That(options.activeSelf, Is.True);
            visualLab.GetType().GetMethod("Toggle").Invoke(visualLab, null);
            Assert.That(options.activeSelf, Is.False);
            visualLab.GetType().GetMethod("Close").Invoke(visualLab, null);
            Assert.That(options.activeSelf, Is.True);
            menus.GetType().GetMethod("Continue").Invoke(menus, null);
        }

        [UnityTest]
        public IEnumerator GamepadCanNavigateTitleButtons()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("Continue"));
            Set(gamepad.dpad, Vector2.down);
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("Options"));
            Set(gamepad.dpad, Vector2.zero);
            Set(gamepad.buttonSouth, 1f);
            yield return null;
            Assert.That(hud.transform.Find("Desktop Menus/Options Screen").gameObject.activeSelf,
                Is.True);
            Set(gamepad.buttonSouth, 0f);
            menus.GetType().GetMethod("Continue").Invoke(menus, null);
        }
    }
}
