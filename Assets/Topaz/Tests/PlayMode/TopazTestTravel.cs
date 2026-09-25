using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Topaz.Tests
{
    internal static class TopazTestTravel
    {
        public static IEnumerator EnterGraveyard(GameObject player)
        {
            Component session = player.GetComponent("WorldSession");
            float deadline = Time.realtimeSinceStartup + 12f;
            while (!Get<bool>(session, "HasActivePair") && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Get<bool>(session, "HasActivePair"), Is.True);
            if (Get<string>(session, "CurrentRegionId") == "home")
            {
                FieldInfo ready = session.GetType().GetField("_trailReadyAt",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                while (Time.time < (float)ready.GetValue(session) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                session.GetType().GetMethod("RequestTrailCrossing")
                    .Invoke(session, new object[] { "expedition.clearing" });
            }
            while ((Get<string>(session, "CurrentRegionId") != "expedition.clearing" ||
                    Get<bool>(session, "BlockMovement")) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Get<string>(session, "CurrentRegionId"), Is.EqualTo("expedition.clearing"));
            Assert.That(Get<bool>(session, "BlockMovement"), Is.False);
            GameObject scout = GameObject.Find("Scout A");
            while ((scout == null || !scout.activeInHierarchy) &&
                   Time.realtimeSinceStartup < deadline)
            {
                scout = GameObject.Find("Scout A");
                yield return null;
            }
            Assert.That(scout, Is.Not.Null);
        }

        public static IEnumerator EnterCrypt(GameObject player)
        {
            Component session = player.GetComponent("WorldSession");
            if (Get<string>(session, "CurrentRegionId") == "home")
                yield return EnterGraveyard(player);
            GameObject entrance = GameObject.Find("Graveyard Crypt Entrance");
            Assert.That(entrance, Is.Not.Null);
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = entrance.transform.position;
            controller.enabled = true;
            Assert.That((bool)session.GetType().GetMethod("TryInteract")
                .Invoke(session, null), Is.True);
            float deadline = Time.realtimeSinceStartup + 12f;
            while ((Get<string>(session, "CurrentRegionId") != "dungeon.home-crypt" ||
                    Get<bool>(session, "BlockMovement")) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Get<string>(session, "CurrentRegionId"), Is.EqualTo("dungeon.home-crypt"));
            Assert.That(Get<bool>(session, "BlockMovement"), Is.False);
        }

        static T Get<T>(Component component, string property) =>
            (T)component.GetType().GetProperty(property).GetValue(component);
    }
}
