using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class MiningAndHomeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator HomeSceneryIsGatherableAndCircleIsClear()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            MonoBehaviour[] components = UnityEngine.Object.FindObjectsByType<MonoBehaviour>();
            Component circle = components.First(value => value.GetType().Name == "SafeZone");
            Component[] trees = components.Where(value => value.GetType().Name == "HarvestTree")
                .Cast<Component>().ToArray();
            Component[] rocks = components.Where(value => value.GetType().Name == "MiningRock")
                .Cast<Component>().ToArray();
            Assert.That(trees.Length, Is.GreaterThanOrEqualTo(17));
            Assert.That(rocks.Length, Is.GreaterThanOrEqualTo(6));
            var ids = new HashSet<string>();
            foreach (Component tree in trees)
            {
                Assert.That((bool)Property(tree, "IsAvailable"), Is.True);
                Assert.That(ids.Add((string)Property(tree, "StableObjectId")), Is.True);
                Assert.That((bool)Invoke(circle, "Contains", tree.transform.position), Is.False);
            }
            foreach (Component rock in rocks)
            {
                Assert.That((bool)Property(rock, "IsAvailable"), Is.True);
                Assert.That(ids.Add((string)Property(rock, "StableObjectId")), Is.True);
                Assert.That((bool)Invoke(circle, "Contains", rock.transform.position), Is.False);
            }
            foreach (Transform scenery in GameObject.Find("KayKit Art Study").transform)
            {
                if (scenery.name.StartsWith("Tree_") || scenery.name.StartsWith("Home Tree "))
                    Assert.That(scenery.GetComponent("HarvestTree"), Is.Not.Null,
                        scenery.name + " should be gatherable.");
                if (scenery.name.StartsWith("Rock_"))
                    Assert.That(scenery.GetComponent("MiningRock"), Is.Not.Null,
                        scenery.name + " should be gatherable.");
            }
            Transform grass = GameObject.Find("Grass Clumps").transform;
            Assert.That(grass.childCount, Is.GreaterThanOrEqualTo(80));
            foreach (Transform clump in grass)
                Assert.That((bool)Invoke(circle, "Contains", clump.position), Is.False);
            Assert.That(GameObject.Find("Firewood 1"), Is.Null);
        }

        [UnityTest]
        public IEnumerator AttackAutoEquipsPickaxeForNearbyRock()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component rock = GameObject.Find("Mining Rock 01").GetComponent("MiningRock");
            Teleport(player, rock.transform.position + Vector3.back * 1.25f);
            yield return null;
            Set(gamepad.rightTrigger, 1f);
            yield return new WaitForSeconds(.55f);
            Set(gamepad.rightTrigger, 0f);
            Assert.That((int)Property(rock, "StrikesRemaining"), Is.EqualTo(1));
            Assert.That((string)Property(session, "EquippedToolName"), Is.EqualTo("Pickaxe"));
            yield return new WaitForSeconds(.55f);
            Assert.That((string)Property(session, "EquippedToolName"), Is.EqualTo("Sword"));
        }

        [UnityTest]
        public IEnumerator TreeInteractionRestoresSelectedPickaxeAndSavesToolBelt()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Assert.That((bool)Invoke(session, "SelectManualTool", "pickaxe"), Is.True);
            Assert.That((string)Property(session, "EquippedToolName"), Is.EqualTo("Pickaxe"));
            GameObject tree = GameObject.Find("Authored Tree 01");
            Teleport(player, tree.transform.position + Vector3.back * 1.2f);
            yield return null;
            Assert.That((bool)Invoke(session, "TryInteract"), Is.True);
            Assert.That((string)Property(session, "EquippedToolName"),
                Is.EqualTo("Logging Axe"));
            yield return new WaitForSeconds(0.85f);
            Assert.That((string)Property(session, "EquippedToolName"), Is.EqualTo("Pickaxe"));
            Invoke(session, "FlushCurrent");
            object profile = Invoke(Field(session, "_repository"), "Load");
            object character = Invoke(profile, "Character", Property(session, "ActiveCharacterId"));
            Assert.That((string)Field(character, "selectedTool"), Is.EqualTo("pickaxe"));
            Assert.That((string)Field(character, "pickaxeId"),
                Is.EqualTo("gear.pickaxe.starter"));
        }

        [UnityTest]
        public IEnumerator PickaxeMinesTwiceThenRockReturnsAfterWorldTime()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component combat = player.GetComponent("PlayerCombat");
            Component rock = GameObject.Find("Mining Rock 01").GetComponent("MiningRock");
            Assert.That((int)Property(rock, "StrikesRemaining"), Is.EqualTo(2));
            Teleport(player, rock.transform.position + Vector3.back * 1.25f);
            yield return null;

            Assert.That((bool)Invoke(combat, "TryStartMining", rock), Is.True);
            Assert.That((string)Property(session, "EquippedToolName"), Is.EqualTo("Pickaxe"));
            yield return new WaitForSeconds(1.05f);
            Assert.That((int)Property(rock, "StrikesRemaining"), Is.EqualTo(1));
            Assert.That((string)Property(session, "EquippedToolName"), Is.EqualTo("Sword"));
            Assert.That((bool)Invoke(combat, "TryStartMining", rock), Is.True);
            yield return new WaitForSeconds(1.05f);
            Assert.That((bool)Property(rock, "IsAvailable"), Is.False);
            Assert.That((int)Property(session, "MiningExperience"), Is.EqualTo(10));
            Assert.That((int)Property(session, "PickupCount"), Is.EqualTo(2));

            object data = Field(session, "_data");
            Set(data, "worldHours", (double)Property(session, "WorldHours") + 72d);
            Invoke(rock, "RefreshForTime");
            Assert.That((bool)Property(rock, "IsAvailable"), Is.True);
            Assert.That((int)Property(rock, "StrikesRemaining"), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator AnvilPlacementSpendsMaterialsAndRemovalRefundsWithoutLoss()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            object pack = Field(session, "_backpack");
            object stone = Property(session, "StoneItem");
            object iron = Property(session, "IronItem");
            Assert.That((int)Invoke(pack, "Add", stone, 6), Is.EqualTo(6));
            Assert.That((int)Invoke(pack, "Add", iron, 2), Is.EqualTo(2));
            Assert.That((bool)Invoke(session, "BeginHomeBuild",
                "structure.blacksmith_anvil"), Is.True);

            Component builds = (Component)Field(session, "homeBuilds");
            Set(builds, "_position", new Vector3(3f, 0f, 3f));
            Invoke(builds, "ConfirmPlacement");
            IList structures = (IList)Field(Field(session, "_data"), "structures");
            Assert.That(structures.Cast<object>().Count(value =>
                (string)Field(value, "definitionId") == "structure.blacksmith_anvil"), Is.EqualTo(1));
            Assert.That((int)Property(session, "StoneCount"), Is.Zero);
            Assert.That((int)Property(session, "IronCount"), Is.Zero);

            object wood = Invoke(session, "Item", "material.wood");
            Assert.That((int)Invoke(pack, "Add", wood, 320), Is.EqualTo(320));
            Assert.That((bool)Invoke(session, "BeginHomeEdit"), Is.True);
            Set(builds, "_selected", structures.Cast<object>().First(value =>
                (string)Field(value, "definitionId") == "structure.blacksmith_anvil"));
            Invoke(builds, "RemoveSelected");
            Assert.That(structures.Cast<object>().Any(value =>
                (string)Field(value, "definitionId") == "structure.blacksmith_anvil"), Is.False);
            Assert.That((int)Property(session, "PickupCount"), Is.EqualTo(2),
                "A full backpack leaves both material refunds in the World.");
            Invoke(session, "FlushCurrent");
            object repository = Field(session, "_repository");
            object profile = Invoke(repository, "Load");
            object world = Invoke(profile, "World", Property(session, "ActiveWorldId"));
            Assert.That(((IList)Field(world, "structures")).Cast<object>().Any(value =>
                (string)Field(value, "definitionId") == "structure.blacksmith_anvil"), Is.False);
            Assert.That(((IList)Field(world, "pickups")).Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator StonePathUsesChestMaterialsOnlyAfterConfirmedPlacement()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            object data = Field(session, "_data");
            IList structures = (IList)Field(data, "structures");
            Type recordType = Type.GetType("Topaz.LoopStudy.StructureStateRecord, Assembly-CSharp", true);
            object chestRecord = Activator.CreateInstance(recordType);
            Set(chestRecord, "instanceId", "test-home-chest");
            Set(chestRecord, "definitionId", "structure.storage_chest");
            Set(chestRecord, "x", 0f);
            Set(chestRecord, "z", -2f);
            structures.Add(chestRecord);
            Component chest = (Component)Field(session, "chest");
            Invoke(chest, "Bind", chestRecord);
            object chestInventory = Property(chest, "Inventory");
            object stone = Property(session, "StoneItem");
            Assert.That((int)Invoke(chestInventory, "Add", stone, 1), Is.EqualTo(1));
            Component builds = (Component)Field(session, "homeBuilds");

            Assert.That((bool)Invoke(session, "BeginHomeBuild", "structure.stone_path"), Is.True);
            Invoke(builds, "Cancel");
            Assert.That((int)Property(session, "StoneCount"), Is.EqualTo(1));
            Assert.That(structures.Cast<object>().Count(value =>
                (string)Field(value, "definitionId") == "structure.stone_path"), Is.Zero);

            Assert.That((bool)Invoke(session, "BeginHomeBuild", "structure.stone_path"), Is.True);
            Set(builds, "_position", new Vector3(3f, 0f, 3f));
            Invoke(builds, "ConfirmPlacement");
            Assert.That(structures.Cast<object>().Count(value =>
                (string)Field(value, "definitionId") == "structure.stone_path"), Is.EqualTo(1));
            Assert.That((int)Property(session, "StoneCount"), Is.Zero);
            Invoke(session, "FlushCurrent");
            object profile = Invoke(Field(session, "_repository"), "Load");
            object world = Invoke(profile, "World", Property(session, "ActiveWorldId"));
            Assert.That(((IList)Field(world, "structures")).Cast<object>().Count(value =>
                (string)Field(value, "definitionId") == "structure.stone_path"), Is.EqualTo(1));
        }

        static void Teleport(GameObject player, Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.position = position;
            if (controller != null) controller.enabled = true;
        }

        static object Property(object target, string name) =>
            target.GetType().GetProperty(name).GetValue(target);
        static object Field(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic).GetValue(target);
        static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic).SetValue(target, value);
        static object Invoke(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic).Invoke(target, args);
    }
}
