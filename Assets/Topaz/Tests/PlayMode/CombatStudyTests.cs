using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class CombatStudyTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator EnemyIsOnBakedNavigationAndHomeBlocksDamage()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject enemy = GameObject.Find("Enemy");
            GameObject player = GameObject.Find("Player");
            Assert.That(enemy, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, Is.True);

            Component vitality = player.GetComponent("PlayerVitality");
            Assert.That(vitality, Is.Not.Null);
            bool damaged = (bool)vitality.GetType().GetMethod("TryTakeDamage").Invoke(vitality, new object[] { 1 });
            Assert.That(damaged, Is.False, "The player starts in the safe homestead.");
            Assert.That(Health(vitality), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator EnemyFindsPathWhenPlayerLeavesHome()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject enemy = GameObject.Find("Enemy");
            GameObject player = GameObject.Find("Player");
            Assert.That(enemy, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            Teleport(player, new Vector3(8f, 0f, 1.5f));
            yield return new WaitForSeconds(0.45f);

            Assert.That(agent.isOnNavMesh, Is.True);
            Assert.That(agent.hasPath, Is.True);
            Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
        }

        [UnityTest]
        public IEnumerator AimedSwordSwingDamagesEnemyOutsideHome()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject enemy = GameObject.Find("Enemy");
            GameObject player = GameObject.Find("Player");
            Assert.That(enemy, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Component combatant = enemy.GetComponent("EnemyCombatant");
            int startingHealth = Health(combatant);

            Vector3 screenRight = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = enemy.transform.position - screenRight * 1.35f;
            controller.enabled = true;
            yield return new WaitForSeconds(0.1f);

            Set(gamepad.rightStick, Vector2.right);
            yield return null;
            Set(gamepad.rightTrigger, 1f);
            yield return new WaitForSeconds(0.35f);
            Set(gamepad.rightTrigger, 0f);

            Assert.That(Health(combatant), Is.LessThan(startingHealth),
                "A swing aimed at a nearby enemy should land during its active window.");
        }

        [UnityTest]
        public IEnumerator EnemyTelegraphsBeforeHittingStationaryPlayer()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject enemy = GameObject.Find("Enemy");
            GameObject player = GameObject.Find("Player");
            Component vitality = player.GetComponent("PlayerVitality");
            Vector3 screenRight = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
            Teleport(player, enemy.transform.position - screenRight * 1.35f);
            yield return new WaitForSeconds(0.12f);

            LineRenderer tell = GameObject.Find("Enemy Attack Tell").GetComponent<LineRenderer>();
            Assert.That(tell.enabled, Is.True, "The red arc must appear before the strike.");
            Assert.That(Health(vitality), Is.EqualTo(3));
            yield return new WaitForSeconds(0.7f);
            Assert.That(Health(vitality), Is.LessThan(3));
        }

        [UnityTest]
        public IEnumerator DodgePreventsDamageOutsideHome()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject player = GameObject.Find("Player");
            Component vitality = player.GetComponent("PlayerVitality");
            Component movement = player.GetComponent("FeelStudyPlayer");
            Teleport(player, new Vector3(-8f, 0f, 0f));
            yield return new WaitForSeconds(0.1f);

            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(0.05f);
            bool invulnerable = (bool)movement.GetType()
                .GetProperty("IsInvulnerable", BindingFlags.Public | BindingFlags.Instance)
                .GetValue(movement);
            Assert.That(invulnerable, Is.True);
            bool damaged = (bool)vitality.GetType().GetMethod("TryTakeDamage")
                .Invoke(vitality, new object[] { 1 });
            Assert.That(damaged, Is.False);
            Assert.That(Health(vitality), Is.EqualTo(3));
            Release(keyboard.spaceKey);
        }

        [UnityTest]
        public IEnumerator DodgeCancelsRecoveryButNotCommittedSwing()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject player = GameObject.Find("Player");
            Component movement = player.GetComponent("FeelStudyPlayer");
            Component combat = player.GetComponent("PlayerCombat");
            Assert.That(movement, Is.Not.Null);
            Assert.That(combat, Is.Not.Null);

            Set(gamepad.rightTrigger, 1f);
            yield return new WaitForSeconds(0.08f);
            Assert.That(Flag(combat, "IsAttackLocked"), Is.True);
            Assert.That(Flag(combat, "CanStartDodge"), Is.False);
            Set(gamepad.rightTrigger, 0f);
            yield return new WaitForSeconds(0.30f);

            bool reachedRecovery = false;
            for (int frame = 0; frame < 120; frame++)
            {
                if (Flag(combat, "IsAttackLocked") && Flag(combat, "CanStartDodge"))
                {
                    reachedRecovery = true;
                    break;
                }
                yield return null;
            }
            Assert.That(reachedRecovery, Is.True, "The sword should enter a dodge-cancellable recovery.");

            Set(gamepad.buttonEast, 1f);
            yield return new WaitForSeconds(0.05f);
            Assert.That(Flag(movement, "IsDodging"), Is.True);
            Assert.That(Flag(combat, "IsAttackLocked"), Is.False);
            Set(gamepad.buttonEast, 0f);
        }

        static void Teleport(GameObject player, Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
        }

        static int Health(Component component) => (int)component.GetType()
            .GetProperty("CurrentHealth", BindingFlags.Public | BindingFlags.Instance)
            .GetValue(component);

        static bool Flag(Component component, string name) => (bool)component.GetType()
            .GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            .GetValue(component);
    }
}
