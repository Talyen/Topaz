using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class SurvivalPlayTests : InputTestFixture
    {
        [TearDown]
        public void RestoreTime() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator ForageCookAndEatUseWorldAndCharacterState()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            float deadline = Time.realtimeSinceStartup + 12f;
            while (!(bool)Property(session, "HasActivePair") &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Property(session, "HasActivePair"), Is.True);
            Component mushrooms = GameObject.Find("mushrooms.home.west").GetComponent("ForagePlant");
            Component berries = GameObject.Find("berries.home.west").GetComponent("ForagePlant");
            Assert.That(Invoke<bool>(mushrooms, "TryForage"), Is.True);
            Assert.That(Invoke<bool>(berries, "TryForage"), Is.True);
            Assert.That(Property(mushrooms, "IsAvailable"), Is.False);
            Assert.That(Property(berries, "IsAvailable"), Is.False);

            var fire = (Component)session.GetType().GetField("homeCampfire",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance).GetValue(session);
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = fire.transform.position;
            controller.enabled = true;
            yield return null;
            Assert.That(Invoke<bool>(session, "TryCookStew"), Is.True);
            Assert.That((int)Property(session, "MushroomsAtFire"), Is.Zero);

            var slots = (System.Collections.IEnumerable)Property(session, "BackpackSlots");
            object[] stacks = slots.Cast<object>().ToArray();
            int berryIndex = System.Array.FindIndex(stacks, slot =>
                (string)slot.GetType().GetField("itemId").GetValue(slot) == "food.red-berries");
            int stewIndex = System.Array.FindIndex(stacks, slot =>
                (string)slot.GetType().GetField("itemId").GetValue(slot) == "food.mushroom-stew");
            Assert.That(Invoke<bool>(session, "TryEat", berryIndex), Is.True);
            Assert.That((int)Property(session, "FoodTier"), Is.EqualTo(1));
            Assert.That(Invoke<bool>(session, "TryEat", stewIndex), Is.True);
            Assert.That((int)Property(session, "FoodTier"), Is.EqualTo(2));
            Assert.That((double)Property(session, "FoodHoursRemaining"), Is.GreaterThan(23.9d));
            Component statusPage = GameObject.Find("Loop HUD").GetComponent("StatusJournalView");
            statusPage.GetType().GetMethod("Show").Invoke(statusPage, null);
            Assert.That(Property(statusPage, "IsOpen"), Is.True);
            Assert.That(statusPage.GetComponentsInChildren<Component>(true)
                .Where(value => value.GetType().Name == "TextMeshProUGUI")
                .Any(value =>
                {
                    string label = (string)value.GetType().GetProperty("text").GetValue(value);
                    return label != null && label.Contains("Mushroom Stew") &&
                        label.Contains("50%");
                }), Is.True);
            statusPage.GetType().GetMethod("Hide").Invoke(statusPage, null);
        }

        [UnityTest]
        public IEnumerator RestAdvancesWorldTimeOnceAndRefreshesHealthAndReserve()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            float deadline = Time.realtimeSinceStartup + 12f;
            while (!(bool)Property(session, "HasActivePair") &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Component berries = GameObject.Find("berries.home.east").GetComponent("ForagePlant");
            Assert.That(Invoke<bool>(berries, "TryForage"), Is.True);
            object[] stacks = ((System.Collections.IEnumerable)Property(session,
                "BackpackSlots")).Cast<object>().ToArray();
            int index = System.Array.FindIndex(stacks, slot =>
                (string)slot.GetType().GetField("itemId").GetValue(slot) == "food.red-berries");
            Assert.That(Invoke<bool>(session, "TryEat", index), Is.True);

            yield return TopazTestTravel.EnterGraveyard(player);
            Component vitality = player.GetComponent("PlayerVitality");
            Assert.That(Invoke<bool>(vitality, "TryTakeDamage", 2), Is.True);
            double before = (double)Property(session, "WorldHours");
            var rest = (IEnumerator)session.GetType().GetMethod("Rest",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance).Invoke(session, null);
            yield return ((MonoBehaviour)session).StartCoroutine(rest);
            Assert.That((double)Property(session, "WorldHours") - before,
                Is.EqualTo(8d).Within(.01d));
            Assert.That(Property(vitality, "CurrentHealth"),
                Is.EqualTo(Property(vitality, "MaximumHealth")));
            Assert.That((float)Property(session, "Stamina"), Is.EqualTo(125f));
            Assert.That((double)Property(session, "RestedHoursRemaining"),
                Is.EqualTo(24d).Within(.01d));
            Assert.That((double)Property(session, "FoodHoursRemaining"),
                Is.EqualTo(4d).Within(.01d));
        }

        static object Property(Component component, string name) =>
            component.GetType().GetProperty(name).GetValue(component);

        static T Invoke<T>(Component component, string method, params object[] args) =>
            (T)component.GetType().GetMethod(method).Invoke(component, args);
    }
}
