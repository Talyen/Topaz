using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Topaz.Tests
{
    internal static class TopazTestTravel
    {
        public static IEnumerator EnterWoodland(GameObject player)
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

        static T Get<T>(Component component, string property) =>
            (T)component.GetType().GetProperty(property).GetValue(component);
    }
}
