using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class PlayerLanternTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator BackpackLanternTogglesTheWorldLightWithoutUsingAStackSlot()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            GameObject player = GameObject.Find("Player");
            GameObject canvas = GameObject.Find("Loop HUD");
            Component session = player.GetComponent("WorldSession");
            Component lantern = player.GetComponent("PlayerLantern");
            Component hud = canvas.GetComponent("LoopHud");
            Assert.That(session, Is.Not.Null);
            Assert.That(lantern, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            Transform entry = canvas.transform.Find("Backpack/Open Journal/Lantern Entry");
            Assert.That(entry, Is.Not.Null);
            UnityEngine.UI.Button button = entry.GetComponent<UnityEngine.UI.Button>();
            Component label = entry.GetComponentsInChildren<Component>(true)
                .FirstOrDefault(candidate => candidate.GetType().Name == "TextMeshProUGUI");
            Assert.That(button, Is.Not.Null);
            Assert.That(label, Is.Not.Null);
            var corners = new Vector3[4];
            entry.GetComponent<RectTransform>().GetWorldCorners(corners);
            Light light = player.GetComponentsInChildren<Light>(true)
                .FirstOrDefault(candidate => candidate.name == "Lantern Light");
            Assert.That(light, Is.Not.Null);
            Assert.That(light.type, Is.EqualTo(LightType.Point));
            Assert.That(light.cookie, Is.Null);
            Assert.That(light.transform.localPosition.x, Is.EqualTo(0f).Within(.001f));
            Assert.That(light.transform.localPosition.z, Is.EqualTo(0f).Within(.001f));
            Assert.That(light.range, Is.EqualTo(8.5f).Within(.001f));
            Transform carried = player.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == "Carried Lantern");
            Assert.That(carried, Is.Not.Null);
            Renderer lanternBody = carried.GetComponentsInChildren<Renderer>(true)
                .FirstOrDefault(candidate => candidate.name == "Visual");
            Assert.That(lanternBody, Is.Not.Null);
            Assert.That(Vector3.Distance(lanternBody.bounds.center, carried.position),
                Is.LessThan(.3f), "The lantern mesh should hang at its mount, not beside the player.");
            Assert.That(light.color.r, Is.GreaterThan(light.color.g));
            Assert.That(light.color.g, Is.GreaterThan(light.color.b));
            Assert.That(GameObject.Find("KayKit Lantern").GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Any(material => material != null && material.name == "Lantern Glass" &&
                    material.GetColor("_EmissionColor") == Color.black), Is.True);
            Assert.That((bool)session.GetType().GetProperty("LanternOn").GetValue(session), Is.False);
            Assert.That(light.enabled, Is.False);
            var stacks = (IEnumerable)session.GetType().GetProperty("BackpackSlots")
                .GetValue(session);
            foreach (object stack in stacks)
                Assert.That(stack.GetType().GetField("itemId").GetValue(stack),
                    Is.Not.EqualTo("gear.lantern"));

            hud.GetType().GetMethod("ToggleInventoryPanel").Invoke(hud, null);
            yield return null;
            Assert.That(entry.gameObject.activeInHierarchy, Is.True);
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = (corners[0] + corners[2]) * 0.5f
            };
            var hits = new List<RaycastResult>();
            canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().Raycast(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0].gameObject.transform.IsChildOf(entry), Is.True,
                $"Lantern entry is covered by {hits[0].gameObject.name}.");
            button.onClick.Invoke();
            Assert.That((bool)session.GetType().GetProperty("LanternOn").GetValue(session), Is.True);
            Assert.That(light.enabled, Is.True);
            Assert.That(label.GetType().GetProperty("text").GetValue(label),
                Does.Contain("On"));
            Assert.That(entry.gameObject.activeInHierarchy, Is.True,
                "The backpack should stay open for an immediate second toggle.");

            button.onClick.Invoke();
            Assert.That((bool)session.GetType().GetProperty("LanternOn").GetValue(session), Is.False);
            Assert.That(light.enabled, Is.False);
            Assert.That(label.GetType().GetProperty("text").GetValue(label),
                Does.Contain("Off"));

            Scene bootstrap = SceneManager.GetSceneByName("Bootstrap");
            Scene empty = SceneManager.CreateScene("Lantern Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(bootstrap);
        }
    }
}
