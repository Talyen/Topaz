using System.Collections;
using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class ExpeditionTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator SavedExpeditionRestoresScenePositionAndEmptyCache()
        {
            string directory = Path.Combine(Path.GetTempPath(),
                "TopazExpeditionResume-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            Type sessionType = Type.GetType("Topaz.LoopStudy.WorldSession, Assembly-CSharp", true);
            Type repositoryType = Type.GetType("Topaz.LoopStudy.SaveRepository, Assembly-CSharp", true);
            Type dataType = Type.GetType("Topaz.LoopStudy.TopazSaveData, Assembly-CSharp", true);
            object repository = Activator.CreateInstance(repositoryType, directory);
            object data = Activator.CreateInstance(dataType);
            dataType.GetField("regionId").SetValue(data, "expedition.clearing");
            dataType.GetField("playerX").SetValue(data, 100f);
            dataType.GetField("playerZ").SetValue(data, -10f);
            dataType.GetField("expeditionCacheClaimed").SetValue(data, true);
            repositoryType.GetMethod("Save").Invoke(repository, new[] { data });
            sessionType.GetProperty("EditorTestSaveDirectory").SetValue(null, directory);
            Component session = null;
            try
            {
                yield return SceneManager.LoadSceneAsync("Bootstrap");
                GameObject player = GameObject.Find("Player");
                session = player.GetComponent("WorldSession");
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!SceneManager.GetSceneByName("Expedition").isLoaded ||
                        Mathf.Abs(player.transform.position.x - 100f) > .1f) &&
                       Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(SceneManager.GetSceneByName("Expedition").isLoaded, Is.True);
                Assert.That(player.transform.position.x, Is.EqualTo(100f).Within(.1f));
                Assert.That(player.transform.position.z, Is.EqualTo(-10f).Within(.1f));
                Assert.That((bool)sessionType.GetProperty("ExpeditionCacheClaimed").GetValue(session), Is.True);
                Transform cache = GameObject.Find("Guarded Supply Cache").transform;
                Assert.That(cache.GetChild(0).gameObject.activeSelf, Is.False);
            }
            finally
            {
                sessionType.GetProperty("EditorTestSaveDirectory").SetValue(null, null);
                if (session != null)
                {
                    object activeRepository = sessionType.GetField("_repository",
                        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                    activeRepository.GetType().GetMethod("Flush").Invoke(activeRepository, null);
                }
            }
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            Directory.Delete(directory, true);
        }

        [UnityTest]
        public IEnumerator SwordCanDamageAnExpeditionScout()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Teleport(player, GameObject.Find("Expedition Gate").transform.position);
            Interact(session);
            yield return WaitForRegion(session, "expedition.clearing");

            Transform scout = GameObject.Find("Expedition Clearing").transform.Find("Scout A");
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!scout.gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(scout.gameObject.activeSelf, Is.True);
            Component combatant = scout.GetComponent("EnemyCombatant");
            int before = (int)combatant.GetType().GetProperty("CurrentHealth").GetValue(combatant);
            Vector3 screenRight = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
            Teleport(player, scout.position - screenRight * 1.35f);
            Set(gamepad.rightStick, Vector2.right);
            yield return null;
            Set(gamepad.rightTrigger, 1f);
            yield return new WaitForSeconds(.35f);
            Set(gamepad.rightTrigger, 0f);
            Assert.That((int)combatant.GetType().GetProperty("CurrentHealth").GetValue(combatant),
                Is.LessThan(before), "The sword should hit enemies in the loaded clearing.");

            Teleport(player, GameObject.Find("Return Trail Marker").transform.position);
            Interact(session);
            yield return WaitForRegion(session, "home");
            yield return WaitForScene(false);
        }

        [UnityTest]
        public IEnumerator PracticeDefeatReturnsPlayerAndSaveRegionHome()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Teleport(player, GameObject.Find("Expedition Gate").transform.position);
            Interact(session);
            yield return WaitForRegion(session, "expedition.clearing");

            Component vitality = player.GetComponent("PlayerVitality");
            for (int i = 0; i < 3; i++)
                vitality.GetType().GetMethod("TryTakeDamage").Invoke(vitality, new object[] { 1 });
            yield return WaitForRegion(session, "home");
            yield return WaitForScene(false);
            Assert.That((int)vitality.GetType().GetProperty("CurrentHealth").GetValue(vitality),
                Is.EqualTo(3));
            Assert.That(player.transform.position.sqrMagnitude, Is.LessThan(1f));
        }

        [UnityTest]
        public IEnumerator GuardedWoodPersistsAcrossReturnAndReentry()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Assert.That(session, Is.Not.Null);
            Assert.That(Region(session), Is.EqualTo("home"));

            Teleport(player, GameObject.Find("Expedition Gate").transform.position);
            Interact(session);
            yield return WaitForRegion(session, "expedition.clearing");
            Assert.That(SceneManager.GetSceneByName("Expedition").isLoaded, Is.True);
            Transform guardianTransform = GameObject.Find("Expedition Clearing").transform.Find("Wide-Sweep Guardian");
            Assert.That(guardianTransform, Is.Not.Null);
            float readyDeadline = Time.realtimeSinceStartup + 5f;
            while (!guardianTransform.gameObject.activeSelf && Time.realtimeSinceStartup < readyDeadline)
                yield return null;
            Assert.That(guardianTransform.gameObject.activeSelf, Is.True);
            Transform cache = GameObject.Find("Guarded Supply Cache").transform;
            Teleport(player, cache.position);
            yield return null;
            object[] cue = { null, null };
            Assert.That((bool)session.GetType().GetMethod("TryGetInteraction")
                .Invoke(session, cue), Is.False,
                "The locked supply cache must not offer an action chip.");
            GameObject guardian = guardianTransform.gameObject;
            Component combatant = guardian.GetComponent("EnemyCombatant");
            combatant.GetType().GetMethod("TakeDamage").Invoke(combatant, new object[] { 99 });
            yield return null;

            int beforeWood = (int)session.GetType().GetProperty("WoodCount").GetValue(session);
            Teleport(player, cache.position);
            yield return null;
            cue = new object[] { null, null };
            Assert.That((bool)session.GetType().GetMethod("TryGetInteraction")
                .Invoke(session, cue), Is.True);
            Assert.That(cue[1], Is.EqualTo("Open cache"));
            Interact(session);
            Assert.That((int)session.GetType().GetProperty("WoodCount").GetValue(session),
                Is.EqualTo(beforeWood + 3));
            Assert.That((bool)session.GetType().GetProperty("ExpeditionCacheClaimed").GetValue(session), Is.True);

            Teleport(player, GameObject.Find("Return Trail Marker").transform.position);
            Interact(session);
            yield return WaitForRegion(session, "home");
            yield return WaitForScene(false);
            Assert.That(SceneManager.GetSceneByName("Expedition").isLoaded, Is.False);
            Teleport(player, GameObject.Find("Expedition Gate").transform.position);
            Interact(session);
            yield return WaitForRegion(session, "expedition.clearing");
            Assert.That((bool)session.GetType().GetProperty("ExpeditionCacheClaimed").GetValue(session), Is.True);
            Assert.That((int)session.GetType().GetProperty("WoodCount").GetValue(session),
                Is.EqualTo(beforeWood + 3));
            Transform reenteredCache = GameObject.Find("Guarded Supply Cache").transform;
            Assert.That(reenteredCache.GetChild(0).gameObject.activeSelf, Is.False,
                "The claimed cache must stay empty on reentry.");
            Teleport(player, GameObject.Find("Return Trail Marker").transform.position);
            Interact(session);
            yield return WaitForRegion(session, "home");
            yield return WaitForScene(false);
        }

        static IEnumerator WaitForRegion(Component session, string expected)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Region(session) != expected && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Region(session), Is.EqualTo(expected));
        }

        static IEnumerator WaitForScene(bool loaded)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetSceneByName("Expedition").isLoaded != loaded &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetSceneByName("Expedition").isLoaded, Is.EqualTo(loaded));
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
        }
    }
}
