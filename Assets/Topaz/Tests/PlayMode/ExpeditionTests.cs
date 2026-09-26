using System.Collections;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class ExpeditionTests : TopazInputTestFixture
    {
        [UnityTest]
        public IEnumerator WalkingThroughTrailLoadsWoodlandAndReturnsHome()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!(bool)session.GetType().GetProperty("HasActivePair").GetValue(session) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Transform gate = GameObject.Find("Woodland Trail").transform;
            Assert.That(gate.position.z, Is.GreaterThan(30f));
            Teleport(player, gate.position - Vector3.forward * 2f);
            player.GetComponent<CharacterController>().Move(Vector3.forward * 2f);
            yield return WaitForRegion(session, "expedition.clearing");
            Assert.That(SceneManager.GetSceneByName("Woodland").isLoaded, Is.True);
            Assert.That(Terrain.activeTerrains.Length, Is.EqualTo(2));
            Transform scout = GameObject.Find("Scout A").transform;
            Assert.That(NavMesh.SamplePosition(scout.position,out NavMeshHit destination,2f,NavMesh.AllAreas), Is.True);
            var route=new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(player.transform.position,destination.position,NavMesh.AllAreas,route),Is.True);
            Assert.That(route.status,Is.EqualTo(NavMeshPathStatus.PathComplete));
            Transform returnTrail=GameObject.Find("Home Trail").transform;
            deadline = Time.realtimeSinceStartup + 5f;
            FieldInfo ready = session.GetType().GetField("_trailReadyAt",
                BindingFlags.Instance | BindingFlags.NonPublic);
            while (Time.time < (float)ready.GetValue(session) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Teleport(player, returnTrail.position + Vector3.forward * 2f);
            player.GetComponent<CharacterController>().Move(Vector3.back * 2f);
            yield return WaitForRegion(session, "home");
            yield return WaitForScene(false);
        }

        [UnityTest]
        public IEnumerator ClearingTreesAndRocksBindAsGatherableNodes()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return CrossTrail(session, "expedition.clearing");

            GameObject clearing = GameObject.Find("Woodland");
            Component[] trees = clearing.GetComponentsInChildren<MonoBehaviour>()
                .Where(value => value.GetType().Name == "HarvestTree").Cast<Component>().ToArray();
            Component[] rocks = clearing.GetComponentsInChildren<MonoBehaviour>()
                .Where(value => value.GetType().Name == "MiningRock").Cast<Component>().ToArray();
            Assert.That(trees.Length, Is.EqualTo(12));
            Assert.That(rocks.Length, Is.EqualTo(4));
            foreach (Component tree in trees)
            {
                Assert.That((bool)tree.GetType().GetProperty("IsAvailable").GetValue(tree), Is.True);
                Assert.That((string)tree.GetType().GetProperty("StableObjectId").GetValue(tree),
                    Does.StartWith("expedition.clearing.tree."));
            }
            foreach (Component rock in rocks)
            {
                Assert.That((bool)rock.GetType().GetProperty("IsAvailable").GetValue(rock), Is.True);
                Assert.That((string)rock.GetType().GetProperty("StableObjectId").GetValue(rock),
                    Does.StartWith("expedition.clearing.rock."));
            }
            yield return CrossTrail(session, "home");
            yield return WaitForScene(false);
        }

        [UnityTest]
        public IEnumerator SwordCanDamageAnExpeditionScout()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return CrossTrail(session, "expedition.clearing");

            Transform scout = GameObject.Find("Woodland").transform.Find("Scout A");
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!scout.gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(scout.gameObject.activeSelf, Is.True);
            Component combatant = scout.GetComponent("EnemyCombatant");
            int before = (int)combatant.GetType().GetProperty("CurrentHealth").GetValue(combatant);
            Vector3 screenRight = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
            Teleport(player, scout.position - screenRight * 1.35f);
            AimAt(GameObject.Find("Scout A").transform.position);
            Set(gamepad.rightStick, Vector2.zero);
            yield return null;
            Set(gamepad.rightTrigger, 1f);
            yield return new WaitForSeconds(.35f);
            Set(gamepad.rightTrigger, 0f);
            Assert.That((int)combatant.GetType().GetProperty("CurrentHealth").GetValue(combatant),
                Is.LessThan(before), "The sword should hit enemies in the loaded clearing.");

            yield return CrossTrail(session, "home");
            yield return WaitForScene(false);
        }

        [UnityTest]
        public IEnumerator AxeStaggersScoutButGuardianResists()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return CrossTrail(session, "expedition.clearing");
            Transform clearing = GameObject.Find("Woodland").transform;
            Component scout = clearing.Find("Scout A").GetComponent("EnemyCombatant");
            Component guardian = clearing.Find("Wide-Sweep Guardian").GetComponent("EnemyCombatant");
            float deadline = Time.realtimeSinceStartup + 5f;
            while ((!scout.gameObject.activeSelf || !guardian.gameObject.activeSelf) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            scout.GetType().GetMethod("StaggerFromWeapon").Invoke(scout, new object[] { .3f });
            guardian.GetType().GetMethod("StaggerFromWeapon")
                .Invoke(guardian, new object[] { .3f });
            FieldInfo timer = scout.GetType().GetField("_guardStaggerUntil",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That((float)timer.GetValue(scout), Is.GreaterThan(Time.time));
            Assert.That((float)timer.GetValue(guardian), Is.LessThanOrEqualTo(Time.time));

            yield return CrossTrail(session, "home");
            yield return WaitForScene(false);
        }

        [UnityTest]
        public IEnumerator DefeatRecoversAtLastCampfireAndAdvancesEightHours()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return CrossTrail(session, "expedition.clearing");

            GameObject campfire = GameObject.Find("Clearing Campfire");
            Assert.That(campfire, Is.Not.Null);
            Assert.That((string)session.GetType().GetProperty("ReturnCampfireId").GetValue(session),
                Is.EqualTo("campfire.home"));
            Teleport(player, campfire.transform.position + Vector3.right * 2f);
            yield return null;
            Assert.That((string)session.GetType().GetProperty("ReturnCampfireId").GetValue(session),
                Is.EqualTo("campfire.home"), "Passing outside the hearth must not activate it.");
            Teleport(player, campfire.transform.position);
            yield return null;
            Assert.That((string)session.GetType().GetProperty("ReturnCampfireId").GetValue(session),
                Is.EqualTo("campfire.expedition.clearing"));

            Transform scout = GameObject.Find("Woodland").transform.Find("Scout A");
            float readyDeadline = Time.realtimeSinceStartup + 5f;
            while (!scout.gameObject.activeSelf && Time.realtimeSinceStartup < readyDeadline)
                yield return null;
            Component enemy = scout.GetComponent("EnemyCombatant");
            enemy.GetType().GetMethod("TakeDamage").Invoke(enemy, new object[] { 1 });
            Assert.That((int)enemy.GetType().GetProperty("CurrentHealth").GetValue(enemy),
                Is.EqualTo(9));

            Transform guardian = GameObject.Find("Woodland").transform
                .Find("Wide-Sweep Guardian");
            Component guardianCombatant = guardian.GetComponent("EnemyCombatant");
            guardianCombatant.GetType().GetMethod("TakeDamage")
                .Invoke(guardianCombatant, new object[] { 99 });
            Assert.That((int)session.GetType().GetProperty("PickupCount").GetValue(session),
                Is.EqualTo(2), "Guardian defeat should leave Bone Fragments and one gear item.");
            Teleport(player, GameObject.Find("Guarded Supply Cache").transform.position);
            Interact(session);
            int wood = (int)session.GetType().GetProperty("WoodCount").GetValue(session);
            Assert.That(wood, Is.EqualTo(3));
            Assert.That((bool)session.GetType().GetProperty("ExpeditionCacheClaimed")
                .GetValue(session), Is.True);
            Teleport(player, campfire.transform.position);
            yield return null;

            session.GetType().GetMethod("RecordSwordHit").Invoke(session, new object[] { 2 });
            int experience = (int)session.GetType().GetProperty("SwordsExperience").GetValue(session);
            double before = (double)session.GetType().GetProperty("WorldHours").GetValue(session);

            Component vitality = player.GetComponent("PlayerVitality");
            Teleport(player, GameObject.Find("Clearing Campfire").transform.position + Vector3.right * 20f);
            for (int i = 0; i < 3; i++)
                vitality.GetType().GetMethod("TryTakeDamage").Invoke(vitality, new object[] { 4 });
            Assert.That((bool)session.GetType().GetProperty("IsRecovering").GetValue(session), Is.True);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((bool)session.GetType().GetProperty("IsRecovering").GetValue(session) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)session.GetType().GetProperty("IsRecovering").GetValue(session), Is.False);
            Assert.That(Region(session), Is.EqualTo("expedition.clearing"));
            Assert.That(SceneManager.GetSceneByName("Woodland").isLoaded, Is.True);
            Assert.That((int)vitality.GetType().GetProperty("CurrentHealth").GetValue(vitality),
                Is.EqualTo(6));
            Vector3 arrival = campfire.transform.Find("Safe Arrival").position;
            Assert.That(Vector3.Distance(player.transform.position, arrival), Is.LessThan(.2f));
            Assert.That((double)session.GetType().GetProperty("WorldHours").GetValue(session) - before,
                Is.EqualTo(8d).Within(.05d));
            Assert.That((int)enemy.GetType().GetProperty("CurrentHealth").GetValue(enemy),
                Is.EqualTo(9), "Recovery must preserve damage until an enemy is defeated.");
            Assert.That((int)guardianCombatant.GetType().GetProperty("CurrentHealth")
                .GetValue(guardianCombatant), Is.Zero,
                "A defeated guardian waits for its 24-hour deadline.");
            Assert.That((int)session.GetType().GetProperty("WoodCount").GetValue(session),
                Is.EqualTo(wood), "The claimed reward must not be lost or duplicated.");
            Assert.That((bool)session.GetType().GetProperty("ExpeditionCacheClaimed")
                .GetValue(session), Is.True);
            Assert.That((int)session.GetType().GetProperty("SwordsExperience").GetValue(session),
                Is.EqualTo(experience));
            Assert.That((string)session.GetType().GetProperty("ReturnCampfireId").GetValue(session),
                Is.EqualTo("campfire.expedition.clearing"));

            yield return CrossTrail(session, "home");
            yield return WaitForScene(false);
        }

        [UnityTest]
        public IEnumerator MissingSavedCampfireFallsBackHomeWithoutLosingExperience()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            object visit = session.GetType().GetField("_visit",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            visit.GetType().GetField("lastCampfireId").SetValue(visit, "campfire.removed");
            var discovered = (System.Collections.IList)visit.GetType()
                .GetField("discoveredCampfireIds").GetValue(visit);
            discovered.Add("campfire.removed");
            session.GetType().GetMethod("RecordSwordHit").Invoke(session, new object[] { 2 });
            int experience = (int)session.GetType().GetProperty("SwordsExperience").GetValue(session);
            yield return TopazTestTravel.EnterWoodland(player);
            double before = (double)session.GetType().GetProperty("WorldHours").GetValue(session);

            Component vitality = player.GetComponent("PlayerVitality");
            Teleport(player, GameObject.Find("Clearing Campfire").transform.position + Vector3.right * 20f);
            for (int i = 0; i < 3; i++)
                vitality.GetType().GetMethod("TryTakeDamage").Invoke(vitality, new object[] { 4 });
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((bool)session.GetType().GetProperty("IsRecovering").GetValue(session) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)session.GetType().GetProperty("IsRecovering").GetValue(session), Is.False);
            Assert.That((string)session.GetType().GetProperty("ReturnCampfireId").GetValue(session),
                Is.EqualTo("campfire.home"));
            Assert.That(player.transform.position.x, Is.EqualTo(0f).Within(.2f));
            Assert.That(player.transform.position.z, Is.EqualTo(.35f).Within(.2f));
            Assert.That((int)session.GetType().GetProperty("SwordsExperience").GetValue(session),
                Is.EqualTo(experience));
            Assert.That((double)session.GetType().GetProperty("WorldHours").GetValue(session) - before,
                Is.EqualTo(8d).Within(.05d));
        }

        [UnityTest]
        public IEnumerator DefeatLoadsTheLastCampfireInAnotherRegion()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return CrossTrail(session, "expedition.clearing");
            Teleport(player, GameObject.Find("Clearing Campfire").transform.position);
            yield return null;
            Assert.That((string)session.GetType().GetProperty("ReturnCampfireId").GetValue(session),
                Is.EqualTo("campfire.expedition.clearing"));

            yield return CrossTrail(session, "home");
            yield return WaitForScene(false);
            Assert.That((string)session.GetType().GetProperty("ReturnCampfireId").GetValue(session),
                Is.EqualTo("campfire.expedition.clearing"));
            yield return TopazTestTravel.EnterWoodland(player);
            double before = (double)session.GetType().GetProperty("WorldHours").GetValue(session);
            Component vitality = player.GetComponent("PlayerVitality");
            Teleport(player, GameObject.Find("Clearing Campfire").transform.position + Vector3.right * 20f);
            for (int i = 0; i < 3; i++)
                vitality.GetType().GetMethod("TryTakeDamage").Invoke(vitality, new object[] { 4 });

            float deadline = Time.realtimeSinceStartup + 10f;
            while ((bool)session.GetType().GetProperty("IsRecovering").GetValue(session) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)session.GetType().GetProperty("IsRecovering").GetValue(session), Is.False);
            Assert.That(Region(session), Is.EqualTo("expedition.clearing"));
            Assert.That(SceneManager.GetSceneByName("Woodland").isLoaded, Is.True);
            Vector3 arrival = GameObject.Find("Clearing Campfire").transform
                .Find("Safe Arrival").position;
            Assert.That(Vector3.Distance(player.transform.position, arrival), Is.LessThan(.2f));
            Assert.That((double)session.GetType().GetProperty("WorldHours").GetValue(session) - before,
                Is.EqualTo(8d).Within(.05d));

            yield return CrossTrail(session, "home");
            yield return WaitForScene(false);
        }

        [UnityTest]
        public IEnumerator WoodlandCacheOpensWithoutGuardianAndRefillsAfterSeventyTwoHours()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return CrossTrail(session, "expedition.clearing");
            Transform guardian = GameObject.Find("Woodland").transform
                .Find("Wide-Sweep Guardian");
            ((Behaviour)guardian.GetComponent("EnemyCombatant")).enabled = false;
            Transform cache = GameObject.Find("Guarded Supply Cache").transform;
            Teleport(player, cache.position+Vector3.back*1.2f);
            AimAt(cache.position);
            yield return null;
            object[] cue = { null, null };
            Assert.That((bool)session.GetType().GetMethod("TryGetInteraction")
                .Invoke(session, cue), Is.True);
            Assert.That(cue[1], Is.EqualTo("Open cache"));
            Interact(session);
            Assert.That((int)session.GetType().GetProperty("WoodCount").GetValue(session),
                Is.EqualTo(3));
            Assert.That(cache.GetChild(0).gameObject.activeSelf, Is.True);
            Interact(session);
            Assert.That((int)session.GetType().GetProperty("WoodCount").GetValue(session),
                Is.EqualTo(3));

            yield return CrossTrail(session, "home");
            yield return WaitForScene(false);
            yield return CrossTrail(session, "expedition.clearing");
            Assert.That((bool)session.GetType().GetProperty("ExpeditionCacheClaimed")
                .GetValue(session), Is.True);
            object data = session.GetType().GetField("_data",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            FieldInfo clock = data.GetType().GetField("worldHours");
            clock.SetValue(data, (double)clock.GetValue(data) + 72.1d);
            yield return null;
            Assert.That((bool)session.GetType().GetProperty("ExpeditionCacheClaimed")
                .GetValue(session), Is.False);
            Teleport(player, GameObject.Find("Guarded Supply Cache").transform.position);
            Interact(session);
            Assert.That((int)session.GetType().GetProperty("WoodCount").GetValue(session),
                Is.EqualTo(6));
            yield return CrossTrail(session, "home");
        }

        static IEnumerator WaitForRegion(Component session, string expected)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Region(session) != expected && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Region(session), Is.EqualTo(expected));
            while ((bool)session.GetType().GetProperty("BlockMovement").GetValue(session) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        static IEnumerator CrossTrail(Component session, string destination)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!(bool)session.GetType().GetProperty("HasActivePair").GetValue(session) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            FieldInfo ready = session.GetType().GetField("_trailReadyAt",
                BindingFlags.Instance | BindingFlags.NonPublic);
            while (Time.time < (float)ready.GetValue(session) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            session.GetType().GetMethod("RequestTrailCrossing")
                .Invoke(session, new object[] { destination });
            yield return WaitForRegion(session, destination);
        }

        static IEnumerator WaitForScene(bool loaded)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetSceneByName("Woodland").isLoaded != loaded &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetSceneByName("Woodland").isLoaded, Is.EqualTo(loaded));
        }

        static string Region(Component session) =>
            (string)session.GetType().GetProperty("CurrentRegionId").GetValue(session);

        static void Interact(Component session) =>
            session.GetType().GetMethod("TryInteract").Invoke(session, null);

        static void Teleport(GameObject player, Vector3 destination)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = destination;
            controller.enabled = true;
            Physics.SyncTransforms();
            controller.Move(Vector3.down*.02f);
        }
    }
}
