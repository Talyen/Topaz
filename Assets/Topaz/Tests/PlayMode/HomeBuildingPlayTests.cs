using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Topaz.Tests
{
    public sealed class HomeBuildingPlayTests : InputTestFixture
    {
        static object Field(object value, string name) => value.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .GetValue(value);
        static void Set(object value, string name, object data) => value.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SetValue(value, data);
        static object Property(object value, string name) => value.GetType()
            .GetProperty(name).GetValue(value);
        static object Call(object value, string name, params object[] args) => value.GetType()
            .GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Invoke(value, args);

        [UnityTest]
        public IEnumerator HomeIsSafeBeyondTheOldCircle()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component vitality = player.GetComponent("PlayerVitality");
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = new Vector3(8f, 0f, 1.5f);
            controller.enabled = true;
            Assert.That(Property(session, "IsAtHome"), Is.True);
            Assert.That(Call(vitality, "TryTakeDamage", 1), Is.False);
            GameObject enemy = GameObject.Find("Enemy");
            Assert.That(enemy, Is.Null,
                "The Home region must not contain a live practice enemy. " +
                (enemy == null ? "" : $"Scene={enemy.scene.name} Combatant=" +
                    (enemy.GetComponent("EnemyCombatant") != null)));
        }

        [UnityTest]
        public IEnumerator CampfireUpgradeSpendsMaterialsAndPersists()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            object backpack = Field(session, "_backpack");
            object wood = Property(session, "WoodItem");
            object stone = Property(session, "StoneItem");
            Assert.That(Call(backpack, "Add", wood, 9), Is.EqualTo(9));
            Assert.That(Call(backpack, "Add", stone, 6), Is.EqualTo(6));
            Assert.That(Property(session, "HomeCampfireTier"), Is.EqualTo(1));
            Assert.That(Call(session, "UpgradeHomeCampfire"), Is.True);
            Assert.That(Property(session, "HomeCampfireTier"), Is.EqualTo(2));
            Assert.That(Property(session, "HomeWoodCount"), Is.EqualTo(0));
            Assert.That(Property(session, "StoneCount"), Is.EqualTo(0));
            Call(session, "FlushCurrent");
            object loaded = Call(Field(session, "_repository"), "Load");
            object world = Call(loaded, "World", Property(session, "ActiveWorldId"));
            Assert.That(Field(world, "campfireTier"), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator TwoBuiltChestsHaveIndependentSavedRecords()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            object backpack = Field(session, "_backpack");
            object wood = Property(session, "WoodItem");
            Assert.That(Call(backpack, "Add", wood, 6), Is.EqualTo(6));
            object builds = Field(session, "homeBuilds");
            foreach (Vector3 position in new[] { new Vector3(3f, 0f, 3f),
                new Vector3(-3f, 0f, 3f) })
            {
                Assert.That(Call(session, "BeginHomeBuild", "structure.storage_chest"), Is.True);
                GameObject preview = (GameObject)Field(builds, "_preview");
                Assert.That(preview.GetComponentsInChildren<Renderer>().Length,
                    Is.GreaterThan(0), "A selected chest needs a visible placement ghost.");
                Set(builds, "_position", position);
                Call(builds, "ConfirmPlacement");
            }
            var records = ((IEnumerable)Field(Field(session, "_world"), "structures"))
                .Cast<object>().Where(value => (string)Field(value, "definitionId") ==
                    "structure.storage_chest").ToArray();
            Assert.That(records.Length, Is.EqualTo(2));
            Assert.That(Field(records[0], "instanceId"), Is.Not.EqualTo(Field(records[1], "instanceId")));
            Assert.That(Property(session, "HomeWoodCount"), Is.EqualTo(0));
            Call(session, "FlushCurrent");
            object loaded = Call(Field(session, "_repository"), "Load");
            object world = Call(loaded, "World", Property(session, "ActiveWorldId"));
            Assert.That(((IEnumerable)Field(world, "structures")).Cast<object>().Count(value =>
                (string)Field(value, "definitionId") == "structure.storage_chest"), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator HomeJournalActionsFitAtEveryUiScale()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component hud = GameObject.Find("Loop HUD").GetComponent("LoopHud");
            Component home = hud.GetComponent("HomeJournalView");
            Component menus = hud.GetComponent("GameMenus");
            Assert.That(home, Is.Not.Null);
            Assert.That(menus, Is.Not.Null);
            Call(home, "Show");
            RectTransform page = GameObject.Find("Home Journal").GetComponent<RectTransform>();
            Assert.That(Field(hud, "homeJournalBackground"), Is.Not.Null);
            Assert.That(page.GetComponent<Image>().sprite, Is.Not.Null,
                "The Home page should retain the Journal backdrop.");
            Assert.That(page.GetComponent<Image>().color, Is.EqualTo(Color.white));
            Assert.That(page.GetComponent<Image>().isActiveAndEnabled, Is.True);
            for (int scale = 0; scale < 3; scale++)
            {
                Call(menus, "SetUiScaleIndex", scale);
                Canvas.ForceUpdateCanvases();
                Call(home, "Layout");
                Vector3[] frame = new Vector3[4];
                page.GetWorldCorners(frame);
                foreach (Button button in page.GetComponentsInChildren<Button>(false))
                {
                    Vector3[] corners = new Vector3[4];
                    button.GetComponent<RectTransform>().GetWorldCorners(corners);
                    Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(frame[0].x - 1f));
                    Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(frame[0].y - 1f));
                    Assert.That(corners[2].x, Is.LessThanOrEqualTo(frame[2].x + 1f));
                    Assert.That(corners[2].y, Is.LessThanOrEqualTo(frame[2].y + 1f));
                }
            }
        }

        [UnityTest]
        public IEnumerator MovingAndRemovingAFullChestPreservesContentsAsPickups()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            object backpack = Field(session, "_backpack");
            Assert.That(Call(backpack, "Add", Property(session, "WoodItem"), 3), Is.EqualTo(3));
            object builds = Field(session, "homeBuilds");
            Assert.That(Call(session, "BeginHomeBuild", "structure.storage_chest"), Is.True);
            Set(builds, "_position", new Vector3(3f, 0f, 3f));
            Call(builds, "ConfirmPlacement");
            object record = ((IEnumerable)Field(Field(session, "_world"), "structures"))
                .Cast<object>().Single(value => (string)Field(value, "definitionId") ==
                    "structure.storage_chest");
            object chest = ((IEnumerable)Property(builds, "Chests")).Cast<object>().Single();
            object inventory = Property(chest, "Inventory");
            Assert.That(Call(inventory, "Add", Property(session, "StoneItem"), 4), Is.EqualTo(4));

            Assert.That(Call(session, "BeginHomeMove"), Is.True);
            Set(builds, "_selected", record);
            Set(builds, "_placingId", "structure.storage_chest");
            Set(builds, "_position", new Vector3(-3f, 0f, 3f));
            Call(builds, "ConfirmPlacement");
            Assert.That(Field(record, "x"), Is.EqualTo(-3f));
            Assert.That(Call(inventory, "Count", "material.stone"), Is.EqualTo(4));

            Assert.That(Call(session, "BeginHomeEdit"), Is.True);
            Set(builds, "_selected", record);
            Call(builds, "RemoveSelected");
            Assert.That(Property(session, "PickupCount"), Is.EqualTo(1));
            Assert.That(Property(session, "HomeWoodCount"), Is.EqualTo(3));
            Assert.That(((IEnumerable)Field(Field(session, "_world"), "structures"))
                .Cast<object>().Any(value => (string)Field(value, "definitionId") ==
                    "structure.storage_chest"), Is.False);
        }

        [UnityTest]
        public IEnumerator AnvilForgesGearFromHomeMaterialsWithoutLosingIt()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            object backpack = Field(session, "_backpack");
            Assert.That(Call(backpack, "Add", Property(session, "StoneItem"), 6), Is.EqualTo(6));
            Assert.That(Call(backpack, "Add", Property(session, "IronItem"), 5), Is.EqualTo(5));
            Assert.That(Call(backpack, "Add", Property(session, "WoodItem"), 2), Is.EqualTo(2));
            object builds = Field(session, "homeBuilds");
            Assert.That(Call(session, "BeginHomeBuild", "structure.blacksmith_anvil"), Is.True);
            Set(builds, "_position", new Vector3(3f, 0f, 3f));
            Call(builds, "ConfirmPlacement");
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = new Vector3(3f, 0f, 3f);
            controller.enabled = true;
            Type forge = Type.GetType("Topaz.LoopStudy.HomeForgeCatalog, Assembly-CSharp", true);
            object recipe = ((Array)forge.GetField("Recipes").GetValue(null)).GetValue(0);
            Assert.That(Call(session, "CanForge", recipe), Is.True);
            Assert.That(Call(session, "TryForge", recipe), Is.True);
            Assert.That(Call(backpack, "Count", "gear.sword.forged"), Is.EqualTo(1));
            Assert.That(Property(session, "IronCount"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator FinalCampfireStageOpensWalkableHomeBeyondTheCircle()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            object pack = Field(session, "_backpack");
            Assert.That(Call(pack, "Add", Property(session, "WoodItem"), 33), Is.EqualTo(33));
            Assert.That(Call(pack, "Add", Property(session, "StoneItem"), 30), Is.EqualTo(30));
            Assert.That(Call(pack, "Add", Property(session, "IronItem"), 8), Is.EqualTo(8));
            Assert.That(Call(session, "UpgradeHomeCampfire"), Is.True);
            Assert.That(Call(session, "UpgradeHomeCampfire"), Is.True);
            Assert.That(Property(session, "HomeCampfireTier"), Is.EqualTo(3));
            object builds = Field(session, "homeBuilds");
            Vector3 center = ((Component)Field(session, "home")).transform.position;
            Assert.That(Field(builds, "finalHomeArea"), Is.Not.Null);
            bool found = false;
            for (float radius = 10f; radius <= 20f && !found; radius += 2f)
                for (int angle = 0; angle < 16 && !found; angle++)
                {
                    float radians = angle * Mathf.PI / 8f;
                    Vector3 point = center + new Vector3(Mathf.Cos(radians) * radius,
                        0f, Mathf.Sin(radians) * radius);
                    point.x = Mathf.Round(point.x / .75f) * .75f;
                    point.z = Mathf.Round(point.z / .75f) * .75f;
                    found = (bool)Call(builds, "CanPlace", "structure.home.stone-floor",
                        point, 0, null);
                }
            Assert.That(found, Is.True,
                "The final upgrade should permit a clear Home ground spot past the old circle.");
        }

        [UnityTest]
        public IEnumerator BuiltBedRestsEightHoursWithoutAnIndoorRequirement()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            object pack = Field(session, "_backpack");
            Assert.That(Call(pack, "Add", Property(session, "WoodItem"), 4), Is.EqualTo(4));
            object builds = Field(session, "homeBuilds");
            Assert.That(Call(session, "BeginHomeBuild", "structure.home.bed"), Is.True);
            Set(builds, "_position", new Vector3(3f, 0f, 3f));
            Call(builds, "ConfirmPlacement");
            double before = (double)Property(session, "WorldHours");
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = new Vector3(3f, 0f, 3f);
            controller.enabled = true;
            Assert.That(Call(session, "TryInteract"), Is.True);
            yield return new WaitForSecondsRealtime(.75f);
            Assert.That((double)Property(session, "WorldHours") - before,
                Is.EqualTo(8d).Within(.15d));
        }
    }
}
