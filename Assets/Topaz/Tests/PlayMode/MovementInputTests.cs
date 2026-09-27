using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class MovementInputTests : TopazInputTestFixture
    {
        [UnityTest]
        public IEnumerator SpaceJumpsOnceAndReturnsToGround()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            yield return new WaitForSeconds(0.1f);

            GameObject player = GameObject.Find("Player");
            Animator animator = player.GetComponentInChildren<Animator>();
            float groundY = player.transform.position.y;
            Component movement = player.GetComponent("PlayerController");
            Assert.That((bool)movement.GetType().GetProperty("IsAirborne").GetValue(movement), Is.False);
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(0.12f);
            Assert.That(player.transform.position.y, Is.GreaterThan(groundY + 0.2f));
            Assert.That((bool)movement.GetType().GetProperty("IsAirborne").GetValue(movement), Is.True);
            yield return new WaitForSeconds(0.6f);
            Assert.That(player.transform.position.y, Is.LessThan(groundY + 0.1f));
            yield return new WaitForSeconds(0.25f);
            Assert.That(player.transform.position.y, Is.LessThan(groundY + 0.1f),
                "Holding Space must not trigger a second jump on landing.");
            Release(keyboard.spaceKey);
        }

        [UnityTest]
        public IEnumerator HoldingWMovesTheCharacterTowardScreenTop()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
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
            yield return WaitForWilderness();
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
            Assert.That(Vector3.Dot(visual.forward, screenUpOnGround), Is.GreaterThan(0.8f));
        }

        [UnityTest]
        public IEnumerator SidewaysMovementBlendsIntoAimRelativeStrafe()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            yield return null;

            Animator animator = GameObject.Find("Player").GetComponentInChildren<Animator>();
            Assert.That(animator, Is.Not.Null);
            Set(gamepad.rightStick, Vector2.up);
            Set(gamepad.leftStick, Vector2.right);
            yield return new WaitForSeconds(0.25f);

            Component movement=GameObject.Find("Player").GetComponent("PlayerController");
            Vector3 velocity=(Vector3)movement.GetType().GetProperty("PlanarVelocity").GetValue(movement);
            Assert.That(Vector3.Dot(velocity,Camera.main.transform.right),Is.GreaterThan(.5f));
        }

        [UnityTest]
        public IEnumerator SideDodgeKeepsItsPoseAfterInvulnerabilityEnds()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            yield return null;

            Component player = GameObject.Find("Player").GetComponent("PlayerController");
            Animator animator = GameObject.Find("Player").GetComponentInChildren<Animator>();
            Assert.That(animator, Is.Not.Null);
            Set(gamepad.rightStick, Vector2.up);
            Set(gamepad.leftStick, Vector2.right);
            yield return null;
            Set(gamepad.buttonEast, 1f);
            yield return new WaitForSeconds(0.05f);
            Assert.That((bool)player.GetType().GetProperty("IsDodging").GetValue(player), Is.True);
            Assert.That(player.GetType().GetProperty("LastDodgeFacing").GetValue(player).ToString(),
                Is.EqualTo("Right"));
            Set(gamepad.buttonEast, 0f);

            yield return new WaitForSeconds(0.18f);
            Assert.That((bool)player.GetType().GetProperty("IsInvulnerable").GetValue(player), Is.False);
            Assert.That((bool)player.GetType().GetProperty("IsDodgeVisualActive").GetValue(player), Is.True,
                "The visual finish should read after the movement and invulnerability window.");
            Assert.That(player.GetType().GetProperty("LastDodgeFacing").GetValue(player).ToString(), Is.EqualTo("Right"));
        }

        [UnityTest]
        public IEnumerator InteractOpensAndClosesNearbyWorkbench()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            yield return null;

            GameObject player = GameObject.Find("Player");
            GameObject bench = GameObject.Find("Workbench");
            GameObject hud = GameObject.Find("Loop HUD");
            Assert.That(player, Is.Not.Null);
            Assert.That(bench, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            var controller=player.GetComponent<CharacterController>();controller.enabled=false;
            player.transform.position = bench.transform.position + Vector3.forward * 1.2f;
            controller.enabled=true;Physics.SyncTransforms();controller.Move(Vector3.down*.02f);
            yield return new WaitForSeconds(0.1f);
            AimAt(GameObject.Find("Workbench").transform.position);
            Press(keyboard.eKey);
            yield return new WaitForSeconds(0.1f);
            Release(keyboard.eKey);
            bool menuOpen = (bool)hud.GetComponent("LoopHud").GetType()
                .GetProperty("MenuOpen").GetValue(hud.GetComponent("LoopHud"));
            Assert.That(menuOpen, Is.True, "Interaction should open the workbench panel.");
            yield return null;
            AimAt(GameObject.Find("Workbench").transform.position);
            Press(keyboard.eKey);
            yield return new WaitForSeconds(0.1f);
            Release(keyboard.eKey);
            menuOpen = (bool)hud.GetComponent("LoopHud").GetType()
                .GetProperty("MenuOpen").GetValue(hud.GetComponent("LoopHud"));
            Assert.That(menuOpen, Is.False, "Interaction should close the workbench panel.");
        }
    }
}
