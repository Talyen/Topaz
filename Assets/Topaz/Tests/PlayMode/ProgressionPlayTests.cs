using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class ProgressionPlayTests : InputTestFixture
    {
        static object Call(object target, string method, params object[] arguments) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public)
                .Invoke(target, arguments);

        static object Property(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
                .GetValue(target);

        static object Field(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.Public).GetValue(target);

        static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.Public).SetValue(target, value);

        static void Teleport(GameObject player, Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
        }

        static void EquipAxeForTest(Component session, Component combat, Component enemy)
        {
            SetField(Property(session, "Equipped"), "weaponId", "gear.axe.twohanded.starter");
            SetField(Property(session, "Equipped"), "offhandId", null);
            SetField(combat, "_strikeWeapon", Property(session, "CurrentWeapon"));
            Vector3 direction = enemy.transform.position - combat.transform.position;
            direction.y = 0f;
            SetField(combat, "_lockedDirection", direction.normalized);
        }

        static void StrikeEnemy(Component combat, Component enemy) =>
            combat.GetType().GetMethod("TryDamage", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(combat, new object[] { enemy });

        [UnityTest]
        public IEnumerator TalentCanBeLearnedInJournalAndOneStrikeMiningKeepsCompletionXp()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component hud = UnityEngine.Object.FindFirstObjectByType(Type.GetType(
                "Topaz.LoopStudy.LoopHud, Assembly-CSharp")) as Component;
            Assert.That(session, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Call(session, "RecordSkillCompletion", "mining", 10, 1);
            Assert.That((int)Call(session, "SkillLevel", "mining"), Is.EqualTo(2));
            Assert.That((bool)Call(session, "TryLearnTalent", "mining", "mining.heavy-pick"), Is.True);
            Assert.That((bool)Call(session, "HasTalent", "mining", "mining.heavy-pick"), Is.True);
            Assert.That((bool)Call(session, "TryLearnTalent", "mining", "mining.heavy-pick"), Is.False);

            ((UnityEngine.UI.Button)Field(hud, "skillsTabButton")).onClick.Invoke();
            Component journal = hud.GetComponent("SkillsJournalView");
            Assert.That((bool)Property(journal, "IsOpen"), Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.Null,
                "Keyboard and gamepad need a focused control when the Skills page opens.");
            GameObject initialFocus = EventSystem.current.currentSelectedGameObject;
            Set(gamepad.dpad.down, 1f);
            yield return null;
            Set(gamepad.dpad.down, 0f);
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.Not.EqualTo(initialFocus), "Gamepad navigation must move through the Skills page.");
            Call(hud, "ClosePanels");

            Component rock = GameObject.Find("Mining Rock 01").GetComponent("MiningRock");
            Vector3 strikePosition = rock.transform.position + Vector3.back * 1.25f;
            Assert.That((bool)Call(rock, "TryStrike", strikePosition, Vector3.forward, 2.1f, 80f),
                Is.True);
            Assert.That((bool)Property(rock, "IsAvailable"), Is.False);
            Assert.That((int)Call(session, "SkillExperienceCenti", "mining"), Is.EqualTo(1750),
                "One-strike mining should still earn the node's full scaled completion XP.");
        }

        [UnityTest]
        public IEnumerator ThirdTalentWaitsForHomeBeforeItCanReplaceAnActiveTalent()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Call(session, "RecordSkillCompletion", "swords", 52, 1);
            Assert.That((int)Call(session, "SkillLevel", "swords"), Is.EqualTo(5));
            Assert.That((bool)Call(session, "TryLearnTalent", "swords", "swords.footwork"), Is.True);
            Assert.That((bool)Call(session, "TryLearnTalent", "swords", "swords.flow"), Is.True);
            Call(session, "RecordSkillCompletion", "swords", 60, 10);
            Assert.That((int)Call(session, "SkillLevel", "swords"), Is.EqualTo(8));
            Assert.That((bool)Call(session, "TryLearnTalent", "swords", "swords.wide-cut"), Is.True);
            Assert.That((bool)Call(session, "HasTalent", "swords", "swords.wide-cut"), Is.False);

            Vector3 homePosition = player.transform.position;
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = homePosition + Vector3.right * 30f;
            controller.enabled = true;
            Assert.That((bool)Property(session, "IsAtHome"), Is.False);
            Assert.That((bool)Call(session, "TrySetTalentActive", "swords", "swords.flow", false),
                Is.False);
            controller.enabled = false;
            player.transform.position = homePosition;
            controller.enabled = true;
            Assert.That((bool)Property(session, "IsAtHome"), Is.True);
            Assert.That((bool)Call(session, "TrySetTalentActive", "swords", "swords.flow", false),
                Is.True);
            Assert.That((bool)Call(session, "TrySetTalentActive", "swords", "swords.wide-cut", true),
                Is.True);
            Assert.That((bool)Call(session, "HasTalent", "swords", "swords.wide-cut"), Is.True);
        }

        [UnityTest]
        public IEnumerator BleedDamagesEnemyWithoutGrantingWeaponXp()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            Component enemy = GameObject.Find("Enemy").GetComponent("EnemyCombatant");
            int before = (int)Property(enemy, "CurrentHealth");
            Call(enemy, "ApplyBleed", 1, 2f, 1.5f);
            yield return new WaitForSeconds(1.6f);
            Assert.That((int)Property(enemy, "CurrentHealth"), Is.EqualTo(before - 1));
            Assert.That((int)Call(session, "SkillExperienceCenti", "axes"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator AxeBleedTriggeredByAHitKeepsTickDamageOutOfSkillXp()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component combat = player.GetComponent("PlayerCombat");
            Component enemy = GameObject.Find("Enemy").GetComponent("EnemyCombatant");
            Call(session, "RecordSkillCompletion", "axes", 10, 1);
            Assert.That((bool)Call(session, "TryLearnTalent", "axes", "axes.bleed"), Is.True);
            Vector3 home = player.transform.position;
            Teleport(player, enemy.transform.position + Vector3.back * 1.3f);
            EquipAxeForTest(session, combat, enemy);
            int before = (int)Property(enemy, "CurrentHealth");
            StrikeEnemy(combat, enemy);
            int afterHit = (int)Property(enemy, "CurrentHealth");
            int earned = (int)Call(session, "SkillExperienceCenti", "axes");
            Assert.That(afterHit, Is.LessThan(before));
            Assert.That(earned, Is.GreaterThan(1000));
            Teleport(player, home);
            yield return new WaitForSeconds(1.6f);
            Assert.That((int)Property(enemy, "CurrentHealth"), Is.EqualTo(afterHit - 1));
            Assert.That((int)Call(session, "SkillExperienceCenti", "axes"), Is.EqualTo(earned));
        }

        [UnityTest]
        public IEnumerator DamageTriggeredRageIncreasesOnlyTheNextAxeHit()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component combat = player.GetComponent("PlayerCombat");
            Component vitality = player.GetComponent("PlayerVitality");
            Component enemy = GameObject.Find("Enemy").GetComponent("EnemyCombatant");
            Call(session, "RecordSkillCompletion", "axes", 10, 1);
            Assert.That((bool)Call(session, "TryLearnTalent", "axes", "axes.rage"), Is.True);
            Teleport(player, enemy.transform.position + Vector3.back * 1.3f);
            EquipAxeForTest(session, combat, enemy);
            Assert.That((bool)Call(vitality, "TryTakeDirectedDamage", 4,
                enemy.transform.position, enemy), Is.True);
            int before = (int)Property(enemy, "CurrentHealth");
            StrikeEnemy(combat, enemy);
            int baseAttack = (int)Field(Property(session, "Stats"), "Attack");
            Assert.That(before - (int)Property(enemy, "CurrentHealth"),
                Is.EqualTo(Mathf.RoundToInt(baseAttack * 1.2f)));
            Call(combat, "ClearTemporaryProgression");
            Assert.That((float)Field(combat, "_rageUntil"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator StewardshipChangesOnlyTheFelledTreesWorldDeadline()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            Component tree = GameObject.Find("Authored Tree 01").GetComponent("HarvestTree");
            Call(session, "RecordSkillCompletion", "logging", 10, 1);
            Assert.That((bool)Call(session, "TryLearnTalent", "logging", "logging.stewardship"),
                Is.True);
            Vector3 position = tree.transform.position + Vector3.back * 1.2f;
            for (int i = 0; i < 3; i++)
                Assert.That((bool)Call(tree, "TryChop", position, Vector3.forward, 2.1f, 90f),
                    Is.True);
            object node = Call(session, "GetOrCreateNodeState", Property(tree, "StableObjectId"));
            double deadline = (double)Field(node, "readyAtWorldHours");
            Assert.That(deadline - (double)Property(session, "WorldHours"),
                Is.EqualTo(60d).Within(.02d));
        }
    }
}
