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
        public IEnumerator InteractWithTreeChopsAndRestoresPreviousWeapon()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            GameObject tree = GameObject.Find("Authored Tree 01");
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            Teleport(player, tree.transform.position + Vector3.back * 1.2f);
            yield return null;

            object[] cue = { null, null };
            Assert.That((bool)session.GetType().GetMethod("TryGetInteraction")
                .Invoke(session, cue), Is.False,
                "Gathering should not show an E interaction chip.");
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Sword"));

            Assert.That(Call<bool>(session, "TryInteract"), Is.True);
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Logging Axe"));
            Assert.That(Call<bool>(session, "TryInteract"), Is.True,
                "A second press during the committed chop is consumed.");
            yield return new WaitForSeconds(.43f);
            Assert.That(Read<int>(harvest, "ChopsRemaining"), Is.EqualTo(2));
            Assert.That(Read<int>(session, "LoggingExperience"), Is.Zero);
            yield return new WaitForSeconds(.4f);
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Sword"));
            object save = session.GetType().GetField("_data",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            Assert.That(save.GetType().GetField("equippedTool").GetValue(save), Is.EqualTo("sword"));
        }

        [UnityTest]
        public IEnumerator GamepadHarvestDodgeRestoresThePreviousWeapon()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            GameObject tree = GameObject.Find("Authored Tree 01");
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            Teleport(player, tree.transform.position + Vector3.back * 1.2f);
            yield return null;

            Set(gamepad.buttonWest, 1f);
            yield return null;
            Set(gamepad.buttonWest, 0f);
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Logging Axe"));
            yield return new WaitForSeconds(.43f);
            Assert.That(Read<int>(harvest, "ChopsRemaining"), Is.EqualTo(2));
            Set(gamepad.buttonEast, 1f);
            yield return new WaitForSeconds(.05f);
            Set(gamepad.buttonEast, 0f);
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Sword"));
        }

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
            Press(keyboard.bKey);
            yield return null;
            Assert.That(Read<bool>(hud, "MenuOpen"), Is.True);
            Release(keyboard.bKey);
            yield return null;
            Press(keyboard.bKey);
            yield return null;
            Assert.That(Read<bool>(hud, "MenuOpen"), Is.False);
            Release(keyboard.bKey);
        }

        [UnityTest]
        public IEnumerator InteractionChipUsesGameplayTargetAndHidesForOpenInventory()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Assert.That(GameObject.Find("Resources"), Is.Null,
                "The permanent prototype resource overlay should be removed.");

            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component hud = GameObject.Find("Loop HUD").GetComponent("LoopHud");
            Teleport(player, new Vector3(1.5f, 0f, -.1f));
            var method = session.GetType().GetMethod("TryGetInteraction");
            object[] arguments = { null, null };
            Assert.That((bool)method.Invoke(session, arguments), Is.True);
            Assert.That(((Transform)arguments[0]).parent.name, Is.EqualTo("Rest Point"));
            Assert.That((string)arguments[1], Is.EqualTo("Rest"));
            CanvasGroup chip = GameObject.Find("Interaction Chip").GetComponent<CanvasGroup>();
            yield return null;
            Assert.That(chip.alpha, Is.GreaterThan(0f));

            Call<object>(hud, "ToggleInventoryPanel");
            arguments = new object[] { null, null };
            Assert.That((bool)method.Invoke(session, arguments), Is.False);
            yield return null;
            Assert.That(chip.alpha, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FullBackpackLeavesHarvestDropOnGround()
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
                Vector3.forward, 2.1f, 90f), Is.True);
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False);
            Assert.That(Read<int>(session, "LoggingExperience"), Is.EqualTo(10));
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(320));
            Assert.That(Read<int>(session, "PickupCount"), Is.EqualTo(1));
            CollectDrop(session);
            Assert.That(Read<int>(session, "PickupCount"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator GamepadAttackAutoEquipsAxeForNearbyTree()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject player = GameObject.Find("Player");
            GameObject tree = GameObject.Find("Authored Tree 01");
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            Teleport(player, tree.transform.position + Vector3.back * 1.3f);
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Sword"));

            Vector3 towardTree = (tree.transform.position - player.transform.position).normalized;
            Vector3 right = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
            Set(gamepad.rightStick, new Vector2(Vector3.Dot(towardTree, right),
                Vector3.Dot(towardTree, forward)));
            yield return null;
            Set(gamepad.rightTrigger, 1f);
            yield return new WaitForSeconds(.42f);
            Set(gamepad.rightTrigger, 0f);

            Assert.That(Read<int>(harvest, "ChopsRemaining"), Is.EqualTo(2),
                "Attack should choose the tree and draw the Logging Axe.");
            yield return new WaitForSeconds(.4f);
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Sword"));
            Set(gamepad.buttonWest, 1f);
            yield return null;
            Set(gamepad.buttonWest, 0f);
            yield return new WaitForSeconds(.43f);

            Assert.That(Read<int>(harvest, "ChopsRemaining"), Is.EqualTo(1));
            Assert.That(Read<int>(session, "WoodCount"), Is.Zero);
            Assert.That(Read<int>(session, "LoggingExperience"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator LivingEnemyInMeleeRangePreventsAutoGather()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            GameObject tree = GameObject.Find("Authored Tree 01");
            Component harvest = tree.GetComponent("HarvestTree");
            Component session = player.GetComponent("WorldSession");
            GameObject enemy = GameObject.Find("Enemy");
            enemy.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            Teleport(player, tree.transform.position + Vector3.back * 1.3f);
            enemy.transform.position = player.transform.position + Vector3.back;
            yield return null;

            Set(gamepad.rightTrigger, 1f);
            yield return new WaitForSeconds(.42f);
            Set(gamepad.rightTrigger, 0f);
            Assert.That(Read<int>(harvest, "ChopsRemaining"), Is.EqualTo(3));
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Sword"));
            Assert.That(Read<int>(session, "LoggingExperience"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator CompletedTreeAwardsLoggingAndHarvestDropsWoodUntilCollected()
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
            Assert.That(Read<int>(session, "WoodCount"), Is.Zero);
            Assert.That(Read<int>(session, "LoggingExperience"), Is.EqualTo(10));
            Assert.That(Read<int>(session, "PickupCount"), Is.EqualTo(1));
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False);
            CollectDrop(session);
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(6));
            Assert.That(Read<int>(session, "PickupCount"), Is.Zero);

            Teleport(player, new Vector3(1.5f, 0, -.1f));
            double harvestedAt = Read<double>(session, "WorldHours");
            for (int rest = 1; rest <= 8; rest++)
            {
                Assert.That(Call<bool>(session, "TryInteract"), Is.True);
                yield return new WaitForSecondsRealtime(.7f);
                Assert.That(Read<double>(session, "WorldHours"),
                    Is.EqualTo(harvestedAt + rest * 8d).Within(.1d));
                Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False);
            }
            Assert.That(Call<bool>(session, "TryInteract"), Is.True);
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.True);
            Teleport(player, tree.transform.position + Vector3.back * 1.3f);
            Assert.That(Call<bool>(harvest, "TryChop", player.transform.position,
                Vector3.forward, 2.1f, 90f), Is.True);
            Assert.That(Read<int>(session, "LoggingExperience"), Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator InventoryPausesWorldClockAndTreeRegrowsFromActiveTime()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component hud = GameObject.Find("Loop HUD").GetComponent("LoopHud");
            Component harvest = GameObject.Find("Authored Tree 01").GetComponent("HarvestTree");
            Teleport(player, harvest.transform.position + Vector3.back * 1.3f);
            for (int chop = 0; chop < 3; chop++)
                Assert.That(Call<bool>(harvest, "TryChop", player.transform.position,
                    Vector3.forward, 2.1f, 90f), Is.True);
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False);

            object save = session.GetType().GetField("_data",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            var nodes = (System.Collections.IList)save.GetType().GetField("nodes").GetValue(save);
            double readyAt = (double)nodes[0].GetType().GetField("readyAtWorldHours").GetValue(nodes[0]);
            save.GetType().GetField("worldHours").SetValue(save, readyAt - .0005d);
            Call<object>(hud, "ToggleInventoryPanel");
            double pausedAt = Read<double>(session, "WorldHours");
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(Read<double>(session, "WorldHours"), Is.EqualTo(pausedAt).Within(.00001d));
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False);

            Call<object>(hud, "ToggleInventoryPanel");
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.True,
                "The deadline must resolve during active play without another rest.");
        }

        [UnityTest]
        public IEnumerator WalkingNearDropCollectsItAutomatically()
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
            Assert.That(Read<int>(session, "PickupCount"), Is.EqualTo(1));
            Teleport(player, tree.transform.position + Vector3.back * .6f);
            yield return new WaitForSeconds(.7f);
            Assert.That(Read<int>(session, "PickupCount"), Is.Zero);
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(6));
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
            CollectDrop(session);
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
            CollectDrop(session);
            Teleport(player, Vector3.zero);
            Assert.That(Call<bool>(session, "TryCraftChest"), Is.True);

            FieldInfo previewPosition = session.GetType().GetField("_previewPosition",
                BindingFlags.Instance | BindingFlags.NonPublic);
            previewPosition.SetValue(session, new Vector3(0, 0, 1.5f));
            session.GetType().GetMethod("TryPlaceChest", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(session, null);
            Assert.That(Read<bool>(session, "ChestPlaced"), Is.True);
            Assert.That(Read<bool>(session, "PendingChest"), Is.False);
            Teleport(player, GameObject.Find("Workbench").transform.position);
            object[] prompt = { null, null };
            Assert.That((bool)session.GetType().GetMethod("TryGetInteraction")
                .Invoke(session, prompt), Is.True);
            Assert.That(prompt[1], Is.EqualTo("Inspect"));
            session.GetType().GetMethod("DepositAllItems").Invoke(session, null);
            Assert.That(Read<int>(session, "WoodCount"), Is.Zero);
            Assert.That(Read<int>(session, "ChestWood"), Is.EqualTo(3));

            object repository = session.GetType().GetField("_repository",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            string savePath = (string)repository.GetType().GetProperty("PathOnDisk").GetValue(repository);
            Assert.That(savePath, Does.StartWith(Application.temporaryCachePath),
                "Editor tests must never use the standalone player save directory.");
            object saved = repository.GetType().GetMethod("Load").Invoke(repository, null);
            var characters = (System.Collections.IList)saved.GetType().GetField("characters").GetValue(saved);
            var worlds = (System.Collections.IList)saved.GetType().GetField("worlds").GetValue(saved);
            Assert.That(characters.Count, Is.EqualTo(1));
            Assert.That(worlds.Count, Is.EqualTo(1));
            Assert.That((bool)characters[0].GetType().GetField("pendingChest")
                .GetValue(characters[0]), Is.False);
            var structures = (System.Collections.IList)worlds[0].GetType().GetField("structures")
                .GetValue(worlds[0]);
            Assert.That(structures.Count, Is.EqualTo(1));
            object placed = structures[0];
            Assert.That((string)placed.GetType().GetField("instanceId").GetValue(placed), Is.Not.Empty);
            var slots = (System.Collections.IList)placed.GetType().GetField("slots").GetValue(placed);
            Assert.That(slots.Count, Is.EqualTo(12));
            Assert.That((int)slots[0].GetType().GetField("count").GetValue(slots[0]), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator CarriedWoodTravelsWhileHarvestedTreeBelongsToItsWorld()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component harvest = GameObject.Find("Authored Tree 01").GetComponent("HarvestTree");
            string characterId = Read<string>(session, "ActiveCharacterId");
            string firstWorldId = Read<string>(session, "ActiveWorldId");
            Teleport(player, harvest.transform.position + Vector3.back * 1.3f);
            for (int i = 0; i < 3; i++)
                Assert.That(Call<bool>(harvest, "TryChop", player.transform.position,
                    Vector3.forward, 2.1f, 90f), Is.True);
            CollectDrop(session);
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(6));
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False);

            var enter = session.GetType().GetMethod("EnterPair");
            bool entered = false;
            yield return (IEnumerator)enter.Invoke(session,
                new object[] { characterId, null, null, true,
                    (Action<bool>)(success => entered = success) });
            Assert.That(entered, Is.True);
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(6));
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.True,
                "A fresh World has its own tree state.");

            entered = false;
            yield return (IEnumerator)enter.Invoke(session,
                new object[] { characterId, firstWorldId, null, false,
                    (Action<bool>)(success => entered = success) });
            Assert.That(entered, Is.True);
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(6));
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False,
                "The first World keeps its harvested tree.");
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
                Assert.That((int)migrated.GetType().GetField("version").GetValue(migrated), Is.EqualTo(5));
                var backpack = (System.Collections.IList)migrated.GetType().GetField("backpackSlots").GetValue(migrated);
                Assert.That(backpack.Count, Is.EqualTo(2));
                Assert.That((int)backpack[0].GetType().GetField("count").GetValue(backpack[0]), Is.EqualTo(20));
                Assert.That((int)backpack[1].GetType().GetField("count").GetValue(backpack[1]), Is.EqualTo(5));
                var structures = (System.Collections.IList)migrated.GetType().GetField("structures").GetValue(migrated);
                var chestSlots = (System.Collections.IList)structures[0].GetType().GetField("slots").GetValue(structures[0]);
                Assert.That(chestSlots.Count, Is.EqualTo(2));
                type.GetMethod("Save").Invoke(repository, new[] { migrated });
                object reloaded = type.GetMethod("Load").Invoke(repository, null);
                Assert.That((int)reloaded.GetType().GetField("version").GetValue(reloaded), Is.EqualTo(5));
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void VersionTwoSaveMigratesWithoutLosingSlots()
        {
            string directory = Path.Combine(Path.GetTempPath(), "TopazMigration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string json = "{\"version\":2,\"day\":2,\"loggingExperience\":7," +
                    "\"backpackSlots\":[{\"itemId\":\"material.wood\",\"count\":11}]," +
                    "\"nodes\":[],\"structures\":[]}";
                File.WriteAllText(Path.Combine(directory, "topaz-save.json"), json);
                Type type = Type.GetType("Topaz.LoopStudy.SaveRepository, Assembly-CSharp", true);
                object repository = Activator.CreateInstance(type, directory);
                object migrated = type.GetMethod("Load").Invoke(repository, null);
                Assert.That((int)migrated.GetType().GetField("version").GetValue(migrated), Is.EqualTo(5));
                Assert.That((int)migrated.GetType().GetField("loggingExperience").GetValue(migrated), Is.EqualTo(7));
                var slots = (System.Collections.IList)migrated.GetType().GetField("backpackSlots").GetValue(migrated);
                Assert.That((int)slots[0].GetType().GetField("count").GetValue(slots[0]), Is.EqualTo(11));
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void VersionThreeSaveMigratesToHomeWithoutLosingDrops()
        {
            string directory = Path.Combine(Path.GetTempPath(), "TopazMigration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string json = "{\"version\":3,\"day\":4,\"backpackSlots\":[]," +
                    "\"nodes\":[],\"structures\":[],\"pickups\":[{" +
                    "\"instanceId\":\"drop1\",\"itemId\":\"material.wood\",\"count\":2," +
                    "\"x\":5,\"z\":6}]}";
                File.WriteAllText(Path.Combine(directory, "topaz-save.json"), json);
                Type type = Type.GetType("Topaz.LoopStudy.SaveRepository, Assembly-CSharp", true);
                object repository = Activator.CreateInstance(type, directory);
                object migrated = type.GetMethod("Load").Invoke(repository, null);
                Assert.That((int)migrated.GetType().GetField("version").GetValue(migrated), Is.EqualTo(5));
                Assert.That((string)migrated.GetType().GetField("regionId").GetValue(migrated), Is.EqualTo("home"));
                var drops = (System.Collections.IList)migrated.GetType().GetField("pickups").GetValue(migrated);
                Assert.That(drops.Count, Is.EqualTo(1));
                Assert.That((int)drops[0].GetType().GetField("count").GetValue(drops[0]), Is.EqualTo(2));
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void VersionFourTreeKeepsRemainingRestProgressOnMigration()
        {
            string directory = Path.Combine(Path.GetTempPath(), "TopazMigration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string json = "{\"version\":4,\"day\":2,\"regionId\":\"home\"," +
                    "\"backpackSlots\":[],\"structures\":[],\"pickups\":[],\"nodes\":[" +
                    "{\"objectId\":\"tree.pending\",\"nextAvailableDay\":4}," +
                    "{\"objectId\":\"tree.ready\",\"nextAvailableDay\":2}]}";
                File.WriteAllText(Path.Combine(directory, "topaz-save.json"), json);
                Type type = Type.GetType("Topaz.LoopStudy.SaveRepository, Assembly-CSharp", true);
                object repository = Activator.CreateInstance(type, directory);
                object migrated = type.GetMethod("Load").Invoke(repository, null);
                Assert.That((int)migrated.GetType().GetField("version").GetValue(migrated), Is.EqualTo(5));
                Assert.That((double)migrated.GetType().GetField("worldHours").GetValue(migrated), Is.EqualTo(8d));
                var nodes = (System.Collections.IList)migrated.GetType().GetField("nodes").GetValue(migrated);
                Assert.That((double)nodes[0].GetType().GetField("readyAtWorldHours").GetValue(nodes[0]),
                    Is.EqualTo(24d), "Two remaining old rests become two eight-hour skips.");
                Assert.That((double)nodes[1].GetType().GetField("readyAtWorldHours").GetValue(nodes[1]),
                    Is.Zero);
                type.GetMethod("Save").Invoke(repository, new[] { migrated });
                object reloaded = type.GetMethod("Load").Invoke(repository, null);
                var restoredNodes = (System.Collections.IList)reloaded.GetType().GetField("nodes").GetValue(reloaded);
                Assert.That((double)restoredNodes[0].GetType().GetField("readyAtWorldHours")
                    .GetValue(restoredNodes[0]), Is.EqualTo(24d));
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void ExpeditionRegionAndClaimedCacheSurviveSaveReload()
        {
            string directory = Path.Combine(Path.GetTempPath(), "TopazExpeditionSave-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                Type repositoryType = Type.GetType("Topaz.LoopStudy.SaveRepository, Assembly-CSharp", true);
                Type dataType = Type.GetType("Topaz.LoopStudy.TopazSaveData, Assembly-CSharp", true);
                object repository = Activator.CreateInstance(repositoryType, directory);
                object data = Activator.CreateInstance(dataType);
                dataType.GetField("regionId").SetValue(data, "expedition.clearing");
                dataType.GetField("expeditionCacheClaimed").SetValue(data, true);
                dataType.GetField("playerX").SetValue(data, 100f);
                dataType.GetField("worldHours").SetValue(data, 37.5d);
                repositoryType.GetMethod("Save").Invoke(repository, new[] { data });

                object reloaded = repositoryType.GetMethod("Load").Invoke(repository, null);
                Assert.That((string)dataType.GetField("regionId").GetValue(reloaded),
                    Is.EqualTo("expedition.clearing"));
                Assert.That((bool)dataType.GetField("expeditionCacheClaimed").GetValue(reloaded), Is.True);
                Assert.That((float)dataType.GetField("playerX").GetValue(reloaded), Is.EqualTo(100f));
                Assert.That((double)dataType.GetField("worldHours").GetValue(reloaded), Is.EqualTo(37.5d));
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void QueuedSavesKeepTheNewestSnapshot()
        {
            string directory = Path.Combine(Path.GetTempPath(), "TopazQueuedSave-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                Type repositoryType = Type.GetType("Topaz.LoopStudy.SaveRepository, Assembly-CSharp", true);
                Type dataType = Type.GetType("Topaz.LoopStudy.TopazSaveData, Assembly-CSharp", true);
                object repository = Activator.CreateInstance(repositoryType, directory);
                object data = Activator.CreateInstance(dataType);
                dataType.GetField("day").SetValue(data, 2);
                repositoryType.GetMethod("QueueSave").Invoke(repository, new[] { data });
                dataType.GetField("day").SetValue(data, 3);
                repositoryType.GetMethod("QueueSave").Invoke(repository, new[] { data });
                object reloaded = repositoryType.GetMethod("Load").Invoke(repository, null);
                Assert.That((int)dataType.GetField("day").GetValue(reloaded), Is.EqualTo(3));
            }
            finally { Directory.Delete(directory, true); }
        }

        static void CollectDrop(Component session)
        {
            GameObject drop = GameObject.Find("Wood Pickup");
            Assert.That(drop, Is.Not.Null);
            Component pickup = drop.GetComponent("WorldPickup");
            Assert.That(pickup, Is.Not.Null);
            session.GetType().GetMethod("TryCollect").Invoke(session, new object[] { pickup });
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
