using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class CampfireTravelTests : TopazInputTestFixture
    {
        [TearDown]
        public void RestoreTime() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator FreshVisitShowsOnlyDisabledHomeAndClosesWithXOrCancel()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            yield return WaitForActive(session);
            Component fire = (Component)session.GetType().GetField("homeCampfire",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance).GetValue(session);
            Transform home = fire?.transform;
            Assert.That(home, Is.Not.Null);
            Teleport(session.gameObject, home.position);
            yield return null;
            Assert.That(Call<bool>(session, "TryInteract"), Is.True);
            yield return null;
            Transform panel = TravelPanel();
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Transform rows = panel.Find("Campfire Journal/Destinations/Viewport/Content");
            Assert.That(rows.childCount, Is.EqualTo(1));
            Assert.That(rows.GetChild(0).name, Is.EqualTo("Travel to First Hearth"));
            Assert.That(rows.GetChild(0).GetComponent<UnityEngine.UI.Button>().interactable,
                Is.False);
            Assert.That(rows.GetChild(0).Find("Campfire Icon")
                .GetComponent<UnityEngine.UI.Image>().sprite, Is.Not.Null);
            string[] labels = panel.GetComponentsInChildren<Component>(true)
                .Where(value => value.GetType().Name == "TextMeshProUGUI")
                .Select(value => (string)value.GetType().GetProperty("text").GetValue(value))
                .ToArray();
            Assert.That(labels, Does.Contain("TRAVEL"));
            Assert.That(labels, Does.Contain("COOK"));
            Assert.That(labels, Does.Contain("Mushroom Stew"));
            panel.Find("Campfire Journal/Back").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            yield return null;
            Assert.That(panel.gameObject.activeSelf, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            Assert.That(Call<bool>(session, "TryInteract"), Is.True);
            yield return null;
            Set(gamepad.buttonEast, 1f);
            yield return null;
            Set(gamepad.buttonEast, 0f);
            Assert.That(panel.gameObject.activeSelf, Is.False);

            Component journal = GameObject.Find("Loop HUD").GetComponent("BuildingJournalView");
            journal.GetType().GetMethod("Show").Invoke(journal, null);
            Assert.That((bool)journal.GetType().GetProperty("IsOpen").GetValue(journal), Is.True);
            Assert.That(journal.GetComponentsInChildren<Component>(true).Any(value => value.GetType().Name == "TextMeshProUGUI" && (string)value.GetType().GetProperty("text").GetValue(value) == "Improve fire"), Is.False);
        }

        [UnityTest]
        public IEnumerator WoodlandTravelPreservesHealthAndClockAndSetsReturnFire()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            yield return TopazTestTravel.EnterWoodland(player);
            var pack=session.GetType().GetField("_backpack",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(session);
            pack.GetType().GetMethod("Add").Invoke(pack,new[]{session.GetType().GetProperty("WoodItem").GetValue(session),(object)20});
            pack.GetType().GetMethod("Add").Invoke(pack,new[]{session.GetType().GetProperty("StoneItem").GetValue(session),(object)20});
            Vector3 campPosition=BuildingTestActions.Place(session,"structure.campfire");
            Teleport(player,campPosition+Vector3.forward);
            yield return null;
            Assert.That(Get<string>(session, "ReturnCampfireId"), Is.Not.EqualTo("campfire.home"));
            Component vitality = player.GetComponent("PlayerVitality");
            var firePosition = player.transform.position;
            Teleport(player, firePosition + Vector3.right * 20f);
            Assert.That(Call<bool>(vitality, "TryTakeDamage", 2), Is.True);
            Teleport(player, firePosition);
            AimAt(campPosition+Vector3.up*.5f);
            yield return new WaitForSeconds(.5f);
            int health = Get<int>(vitality, "CurrentHealth");
            Assert.That(health, Is.LessThan(Get<int>(vitality, "MaximumHealth")));
            Assert.That(Call<bool>(session, "TryInteract"), Is.True);
            yield return null;
            Assert.That(Time.timeScale, Is.Zero);
            double hours = Get<double>(session, "WorldHours");
            Assert.That(Call<bool>(vitality, "TryTakeDamage", 1), Is.False,
                "The open Travel menu pauses damage during combat.");
            TravelPanel().Find("Campfire Journal/Destinations/Viewport/Content/Travel to First Hearth")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return WaitForTravel(session, "wilderness");
            Assert.That(Get<int>(vitality, "CurrentHealth"), Is.EqualTo(health));
            Assert.That(Get<double>(session, "WorldHours") - hours,
                Is.LessThan(.05d));
            Assert.That(Get<string>(session, "ReturnCampfireId"),
                Is.EqualTo("campfire.home"));
            Assert.That(SceneManager.GetSceneByName("Woodland").isLoaded, Is.False);
        }

        static Transform TravelPanel() => GameObject.Find("Loop HUD").transform
            .Find("Campfire Travel");

        static IEnumerator WaitForActive(Component session)
        {
            float deadline = Time.realtimeSinceStartup + 12f;
            while (!Get<bool>(session, "HasActivePair") && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Get<bool>(session, "HasActivePair"), Is.True);
        }

        static IEnumerator WaitForTravel(Component session, string region)
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while ((Get<string>(session, "CurrentRegionId") != region ||
                    Get<bool>(session, "IsFastTraveling")) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Get<string>(session, "CurrentRegionId"), Is.EqualTo(region));
            Assert.That(Get<bool>(session, "IsFastTraveling"), Is.False);
            yield return null;
        }

        static void Teleport(GameObject player, Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
        }

        static T Get<T>(Component component, string property) =>
            (T)component.GetType().GetProperty(property).GetValue(component);

        static T Call<T>(Component component, string method, params object[] args) =>
            (T)component.GetType().GetMethod(method).Invoke(component, args);
    }
}
