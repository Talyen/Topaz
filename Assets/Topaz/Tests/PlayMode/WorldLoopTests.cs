using System;
using System.Collections;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class WorldLoopTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator InventoryKeyTogglesSixteenSlotPanel()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component hud = GameObject.Find("Loop HUD").GetComponent("LoopHud");
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            var slots = (System.Collections.IList)Read<object>(session, "BackpackSlots");
            Assert.That(slots.Count, Is.EqualTo(16));
            Press(keyboard.iKey);
            yield return null;
            Assert.That(Read<bool>(hud, "MenuOpen"), Is.True);
            Release(keyboard.iKey);
            yield return null;
            Press(keyboard.iKey);
            yield return null;
            Assert.That(Read<bool>(hud, "MenuOpen"), Is.False);
            Release(keyboard.iKey);
        }

        [UnityTest]
        public IEnumerator FullBackpackDefersHarvestWithoutAwardingLoggingExperience()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            GameObject tree = GameObject.Find("Authored Tree 01");
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            object backpack = session.GetType().GetField("_backpack",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            object wood = session.GetType().GetField("wood",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            int accepted = (int)backpack.GetType().GetMethod("Add").Invoke(backpack, new[] { wood, (object)320 });
            Assert.That(accepted, Is.EqualTo(320));
            Teleport(player, tree.transform.position + Vector3.back * 1.3f);
            for (int i = 0; i < 2; i++)
                Assert.That(Call<bool>(harvest, "TryChop", player.transform.position,
                    Vector3.forward, 2.1f, 90f), Is.True);
            Assert.That(Call<bool>(harvest, "TryChop", player.transform.position,
                Vector3.forward, 2.1f, 90f), Is.False);
            Assert.That(Read<int>(harvest, "ChopsRemaining"), Is.EqualTo(1));
            Assert.That(Read<int>(session, "LoggingExperience"), Is.Zero);
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(320));
        }

        [UnityTest]
        public IEnumerator GamepadAxeSwingChopsInAimedDirection()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject player = GameObject.Find("Player");
            GameObject tree = GameObject.Find("Authored Tree 01");
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            Teleport(player, tree.transform.position + Vector3.back * 1.3f);
            Set(gamepad.buttonNorth, 1f);
            yield return null;
            Set(gamepad.buttonNorth, 0f);
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Axe"));

            Vector3 towardTree = (tree.transform.position - player.transform.position).normalized;
            Vector3 right = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
            Set(gamepad.rightStick, new Vector2(Vector3.Dot(towardTree, right),
                Vector3.Dot(towardTree, forward)));
            yield return null;
            Set(gamepad.rightTrigger, 1f);
            yield return new WaitForSeconds(.42f);
            Set(gamepad.rightTrigger, 0f);

            Assert.That(Read<int>(harvest, "ChopsRemaining"), Is.EqualTo(2));
            Assert.That(Read<int>(session, "WoodCount"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator LoggingAwardsOnlyOnCompletedHarvestAndTreeReturnsOnDayFour()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject player = GameObject.Find("Player");
            GameObject tree = GameObject.Find("Authored Tree 01");
            Assert.That(player, Is.Not.Null);
            Assert.That(tree, Is.Not.Null);
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            Assert.That(session, Is.Not.Null);
            Assert.That(harvest, Is.Not.Null);
            Teleport(player, tree.transform.position + Vector3.back * 1.3f);
            Vector3 towardTree = Vector3.forward;
            for (int chop = 0; chop < 2; chop++)
            {
                Assert.That(Call<bool>(harvest, "TryChop", player.transform.position,
                    towardTree, 2.1f, 90f), Is.True);
                Assert.That(Read<int>(session, "WoodCount"), Is.Zero);
                Assert.That(Read<int>(session, "LoggingExperience"), Is.Zero);
            }

            Assert.That(Call<bool>(harvest, "TryChop", player.transform.position,
                towardTree, 2.1f, 90f), Is.True);
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(6));
            Assert.That(Read<int>(session, "LoggingExperience"), Is.EqualTo(5));
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False);

            Teleport(player, new Vector3(1.5f, 0, -.1f));
            for (int day = 2; day <= 3; day++)
            {
                Assert.That(Call<bool>(session, "TryInteract"), Is.True);
                Assert.That(Read<int>(session, "CurrentDay"), Is.EqualTo(day));
                Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False);
            }
            Assert.That(Call<bool>(session, "TryInteract"), Is.True);
            Assert.That(Read<int>(session, "CurrentDay"), Is.EqualTo(4));
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.True);
        }

        [UnityTest]
        public IEnumerator CraftingConsumesWoodAndCreatesOnePendingChest()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject player = GameObject.Find("Player");
            GameObject tree = GameObject.Find("Authored Tree 01");
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            Teleport(player, tree.transform.position + Vector3.back * 1.3f);
            for (int i = 0; i < 3; i++)
                Assert.That(Call<bool>(harvest, "TryChop", player.transform.position,
                    Vector3.forward, 2.1f, 90f), Is.True);
            Teleport(player, Vector3.zero);

            Assert.That(Call<bool>(session, "TryCraftChest"), Is.True);
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(3));
            Assert.That(Read<bool>(session, "PendingChest"), Is.True);
            Assert.That(Call<bool>(session, "TryCraftChest"), Is.False);
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator PlacedChestStoresWoodAndSurvivesSaveRoundTrip()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject player = GameObject.Find("Player");
            GameObject tree = GameObject.Find("Authored Tree 01");
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            Teleport(player, tree.transform.position + Vector3.back * 1.3f);
            for (int i = 0; i < 3; i++)
                Call<bool>(harvest, "TryChop", player.transform.position,
                    Vector3.forward, 2.1f, 90f);
            Teleport(player, Vector3.zero);
            Assert.That(Call<bool>(session, "TryCraftChest"), Is.True);

            FieldInfo previewPosition = session.GetType().GetField("_previewPosition",
                BindingFlags.Instance | BindingFlags.NonPublic);
            previewPosition.SetValue(session, new Vector3(0, 0, 1.5f));
            session.GetType().GetMethod("TryPlaceChest", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(session, null);
            Assert.That(Read<bool>(session, "ChestPlaced"), Is.True);
            Assert.That(Read<bool>(session, "PendingChest"), Is.False);
            session.GetType().GetMethod("DepositAllItems").Invoke(session, null);
            Assert.That(Read<int>(session, "WoodCount"), Is.Zero);
            Assert.That(Read<int>(session, "ChestWood"), Is.EqualTo(3));

            object repository = session.GetType().GetField("_repository",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            string savePath = (string)repository.GetType().GetProperty("PathOnDisk").GetValue(repository);
            Assert.That(savePath, Does.StartWith(Application.temporaryCachePath),
                "Editor tests must never use the standalone player save directory.");
            object saved = repository.GetType().GetMethod("Load").Invoke(repository, null);
            Assert.That((bool)saved.GetType().GetField("pendingChest").GetValue(saved), Is.False);
            var structures = (System.Collections.IList)saved.GetType().GetField("structures").GetValue(saved);
            Assert.That(structures.Count, Is.EqualTo(1));
            object placed = structures[0];
            Assert.That((string)placed.GetType().GetField("instanceId").GetValue(placed), Is.Not.Empty);
            var slots = (System.Collections.IList)placed.GetType().GetField("slots").GetValue(placed);
            Assert.That(slots.Count, Is.EqualTo(12));
            Assert.That((int)slots[0].GetType().GetField("count").GetValue(slots[0]), Is.EqualTo(3));
        }

        [Test]
        public void VersionOneSaveMigratesStacksWithoutLosingWood()
        {
            string directory = Path.Combine(Path.GetTempPath(), "TopazMigration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string json = "{\"version\":1,\"day\":4,\"loggingExperience\":5," +
                    "\"equippedTool\":\"axe\",\"backpack\":[{\"itemId\":\"material.wood\",\"count\":25}]," +
                    "\"nodes\":[],\"structures\":[{\"instanceId\":\"chest1\",\"definitionId\":\"structure.storage_chest\"," +
                    "\"x\":1,\"z\":1,\"woodStored\":23}]}";
                File.WriteAllText(Path.Combine(directory, "topaz-save.json"), json);
                Type type = Type.GetType("Topaz.LoopStudy.SaveRepository, Assembly-CSharp", true);
                object repository = Activator.CreateInstance(type, directory);
                object migrated = type.GetMethod("Load").Invoke(repository, null);
                Assert.That((int)migrated.GetType().GetField("version").GetValue(migrated), Is.EqualTo(2));
                var backpack = (System.Collections.IList)migrated.GetType().GetField("backpackSlots").GetValue(migrated);
                Assert.That(backpack.Count, Is.EqualTo(2));
                Assert.That((int)backpack[0].GetType().GetField("count").GetValue(backpack[0]), Is.EqualTo(20));
                Assert.That((int)backpack[1].GetType().GetField("count").GetValue(backpack[1]), Is.EqualTo(5));
                var structures = (System.Collections.IList)migrated.GetType().GetField("structures").GetValue(migrated);
                var chestSlots = (System.Collections.IList)structures[0].GetType().GetField("slots").GetValue(structures[0]);
                Assert.That(chestSlots.Count, Is.EqualTo(2));
                type.GetMethod("Save").Invoke(repository, new[] { migrated });
                object reloaded = type.GetMethod("Load").Invoke(repository, null);
                Assert.That((int)reloaded.GetType().GetField("version").GetValue(reloaded), Is.EqualTo(2));
            }
            finally { Directory.Delete(directory, true); }
        }

        static T Read<T>(Component target, string property) => (T)target.GetType()
            .GetProperty(property, BindingFlags.Instance | BindingFlags.Public).GetValue(target);

        static T Call<T>(Component target, string method, params object[] arguments) => (T)target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.Public).Invoke(target, arguments);

        static void Teleport(GameObject player, Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
        }
    }
}
