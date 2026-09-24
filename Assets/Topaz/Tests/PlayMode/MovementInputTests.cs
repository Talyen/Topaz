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
        public IEnumerator SpaceJumpsOnceAndReturnsToGround()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return new WaitForSeconds(0.1f);

            GameObject player = GameObject.Find("Player");
            Animator animator = player.GetComponentInChildren<Animator>();
            float groundY = player.transform.position.y;
            Component movement = player.GetComponent("FeelStudyPlayer");
            Assert.That((bool)movement.GetType().GetProperty("IsAirborne").GetValue(movement), Is.False);
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(0.12f);
            Assert.That(player.transform.position.y, Is.GreaterThan(groundY + 0.2f));
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"), Is.True);
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
        public IEnumerator SidewaysMovementBlendsIntoAimRelativeStrafe()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            Animator animator = GameObject.Find("Player").GetComponentInChildren<Animator>();
            Assert.That(animator, Is.Not.Null);
            Set(gamepad.rightStick, Vector2.up);
            Set(gamepad.leftStick, Vector2.right);
            yield return new WaitForSeconds(0.25f);

            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
            Assert.That(animator.GetFloat("MoveX"), Is.GreaterThan(0.5f));
            Assert.That(Mathf.Abs(animator.GetFloat("MoveY")), Is.LessThan(0.35f));
        }

        [UnityTest]
        public IEnumerator SideDodgeKeepsItsPoseAfterInvulnerabilityEnds()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            Component player = GameObject.Find("Player").GetComponent("FeelStudyPlayer");
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
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("DodgeRight"), Is.True,
                "A rightward dodge should play KayKit's right dodge pose.");
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
