using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class WorldLoopTests : TopazInputTestFixture
    {
        [UnityTest]
        public IEnumerator InteractWithTreeChopsAndRestoresPreviousWeapon()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            yield return null;
            GameObject player = GameObject.Find("Player");
            GameObject tree = FindResource("HarvestTree");
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            Teleport(player, tree.transform.position + Vector3.back * 1.2f);
            AimAt(tree.transform.position);
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
            yield return new WaitForSeconds(.6f);
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
            yield return WaitForWilderness();
            yield return null;
            GameObject player = GameObject.Find("Player");
            GameObject tree = FindResource("HarvestTree");
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            Teleport(player, tree.transform.position + Vector3.back * 1.2f);
            AimAt(tree.transform.position);
            yield return null;

            yield return new WaitForSeconds(.1f);
            Set(gamepad.buttonWest, 1f);
            yield return null;
            Set(gamepad.buttonWest, 0f);
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Logging Axe"));
            yield return new WaitForSeconds(.6f);
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
            yield return WaitForWilderness();
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
            yield return WaitForWilderness();
            yield return null;
            Assert.That(GameObject.Find("Resources"), Is.Null,
                "The permanent prototype resource overlay should be removed.");

            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component hud = GameObject.Find("Loop HUD").GetComponent("LoopHud");
            Teleport(player, GameObject.Find("Rest Point").transform.position);
            var method = session.GetType().GetMethod("TryGetInteraction");
            object[] arguments = { null, null };
            Assert.That((bool)method.Invoke(session, arguments), Is.True);
            Assert.That(((Transform)arguments[0]).parent.name, Is.EqualTo("Rest Point"));
            Assert.That((string)arguments[1], Is.EqualTo("Rest"));
            CanvasGroup chip = GameObject.Find("Interaction Chip").GetComponent<CanvasGroup>();
            yield return null;
            yield return new WaitForSeconds(.2f);
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
            yield return WaitForWilderness();
            yield return null;
            GameObject player = GameObject.Find("Player");
            GameObject tree = FindResource("HarvestTree");
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            object backpack = session.GetType().GetField("_backpack",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            object wood = session.GetType().GetField("wood",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            int accepted = (int)backpack.GetType().GetMethod("Add").Invoke(backpack, new[] { wood, (object)320 });
            Assert.That(accepted, Is.EqualTo(280));
            Teleport(player, tree.transform.position + Vector3.back * 1.3f);
            for (int i = 0; i < 2; i++)
                Assert.That(Call<bool>(harvest, "TryChop", player.transform.position,
                    Vector3.forward, 2.1f, 90f), Is.True);
            Assert.That(Call<bool>(harvest, "TryChop", player.transform.position,
                Vector3.forward, 2.1f, 90f), Is.True);
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False);
            Assert.That(Read<int>(session, "LoggingExperience"), Is.EqualTo(10));
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(280));
            Assert.That(Read<int>(session, "PickupCount"), Is.EqualTo(1));
            CollectDrop(session);
            Assert.That(Read<int>(session, "PickupCount"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator GamepadAttackAutoEquipsAxeForNearbyTree()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            yield return null;

            GameObject player = GameObject.Find("Player");
            GameObject tree = FindResource("HarvestTree");
            Component session = player.GetComponent("WorldSession");
            Component harvest = tree.GetComponent("HarvestTree");
            Teleport(player, tree.transform.position + Vector3.back * 1.3f);
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Sword"));

            // The third-person right stick rotates the camera; it is no longer a world-space aim vector.
            AimAt(tree.transform.position);
            yield return null;
            yield return new WaitForSeconds(.1f);
            Set(gamepad.rightTrigger, 1f);
            yield return new WaitForSeconds(.6f);
            Set(gamepad.rightTrigger, 0f);

            Assert.That(Read<int>(harvest, "ChopsRemaining"), Is.EqualTo(2),
                "Attack should choose the tree and draw the Logging Axe.");
            yield return new WaitForSeconds(.4f);
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Sword"));
            yield return new WaitForSeconds(.1f);
            Set(gamepad.buttonWest, 1f);
            yield return null;
            Set(gamepad.buttonWest, 0f);
            yield return new WaitForSeconds(.6f);

            Assert.That(Read<int>(harvest, "ChopsRemaining"), Is.EqualTo(1));
            Assert.That(Read<int>(session, "WoodCount"), Is.Zero);
            Assert.That(Read<int>(session, "LoggingExperience"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator LivingEnemyInMeleeRangePreventsAutoGather()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return TopazTestTravel.EnterWoodland(player);
            GameObject tree = FindResource("HarvestTree").GetComponentsInChildren<MonoBehaviour>()
                .First(value => value.GetType().Name == "HarvestTree").gameObject;
            Component harvest = tree.GetComponent("HarvestTree");
            GameObject enemy = TopazTestTravel.Scout();
            enemy.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            Teleport(player, tree.transform.position + Vector3.back * 1.3f);
            enemy.transform.position = player.transform.position + Vector3.back;
            yield return null;

            yield return new WaitForSeconds(.1f);
            Set(gamepad.rightTrigger, 1f);
            yield return new WaitForSeconds(.6f);
            Set(gamepad.rightTrigger, 0f);
            Assert.That(Read<int>(harvest, "ChopsRemaining"), Is.EqualTo(3));
            Assert.That(Read<string>(session, "EquippedToolName"), Is.EqualTo("Sword"));
            Assert.That(Read<int>(session, "LoggingExperience"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator CompletedTreeAwardsLoggingAndHarvestDropsWoodUntilCollected()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            yield return null;

            GameObject player = GameObject.Find("Player");
            GameObject tree = FindResource("HarvestTree");
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

            Teleport(player, GameObject.Find("Rest Point").transform.position);
            double harvestedAt = Read<double>(session, "WorldHours");
            for (int rest = 1; rest <= 8; rest++)
            {
                Assert.That(Call<bool>(session, "TryInteract"), Is.True);
                yield return WaitForRestCompletion(session);
                Assert.That(Read<double>(session, "WorldHours"),
                    Is.EqualTo(harvestedAt + rest * 8d).Within(.1d));
                Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False);
            }
            Assert.That(Call<bool>(session, "TryInteract"), Is.True);
            yield return WaitForRestCompletion(session);
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
            yield return WaitForWilderness();
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component hud = GameObject.Find("Loop HUD").GetComponent("LoopHud");
            Component harvest = FindResource("HarvestTree").GetComponent("HarvestTree");
            Teleport(player, harvest.transform.position + Vector3.back * 1.3f);
            for (int chop = 0; chop < 3; chop++)
                Assert.That(Call<bool>(harvest, "TryChop", player.transform.position,
                    Vector3.forward, 2.1f, 90f), Is.True);
            Assert.That(Read<bool>(harvest, "IsAvailable"), Is.False);

            object save = session.GetType().GetField("_data",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            var nodes = (System.Collections.IList)save.GetType().GetField("nodes").GetValue(save);
            object harvestedNode = nodes.Cast<object>().Single(n => (string)n.GetType().GetField("objectId").GetValue(n) == Read<string>(harvest, "StableObjectId"));
            double readyAt = (double)harvestedNode.GetType().GetField("readyAtWorldHours").GetValue(harvestedNode);
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
            yield return WaitForWilderness();
            yield return null;
            GameObject player = GameObject.Find("Player");
            GameObject tree = FindResource("HarvestTree");
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
        public IEnumerator CarriedWoodTravelsWhileHarvestedTreeBelongsToItsWorld()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component harvest = FindResource("HarvestTree").GetComponent("HarvestTree");
            string characterId = Read<string>(session, "ActiveCharacterId");
            string firstWorldId = Read<string>(session, "ActiveWorldId");
            string firstNodeId = Read<string>(harvest,"StableObjectId");
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
            yield return WaitForWilderness();
            Assert.That(Read<bool>(FindResource("HarvestTree").GetComponent("HarvestTree"), "IsAvailable"), Is.True,
                "A fresh World has its own tree state.");

            entered = false;
            yield return (IEnumerator)enter.Invoke(session,
                new object[] { characterId, firstWorldId, null, false,
                    (Action<bool>)(success => entered = success) });
            Assert.That(entered, Is.True);
            Assert.That(Read<int>(session, "WoodCount"), Is.EqualTo(6));
            yield return WaitForWilderness();
            Assert.That(Read<bool>(FindResource("HarvestTree","StableObjectId",firstNodeId).GetComponent("HarvestTree"), "IsAvailable"), Is.False,
                "The first World keeps its harvested tree.");
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

        static IEnumerator WaitForRestCompletion(Component session)
        {
            Assert.That(Read<bool>(session,"IsResting"),Is.True,"The interaction must start a new rest.");
            float deadline=Time.realtimeSinceStartup+3;
            while(Read<bool>(session,"IsResting") && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Read<bool>(session,"IsResting"),Is.False,"Both rest fades must complete before another interaction.");
            // Resume an active clock update between rests. Nine chained +8 additions can land
            // one double ULP below a separately computed +72 deadline in the completion frame.
            yield return null;
            yield return null;
        }
        static void Teleport(GameObject player, Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                Vector3 local = position - terrain.transform.position;
                if (local.x >= 0 && local.z >= 0 && local.x <= terrain.terrainData.size.x && local.z <= terrain.terrainData.size.z)
                    position.y = terrain.SampleHeight(position) + terrain.transform.position.y + .01f;
            }
            player.transform.position = position;
            player.GetComponent("PlayerController").GetType().GetMethod("ResetMotion").Invoke(player.GetComponent("PlayerController"), null);
            controller.enabled = true;
            Physics.SyncTransforms();
            controller.Move(Vector3.down * .02f);
        }
    }
}
