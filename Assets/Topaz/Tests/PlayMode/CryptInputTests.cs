using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class CryptInputTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator RogueBoltStopsAtRaisedShieldAndGalleryCover()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Teleport(player, GameObject.Find("Home Crypt Entrance").transform.position);
            session.GetType().GetMethod("TryInteract").Invoke(session, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (GameObject.Find("Gallery Rogue") == null &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Component rogue = GameObject.Find("Gallery Rogue").GetComponent("EnemyCombatant");
            Component minion = GameObject.Find("Gallery Minion A").GetComponent("EnemyCombatant");
            while ((!rogue.gameObject.activeSelf || !minion.gameObject.activeSelf) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            while ((bool)session.GetType().GetProperty("SuppressAttack").GetValue(session) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            ((Behaviour)rogue).enabled = false;
            ((Behaviour)minion).enabled = false;
            Vector3 screenRight = Vector3.ProjectOnPlane(Camera.main.transform.right,
                Vector3.up).normalized;
            Teleport(player, rogue.transform.position - screenRight * 4.5f);
            Vector3 towardRogue = (rogue.transform.position - player.transform.position).normalized;
            Vector3 screenUp = Vector3.ProjectOnPlane(Camera.main.transform.forward,
                Vector3.up).normalized;
            Set(gamepad.rightStick, new Vector2(Vector3.Dot(towardRogue, screenRight),
                Vector3.Dot(towardRogue, screenUp)));
            Set(gamepad.leftTrigger, 1f);
            yield return new WaitForSeconds(.2f);
            Component combat = player.GetComponent("PlayerCombat");
            while (!(bool)combat.GetType().GetProperty("IsGuardRaised").GetValue(combat) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)combat.GetType().GetProperty("IsGuardRaised").GetValue(combat),
                Is.True);
            Component vitality = player.GetComponent("PlayerVitality");
            int health = (int)vitality.GetType().GetProperty("CurrentHealth").GetValue(vitality);
            object definition = rogue.GetType().GetField("definition",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rogue);
            object attack = definition.GetType().GetProperty("CrossbowAttack").GetValue(definition);
            object prefab = attack.GetType().GetProperty("BoltPrefab").GetValue(attack);
            FireBolt(prefab, attack, rogue, player.transform.position);
            yield return new WaitForSeconds(.55f);
            Assert.That((int)vitality.GetType().GetProperty("CurrentHealth").GetValue(vitality),
                Is.EqualTo(health));
            Assert.That((int)session.GetType().GetProperty("ShieldExperience").GetValue(session),
                Is.GreaterThan(0));
            Assert.That((bool)rogue.GetType().GetProperty("HasHitReaction").GetValue(rogue),
                Is.False, "A distant blocked bolt must not stagger its shooter.");

            Set(gamepad.leftTrigger, 0f);
            Teleport(player, rogue.transform.position + new Vector3(-3f, 0f, -6f));
            yield return null;
            FireBolt(prefab, attack, rogue, player.transform.position);
            yield return new WaitForSeconds(.65f);
            Assert.That((int)vitality.GetType().GetProperty("CurrentHealth").GetValue(vitality),
                Is.EqualTo(health), "The collidable gallery pillar must stop the bolt.");
        }

        static void FireBolt(object prefab, object attack, Component shooter,
            Vector3 target)
        {
            Component bolt = Object.Instantiate((Component)prefab);
            Vector3 direction = Vector3.ProjectOnPlane(target - shooter.transform.position,
                Vector3.up).normalized;
            Vector3 origin = shooter.transform.position + Vector3.up + direction * .5f;
            bolt.GetType().GetMethod("Launch").Invoke(bolt,
                new[] { attack, origin, direction, (object)3, shooter, null, false, 0f });
        }

        [UnityTest]
        public IEnumerator CrossbowGamepadAimAssistsAVisibleTargetAndReloadBlocksSecondShot()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Teleport(player, GameObject.Find("Home Crypt Entrance").transform.position);
            session.GetType().GetMethod("TryInteract").Invoke(session, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (GameObject.Find("Gallery Rogue") == null &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Component minion = GameObject.Find("Gallery Minion A").GetComponent("EnemyCombatant");
            Component rogue = GameObject.Find("Gallery Rogue").GetComponent("EnemyCombatant");
            while ((!minion.gameObject.activeSelf || !rogue.gameObject.activeSelf) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            ((Behaviour)minion).enabled = false;
            ((Behaviour)rogue).enabled = false;
            object equipment = session.GetType().GetProperty("Equipped").GetValue(session);
            equipment.GetType().GetField("weaponId").SetValue(equipment, "gear.crypt.crossbow");
            equipment.GetType().GetField("offhandId").SetValue(equipment, null);
            Vector3 right = Vector3.ProjectOnPlane(Camera.main.transform.right,
                Vector3.up).normalized;
            Teleport(player, minion.transform.position - right * 4.5f);
            int before = (int)minion.GetType().GetProperty("CurrentHealth").GetValue(minion);
            Set(gamepad.rightStick, new Vector2(.996f, .087f));
            yield return null;
            Set(gamepad.rightTrigger, 1f);
            yield return null;
            Set(gamepad.rightTrigger, 0f);
            yield return new WaitForSeconds(.65f);
            int after = (int)minion.GetType().GetProperty("CurrentHealth").GetValue(minion);
            Assert.That(after, Is.LessThan(before),
                "A target inside the narrow gamepad assist cone should take a bolt hit.");
            Component combat = player.GetComponent("PlayerCombat");
            Assert.That((bool)combat.GetType().GetProperty("IsCrossbowReloading").GetValue(combat),
                Is.True);
            Component ability = player.GetComponent("CrossbowPlayerAbility");
            object weapon = session.GetType().GetProperty("CurrentWeapon").GetValue(session);
            object attack = weapon.GetType().GetProperty("CrossbowAttack").GetValue(weapon);
            Assert.That((bool)ability.GetType().GetMethod("TryAim").Invoke(ability,
                new[] { attack }), Is.False);
            Assert.That((int)session.GetType().GetMethod("SkillExperienceCenti")
                .Invoke(session, new object[] { "crossbows" }), Is.GreaterThan(0));
            float reloadBeforeDodge = (float)combat.GetType().GetProperty(
                "CrossbowReloadProgress").GetValue(combat);
            Set(gamepad.buttonEast, 1f);
            yield return null;
            Set(gamepad.buttonEast, 0f);
            yield return null;
            Assert.That((bool)player.GetComponent("FeelStudyPlayer").GetType()
                .GetProperty("IsDodging").GetValue(player.GetComponent("FeelStudyPlayer")),
                Is.True);
            Assert.That((float)combat.GetType().GetProperty("CrossbowReloadProgress")
                .GetValue(combat), Is.LessThan(reloadBeforeDodge),
                "Dodge should restart the unfinished automatic reload.");
        }

        [UnityTest]
        public IEnumerator EquippedStaffCastsWithGamepadAttack()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Teleport(player, GameObject.Find("Home Crypt Entrance").transform.position);
            session.GetType().GetMethod("TryInteract").Invoke(session, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((string)session.GetType().GetProperty("CurrentRegionId").GetValue(session) !=
                   "dungeon.home-crypt" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((string)session.GetType().GetProperty("CurrentRegionId").GetValue(session),
                Is.EqualTo("dungeon.home-crypt"));

            object equipment = session.GetType().GetProperty("Equipped").GetValue(session);
            equipment.GetType().GetField("weaponId").SetValue(equipment, "gear.crypt.staff");
            equipment.GetType().GetField("offhandId").SetValue(equipment, null);
            Component enemy = GameObject.Find("Home Crypt").transform.Find("Gallery Minion A")
                .GetComponent("EnemyCombatant");
            while (!enemy.gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
                yield return null;
            ((Behaviour)enemy).enabled = false;
            Vector3 right = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
            Teleport(player, enemy.transform.position - right * 4.5f);
            int before = (int)enemy.GetType().GetProperty("CurrentHealth").GetValue(enemy);
            Set(gamepad.rightStick, Vector2.right);
            yield return null;
            Set(gamepad.rightTrigger, 1f);
            yield return null;
            Set(gamepad.rightTrigger, 0f);
            yield return new WaitForSeconds(1f);
            int afterGamepad = (int)enemy.GetType().GetProperty("CurrentHealth").GetValue(enemy);
            Assert.That(afterGamepad, Is.LessThan(before));
            Teleport(player, GameObject.Find("Return Door").transform.position);
            session.GetType().GetMethod("TryInteract").Invoke(session, null);
            while ((string)session.GetType().GetProperty("CurrentRegionId").GetValue(session) !=
                   "home" && Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        static void Teleport(GameObject player, Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
        }
    }
}
