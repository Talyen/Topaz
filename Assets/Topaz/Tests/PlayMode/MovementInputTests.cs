using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class MovementInputTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator HoldingWMovesTheCharacterTowardScreenTop()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject player = GameObject.Find("Player");
            Camera camera = Camera.main;
            Assert.That(player, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);

            Vector3 start = player.transform.position;
            Vector3 screenUpOnGround = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            Press(keyboard.wKey);
            yield return new WaitForSeconds(0.3f);
            Release(keyboard.wKey);

            Vector3 travelled = player.transform.position - start;
            Assert.That(Vector3.Dot(travelled, screenUpOnGround), Is.GreaterThan(0.1f),
                "Holding W should move the player toward the top of the fixed-angle view.");
        }

        [UnityTest]
        public IEnumerator GamepadSticksMoveAndAimIndependently()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject player = GameObject.Find("Player");
            Camera camera = Camera.main;
            Assert.That(player, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);

            Vector3 start = player.transform.position;
            Set(gamepad.leftStick, Vector2.up);
            Set(gamepad.rightStick, Vector2.right);
            yield return new WaitForSeconds(0.3f);

            Vector3 screenUpOnGround = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            Assert.That(Vector3.Dot(player.transform.position - start, screenUpOnGround), Is.GreaterThan(0.1f));

            Transform visual = GameObject.Find("Facing Visual").transform;
            Vector3 screenRightOnGround = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
            Assert.That(Vector3.Dot(visual.forward, screenRightOnGround), Is.GreaterThan(0.8f));
        }

        [UnityTest]
        public IEnumerator InteractOpensAndClosesNearbyWorkbench()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject player = GameObject.Find("Player");
            GameObject bench = GameObject.Find("Workbench");
            GameObject hud = GameObject.Find("Loop HUD");
            Assert.That(player, Is.Not.Null);
            Assert.That(bench, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            player.transform.position = bench.transform.position + Vector3.forward * .7f;
            yield return new WaitForSeconds(0.1f);
            Press(keyboard.eKey);
            yield return new WaitForSeconds(0.1f);
            Release(keyboard.eKey);
            bool menuOpen = (bool)hud.GetComponent("LoopHud").GetType()
                .GetProperty("MenuOpen").GetValue(hud.GetComponent("LoopHud"));
            Assert.That(menuOpen, Is.True, "Interaction should open the workbench panel.");
            yield return null;
            Press(keyboard.eKey);
            yield return new WaitForSeconds(0.1f);
            Release(keyboard.eKey);
            menuOpen = (bool)hud.GetComponent("LoopHud").GetType()
                .GetProperty("MenuOpen").GetValue(hud.GetComponent("LoopHud"));
            Assert.That(menuOpen, Is.False, "Interaction should close the workbench panel.");
        }
    }
}
