using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class CryptTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator RogueWarnsShootsAndChangesFiringSpot()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return EnterCrypt(player, session);
            Transform root = GameObject.Find("Home Crypt").transform;
            Component rogue = root.Find("Gallery Rogue").GetComponent("EnemyCombatant");
            Component minion = root.Find("Gallery Minion A").GetComponent("EnemyCombatant");
            float deadline = Time.realtimeSinceStartup + 5f;
            while ((!rogue.gameObject.activeSelf || !minion.gameObject.activeSelf) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            ((Behaviour)minion).enabled = false;
            Teleport(player, rogue.transform.position + Vector3.back * 5f);
            Vector3 start = rogue.transform.position;
            while (!Get<bool>(rogue, "IsWindingUp") &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Get<bool>(rogue, "IsWindingUp"), Is.True);
            LineRenderer warning = root.Find("Gallery Rogue Tell")
                .GetComponent<LineRenderer>();
            Assert.That(warning.enabled, Is.True);
            Assert.That(warning.positionCount, Is.EqualTo(2));
            int before = Get<int>(player.GetComponent("PlayerVitality"), "CurrentHealth");
            yield return new WaitForSeconds(1.5f);
            Assert.That(Get<int>(player.GetComponent("PlayerVitality"), "CurrentHealth"),
                Is.LessThan(before), "An unobstructed Rogue bolt should reach the player.");
            deadline = Time.realtimeSinceStartup + 4f;
            while (Vector3.Distance(start, rogue.transform.position) < 1f &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Vector3.Distance(start, rogue.transform.position),
                Is.GreaterThan(1f), "The Rogue should move to its second firing spot.");
        }

        [UnityTest]
        public IEnumerator RogueCrossbowWaitsForBackpackSpaceAndDropsOncePerWorld()
        {
            string directory = Path.Combine(Path.GetTempPath(),
                "TopazCryptCrossbow-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            Type sessionType = Type.GetType("Topaz.LoopStudy.WorldSession, Assembly-CSharp", true);
            sessionType.GetProperty("EditorTestSaveDirectory").SetValue(null, directory);
            try
            {
                yield return SceneManager.LoadSceneAsync("Bootstrap");
                yield return null;
                GameObject player = GameObject.Find("Player");
                Component session = player.GetComponent("WorldSession");
                yield return EnterCrypt(player, session);
                Transform root = GameObject.Find("Home Crypt").transform;
                Assert.That(root.Find("Gallery Minion A"), Is.Not.Null);
                Assert.That(root.Find("Gallery Minion B"), Is.Null);
                Assert.That(GameObject.Find("Gallery Bolt Cover")?.GetComponent<Collider>(),
                    Is.Not.Null);
                Transform rogue = root.Find("Gallery Rogue");
                Assert.That(rogue, Is.Not.Null);
                float deadline = Time.realtimeSinceStartup + 5f;
                while (!rogue.gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(rogue.gameObject.activeSelf, Is.True);
                rogue.GetComponent("EnemyCombatant").GetType().GetMethod("TakeDamage")
                    .Invoke(rogue.GetComponent("EnemyCombatant"), new object[] { 999 });
                yield return null;
                Assert.That(Get<bool>(session, "CryptRogueCrossbowAwarded"), Is.True);
                Assert.That(Get<bool>(session, "CrossbowsDiscovered"), Is.False);
                var slots = ((IEnumerable)Get<object>(session, "BackpackSlots"))
                    .Cast<object>().ToArray();
                foreach (object slot in slots)
                {
                    slot.GetType().GetField("itemId").SetValue(slot, "material.wood");
                    slot.GetType().GetField("count").SetValue(slot, 20);
                }
                GameObject pickup = GameObject.Find("Skeleton Crossbow Pickup");
                Assert.That(pickup, Is.Not.Null);
                session.GetType().GetMethod("TryCollect").Invoke(session,
                    new object[] { pickup.GetComponent("WorldPickup") });
                Assert.That(Get<int>(session, "PickupCount"), Is.EqualTo(1));
                session.GetType().GetMethod("Commit").Invoke(session, null);
                session.GetType().GetMethod("FlushCurrent").Invoke(session, null);

                yield return SceneManager.LoadSceneAsync("Bootstrap");
                player = GameObject.Find("Player");
                session = player.GetComponent("WorldSession");
                yield return WaitForRegion(session, "dungeon.home-crypt");
                Assert.That(Get<bool>(session, "CryptRogueCrossbowAwarded"), Is.True);
                pickup = GameObject.Find("Skeleton Crossbow Pickup");
                Assert.That(pickup, Is.Not.Null);
                slots = ((IEnumerable)Get<object>(session, "BackpackSlots"))
                    .Cast<object>().ToArray();
                slots[0].GetType().GetField("itemId").SetValue(slots[0], null);
                slots[0].GetType().GetField("count").SetValue(slots[0], 0);
                session.GetType().GetMethod("TryCollect").Invoke(session,
                    new object[] { pickup.GetComponent("WorldPickup") });
                Assert.That(Get<bool>(session, "CrossbowsDiscovered"), Is.True);
                Assert.That(Get<int>(session, "PickupCount"), Is.Zero);
                Assert.That((bool)session.GetType().GetMethod("TryEquipFromBackpack")
                    .Invoke(session, new object[] { 0 }), Is.False,
                    "A full backpack must not discard the displaced shield.");
                slots[1].GetType().GetField("itemId").SetValue(slots[1], null);
                slots[1].GetType().GetField("count").SetValue(slots[1], 0);
                Assert.That((bool)session.GetType().GetMethod("TryEquipFromBackpack")
                    .Invoke(session, new object[] { 0 }), Is.True);
                object weapon = Get<object>(session, "CurrentWeapon");
                Assert.That(weapon.GetType().GetProperty("StableId").GetValue(weapon),
                    Is.EqualTo("weapon.crypt.crossbow"));
                Assert.That(Get<bool>(session, "HasShield"), Is.False);

                yield return ReturnHome(player, session);
                yield return EnterCrypt(player, session);
                Assert.That(GameObject.Find("Skeleton Crossbow Pickup"), Is.Null);
                Assert.That(GameObject.Find("Home Crypt").transform.Find("Gallery Rogue"),
                    Is.Not.Null);
                yield return ReturnHome(player, session);
            }
            finally
            {
                sessionType.GetProperty("EditorTestSaveDirectory").SetValue(null, null);
            }
        }

        [UnityTest]
        public IEnumerator EntranceCampfireAndShortcutSurviveReturn()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return EnterCrypt(player, session);
            Assert.That(SceneManager.GetSceneByName("Crypt").isLoaded, Is.True);
            Assert.That(GameObject.Find("Home Crypt").transform.Find("Gallery Minion A"), Is.Not.Null);
            Assert.That(GameObject.Find("Home Crypt").transform.Find("Hall Warrior"), Is.Not.Null);
            var route = new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(player.transform.position,
                GameObject.Find("Home Crypt").transform.Find("Crypt Mage").position,
                NavMesh.AllAreas, route), Is.True);
            Assert.That(route.status, Is.EqualTo(NavMeshPathStatus.PathComplete),
                "The authored rooms must leave a walkable route to the Mage.");

            Teleport(player, GameObject.Find("Crypt Campfire").transform.position);
            yield return null;
            Assert.That(Get<string>(session, "ReturnCampfireId"),
                Is.EqualTo("campfire.dungeon.home-crypt"));
            Teleport(player, GameObject.Find("Shortcut Lever").transform.position);
            Interact(session);
            Assert.That(Get<bool>(session, "CryptShortcutOpen"), Is.True);
            Assert.That(GameObject.Find("Shortcut Gate Barrier"), Is.Null);

            Teleport(player, GameObject.Find("Return Door").transform.position);
            Interact(session);
            yield return WaitForRegion(session, "home");
            yield return WaitForCryptUnload();
            yield return EnterCrypt(player, session);
            Assert.That(Get<bool>(session, "CryptShortcutOpen"), Is.True);
            Assert.That(GameObject.Find("Shortcut Gate Barrier"), Is.Null);
            yield return ReturnHome(player, session);
        }

        [UnityTest]
        public IEnumerator MageDropsOnePersistentStaffAndRevealFollowsPickup()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Assert.That(Get<bool>(session, "StaffDiscovered"), Is.False);
            yield return EnterCrypt(player, session);
            Transform mage = GameObject.Find("Home Crypt").transform.Find("Crypt Mage");
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!mage.gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(mage.gameObject.activeSelf, Is.True);
            mage.GetComponent("EnemyCombatant").GetType().GetMethod("TakeDamage")
                .Invoke(mage.GetComponent("EnemyCombatant"), new object[] { 999 });
            yield return null;
            Assert.That(Get<bool>(session, "CryptMageDefeated"), Is.True);
            GameObject staffPickup = GameObject.Find("Crypt Staff Pickup");
            Assert.That(staffPickup, Is.Not.Null);
            Assert.That(Get<bool>(session, "StaffDiscovered"), Is.False);
            int count = Get<int>(session, "PickupCount");
            session.GetType().GetMethod("TryCollect").Invoke(session,
                new object[] { staffPickup.GetComponent("WorldPickup") });
            Assert.That(Get<bool>(session, "StaffDiscovered"), Is.True);
            Assert.That(Get<int>(session, "PickupCount"), Is.EqualTo(count - 1));

            var slots = (IEnumerable)Get<object>(session, "BackpackSlots");
            int index = 0;
            foreach (object slot in slots)
            {
                if ((string)slot.GetType().GetField("itemId").GetValue(slot) == "gear.crypt.staff")
                    break;
                index++;
            }
            Assert.That(index, Is.LessThan(16));
            Assert.That((bool)session.GetType().GetMethod("TryEquipFromBackpack")
                .Invoke(session, new object[] { index }), Is.True);
            object weapon = Get<object>(session, "CurrentWeapon");
            Assert.That((string)weapon.GetType().GetProperty("StableId").GetValue(weapon),
                Is.EqualTo("weapon.crypt.staff"));

            Teleport(player, GameObject.Find("Return Door").transform.position);
            Interact(session);
            yield return WaitForRegion(session, "home");
            yield return WaitForCryptUnload();
            yield return EnterCrypt(player, session);
            Assert.That(Get<bool>(session, "CryptMageDefeated"), Is.True);
            Assert.That(Get<bool>(session, "StaffDiscovered"), Is.True);
            Assert.That(GameObject.Find("Crypt Staff Pickup"), Is.Null);
            Assert.That(GameObject.Find("Home Crypt").transform.Find("Crypt Mage").gameObject.activeSelf,
                Is.False);
            yield return ReturnHome(player, session);
        }

        [UnityTest]
        public IEnumerator FullBackpackLeavesStaffInWorldAcrossReload()
        {
            string directory = Path.Combine(Path.GetTempPath(),
                "TopazCryptReward-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            Type sessionType = Type.GetType("Topaz.LoopStudy.WorldSession, Assembly-CSharp", true);
            sessionType.GetProperty("EditorTestSaveDirectory").SetValue(null, directory);
            Component session = null;
            try
            {
                yield return SceneManager.LoadSceneAsync("Bootstrap");
                yield return null;
                GameObject player = GameObject.Find("Player");
                session = player.GetComponent("WorldSession");
                yield return EnterCrypt(player, session);
                Transform mage = GameObject.Find("Home Crypt").transform.Find("Crypt Mage");
                float ready = Time.realtimeSinceStartup + 5f;
                while (!mage.gameObject.activeSelf && Time.realtimeSinceStartup < ready)
                    yield return null;
                Component enemy = mage.GetComponent("EnemyCombatant");
                enemy.GetType().GetMethod("TakeDamage").Invoke(enemy, new object[] { 999 });
                yield return null;
                object pickup = GameObject.Find("Crypt Staff Pickup").GetComponent("WorldPickup");
                var slots = ((IEnumerable)Get<object>(session, "BackpackSlots"))
                    .Cast<object>().ToArray();
                foreach (object slot in slots)
                {
                    slot.GetType().GetField("itemId").SetValue(slot, "material.wood");
                    slot.GetType().GetField("count").SetValue(slot, 20);
                }
                session.GetType().GetMethod("TryCollect").Invoke(session, new[] { pickup });
                Assert.That(Get<int>(session, "PickupCount"), Is.EqualTo(1));
                Assert.That(Get<bool>(session, "StaffDiscovered"), Is.False);
                session.GetType().GetMethod("Commit").Invoke(session, null);
                session.GetType().GetMethod("FlushCurrent").Invoke(session, null);

                yield return SceneManager.LoadSceneAsync("Bootstrap");
                yield return WaitForRegion(GameObject.Find("Player").GetComponent("WorldSession"),
                    "dungeon.home-crypt");
                session = GameObject.Find("Player").GetComponent("WorldSession");
                Assert.That(Get<bool>(session, "CryptMageDefeated"), Is.True);
                Assert.That(GameObject.Find("Crypt Staff Pickup"), Is.Not.Null);
                Assert.That(Get<int>(session, "PickupCount"), Is.EqualTo(1));
                object freeSlot = ((IEnumerable)Get<object>(session, "BackpackSlots"))
                    .Cast<object>().First();
                freeSlot.GetType().GetField("itemId").SetValue(freeSlot, null);
                freeSlot.GetType().GetField("count").SetValue(freeSlot, 0);
                session.GetType().GetMethod("TryCollect").Invoke(session,
                    new object[] { GameObject.Find("Crypt Staff Pickup").GetComponent("WorldPickup") });
                Assert.That(Get<bool>(session, "StaffDiscovered"), Is.True);
                Assert.That(Get<int>(session, "PickupCount"), Is.Zero);
            }
            finally
            {
                sessionType.GetProperty("EditorTestSaveDirectory").SetValue(null, null);
                if (session != null) session.GetType().GetMethod("FlushCurrent").Invoke(session, null);
            }
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            Directory.Delete(directory, true);
        }

        [UnityTest]
        public IEnumerator SharedGroundSpellHitsOnlyOpponentsOnce()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return EnterCrypt(player, session);
            Transform crypt = GameObject.Find("Home Crypt").transform;
            Transform minion = crypt.Find("Gallery Minion A");
            Transform mage = crypt.Find("Crypt Mage");
            float ready = Time.realtimeSinceStartup + 5f;
            while ((!minion.gameObject.activeSelf || !mage.gameObject.activeSelf) &&
                   Time.realtimeSinceStartup < ready) yield return null;
            Component minionCombat = minion.GetComponent("EnemyCombatant");
            ((Behaviour)minionCombat).enabled = false;
            Component mageCombat = mage.GetComponent("EnemyCombatant");
            ((Behaviour)mageCombat).enabled = false;
            Component playerVitality = player.GetComponent("PlayerVitality");
            int enemyBefore = Get<int>(minionCombat, "CurrentHealth");
            int playerBefore = Get<int>(playerVitality, "CurrentHealth");
            Component playerSpell = player.GetComponent("GroundSpellAbility");
            Assert.That((bool)playerSpell.GetType().GetMethod("Begin").Invoke(playerSpell,
                new object[] { minion.position, false, 2, 0f }), Is.True);
            yield return new WaitForSeconds(1f);
            Assert.That(Get<int>(minionCombat, "CurrentHealth"), Is.EqualTo(enemyBefore - 2));
            Assert.That(Get<int>(playerVitality, "CurrentHealth"), Is.EqualTo(playerBefore));
            yield return new WaitForSeconds(.2f);
            Assert.That(Get<int>(minionCombat, "CurrentHealth"), Is.EqualTo(enemyBefore - 2));

            Teleport(player, mage.position + Vector3.right * 3f);
            Component mageSpell = mage.GetComponent("GroundSpellAbility");
            Assert.That((bool)mageSpell.GetType().GetMethod("Begin").Invoke(mageSpell,
                new object[] { player.transform.position, true, 4, 0f }), Is.True);
            yield return new WaitForSeconds(1f);
            Assert.That(Get<int>(playerVitality, "CurrentHealth"), Is.LessThan(playerBefore));
            Assert.That(Get<int>(mageCombat, "CurrentHealth"),
                Is.EqualTo(Get<int>(mageCombat, "MaximumHealth")));
            yield return ReturnHome(player, session);
        }

        [UnityTest]
        public IEnumerator DefeatReturnsToSafeCryptHearthAndAdvancesTimeOnce()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return EnterCrypt(player, session);
            Transform hearth = GameObject.Find("Crypt Campfire").transform;
            Teleport(player, hearth.position);
            yield return null;
            Assert.That(Get<string>(session, "ReturnCampfireId"),
                Is.EqualTo("campfire.dungeon.home-crypt"));
            double before = Get<double>(session, "WorldHours");
            Component vitality = player.GetComponent("PlayerVitality");
            vitality.GetType().GetMethod("TryTakeDamage").Invoke(vitality,
                new object[] { 99 });
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Get<bool>(session, "IsRecovering") &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Get<bool>(session, "IsRecovering"), Is.False);
            Assert.That(Get<string>(session, "CurrentRegionId"),
                Is.EqualTo("dungeon.home-crypt"));
            Assert.That(Get<double>(session, "WorldHours") - before,
                Is.EqualTo(8d).Within(.05d));
            Assert.That(player.transform.position.z, Is.EqualTo(5.2f).Within(.3f));
            Assert.That(Get<int>(vitality, "CurrentHealth"), Is.EqualTo(6));
            yield return new WaitForSeconds(1.5f);
            Assert.That(Get<int>(vitality, "CurrentHealth"), Is.EqualTo(6),
                "Ordinary enemies must not pressure the midway recovery spot.");
            yield return ReturnHome(player, session);
        }

        static IEnumerator ReturnHome(GameObject player, Component session)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (GameObject.Find("Return Door") == null &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(GameObject.Find("Return Door"), Is.Not.Null);
            Teleport(player, GameObject.Find("Return Door").transform.position);
            Interact(session);
            yield return WaitForRegion(session, "home");
            yield return WaitForCryptUnload();
        }

        static IEnumerator EnterCrypt(GameObject player, Component session)
        {
            Teleport(player, GameObject.Find("Home Crypt Entrance").transform.position);
            float deadline = Time.realtimeSinceStartup + 10f;
            object[] cue = { null, null };
            bool ready = false;
            while (Time.realtimeSinceStartup < deadline)
            {
                cue[0] = cue[1] = null;
                ready = (bool)session.GetType().GetMethod("TryGetInteraction")
                    .Invoke(session, cue) && (string)cue[1] == "Enter crypt";
                if (ready) break;
                yield return null;
            }
            Assert.That(ready, Is.True, "The crypt entrance should offer an interaction.");
            Interact(session);
            yield return WaitForRegion(session, "dungeon.home-crypt");
            while (GameObject.Find("Home Crypt") == null &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(GameObject.Find("Home Crypt"), Is.Not.Null);
        }

        static IEnumerator WaitForCryptUnload()
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetSceneByName("Crypt").isLoaded &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetSceneByName("Crypt").isLoaded, Is.False);
        }

        static T Get<T>(Component component, string property) =>
            (T)component.GetType().GetProperty(property).GetValue(component);

        static void Interact(Component session) =>
            session.GetType().GetMethod("TryInteract").Invoke(session, null);

        static IEnumerator WaitForRegion(Component session, string region)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Get<string>(session, "CurrentRegionId") != region &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Get<string>(session, "CurrentRegionId"), Is.EqualTo(region));
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
