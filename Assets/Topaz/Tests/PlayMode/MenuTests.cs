using System.Collections;
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class MenuTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator EscapePausesAndResumesTheGame()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;

            Component menus = GameObject.Find("Loop HUD").GetComponent("GameMenus");
            Assert.That(menus, Is.Not.Null);
            Assert.That((bool)menus.GetType().GetProperty("IsPaused").GetValue(menus), Is.False);
            Press(keyboard.escapeKey);
            yield return null;
            Assert.That((bool)menus.GetType().GetProperty("IsPaused").GetValue(menus), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f),
                "Editor tests keep simulation time running while checking menu navigation.");
            Release(keyboard.escapeKey);
            yield return null;
            Press(keyboard.escapeKey);
            yield return null;
            Assert.That((bool)menus.GetType().GetProperty("IsPaused").GetValue(menus), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Release(keyboard.escapeKey);
        }

        [UnityTest]
        public IEnumerator TitleAndOptionsPanelsAreWired()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            Assert.That(hud.transform.Find("Desktop Menus/Title Screen"), Is.Not.Null);
            Assert.That(hud.transform.Find("Desktop Menus/Pause Screen"), Is.Not.Null);
            Assert.That(hud.transform.Find("Desktop Menus/Options Screen"), Is.Not.Null);
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            Assert.That((bool)menus.GetType().GetProperty("IsTitle").GetValue(menus), Is.True);
            menus.GetType().GetMethod("Continue").Invoke(menus, null);
            Assert.That((bool)menus.GetType().GetProperty("IsTitle").GetValue(menus), Is.False);
        }

        [UnityTest]
        public IEnumerator TitleUsesTheCampfireCameraAndGameRestoresTheWorldCamera()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            Camera menuCamera = GameObject.Find("Main Menu Camera").GetComponent<Camera>();
            Camera worldCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Assert.That(menuCamera.enabled, Is.True);
            Assert.That(worldCamera.enabled, Is.False);
            Component cameraData = menuCamera.GetComponent("UniversalAdditionalCameraData");
            Assert.That(cameraData, Is.Not.Null);
            Assert.That((bool)cameraData.GetType().GetProperty("renderPostProcessing")
                .GetValue(cameraData), Is.True);
            menus.GetType().GetMethod("Continue").Invoke(menus, null);
            Assert.That(menuCamera.enabled, Is.False);
            Assert.That(worldCamera.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator GraphicsMenuTemporarilyReplacesOptionsCard()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            Component visualLab = hud.GetComponent("VisualOptionsMenu");
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            var titleOptions = hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Options")
                .GetComponent<UnityEngine.UI.Button>();
            titleOptions.onClick.Invoke();
            GameObject options = hud.transform.Find("Desktop Menus/Options Screen").gameObject;
            Assert.That(options.activeSelf, Is.True);
            visualLab.GetType().GetMethod("Toggle").Invoke(visualLab, null);
            Assert.That(options.activeSelf, Is.False);
            Assert.That(hud.transform.Find("Graphics").gameObject.activeSelf, Is.True);
            visualLab.GetType().GetMethod("Close").Invoke(visualLab, null);
            Assert.That(options.activeSelf, Is.True);
            menus.GetType().GetMethod("Continue").Invoke(menus, null);
        }

        [UnityTest]
        public IEnumerator UiScaleCyclesThroughThreeSizesAndKeepsOptionsFocus()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Options")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            var scaleButton = hud.transform.Find(
                "Desktop Menus/Options Screen/Options Composition/UI Scale: 100%")
                .GetComponent<UnityEngine.UI.Button>();
            var scaler = hud.GetComponent<UnityEngine.UI.CanvasScaler>();
            Assert.That(scaler.referenceResolution.x, Is.EqualTo(1920f).Within(.1f));

            scaleButton.onClick.Invoke();
            Assert.That((int)menus.GetType().GetProperty("CurrentUiScaleIndex")
                .GetValue(menus), Is.EqualTo(1));
            Assert.That(scaler.referenceResolution.x, Is.EqualTo(1536f).Within(.1f));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(scaleButton.gameObject));

            scaleButton.onClick.Invoke();
            Assert.That(scaler.referenceResolution.x, Is.EqualTo(1280f).Within(.1f));
            scaleButton.onClick.Invoke();
            Assert.That(scaler.referenceResolution.x, Is.EqualTo(1920f).Within(.1f));
        }

        [UnityTest]
        public IEnumerator LargeUiScaleKeepsTheJournalInsideTheCanvas()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            menus.GetType().GetMethod("SetUiScaleIndex").Invoke(menus, new object[] { 2 });
            Component inventory = hud.GetComponent("LoopHud");
            inventory.GetType().GetMethod("ToggleInventoryPanel").Invoke(inventory, null);
            yield return null;
            yield return null;

            RectTransform canvas = hud.GetComponent<RectTransform>();
            RectTransform book = hud.transform.Find("Backpack/Open Journal")
                .GetComponent<RectTransform>();
            Assert.That(book.GetComponent("TopazJournalFit"), Is.Not.Null);
            Assert.That(book.localScale.x, Is.LessThan(.8f),
                $"Canvas {canvas.rect.size}, book scale {book.localScale.x}");
            GameObject focused = EventSystem.current.currentSelectedGameObject;
            Assert.That(focused, Is.Not.Null);
            Assert.That(focused.transform.Find("Focus Frame")
                ?.GetComponent<UnityEngine.UI.Image>().enabled, Is.True,
                "The selected Journal control needs a visible focus frame.");
            var corners = new Vector3[4];
            book.GetWorldCorners(corners);
            foreach (Vector3 world in corners)
            {
                Vector3 local = canvas.InverseTransformPoint(world);
                Assert.That(local.x, Is.InRange(canvas.rect.xMin - 1f, canvas.rect.xMax + 1f));
                Assert.That(local.y, Is.InRange(canvas.rect.yMin - 1f, canvas.rect.yMax + 1f));
            }
        }

        [UnityTest]
        public IEnumerator LargeUiScaleKeepsTitleAndOptionsControlsReachable()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            menus.GetType().GetMethod("SetUiScaleIndex").Invoke(menus, new object[] { 2 });
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            yield return null;
            yield return null;
            RectTransform canvas = hud.GetComponent<RectTransform>();
            AssertInside(canvas, hud.transform.Find(
                "Desktop Menus/Title Screen/Title Composition/Quit").GetComponent<RectTransform>());

            hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Options")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            yield return null;
            AssertInside(canvas, hud.transform.Find(
                "Desktop Menus/Options Screen/Options Composition/Back").GetComponent<RectTransform>());
            Assert.That(hud.transform.Find("Desktop Menus/Options Screen/Options Composition")
                .GetComponent("TopazResponsiveOptions"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator EquipmentSlotsStayOnTheirJournalPageAtLargeScale()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            hud.GetComponent("GameMenus").GetType().GetMethod("SetUiScaleIndex")
                .Invoke(hud.GetComponent("GameMenus"), new object[] { 2 });
            Component inventory = hud.GetComponent("LoopHud");
            inventory.GetType().GetMethod("ShowEquipmentPanel").Invoke(inventory, null);
            yield return null;
            yield return null;
            RectTransform book = hud.transform.Find("Equipment/Open Journal")
                .GetComponent<RectTransform>();
            RectTransform weapon = book.Find("Gear Weapon").GetComponent<RectTransform>();
            var corners = new Vector3[4];
            weapon.GetWorldCorners(corners);
            foreach (Vector3 world in corners)
                Assert.That(book.InverseTransformPoint(world).x, Is.LessThanOrEqualTo(0f),
                    "Equipment choices should not overlap the journal spine.");
            Assert.That(book.Find("Skills").gameObject.activeSelf, Is.False,
                "Detailed skill progress belongs on the Skills page.");
        }

        [UnityTest]
        public IEnumerator LargeUiScaleKeepsGraphicsAndCharacterSelectionVisible()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            menus.GetType().GetMethod("SetUiScaleIndex").Invoke(menus, new object[] { 2 });
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Options")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            hud.GetComponent("VisualOptionsMenu").GetType().GetMethod("Toggle")
                .Invoke(hud.GetComponent("VisualOptionsMenu"), null);
            yield return null;
            yield return null;
            RectTransform canvas = hud.GetComponent<RectTransform>();
            AssertInside(canvas, hud.transform.Find("Graphics").GetComponent<RectTransform>());

            hud.GetComponent("VisualOptionsMenu").GetType().GetMethod("Close")
                .Invoke(hud.GetComponent("VisualOptionsMenu"), null);
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Play")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            yield return null;
            AssertInside(canvas, hud.transform.Find(
                "Desktop Menus/Character Selection/Iron Selection Card")
                .GetComponent<RectTransform>());
        }

        static void AssertInside(RectTransform canvas, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            foreach (Vector3 world in corners)
            {
                Vector3 local = canvas.InverseTransformPoint(world);
                Assert.That(local.x, Is.InRange(canvas.rect.xMin - 1f, canvas.rect.xMax + 1f));
                Assert.That(local.y, Is.InRange(canvas.rect.yMin - 1f, canvas.rect.yMax + 1f));
            }
        }

        [UnityTest]
        public IEnumerator BackpackOpenedFromPauseReturnsToPauseWhenClosed()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            Component inventory = hud.GetComponent("LoopHud");
            menus.GetType().GetMethod("HandleEscape").Invoke(menus, null);
            Assert.That((bool)menus.GetType().GetProperty("IsPaused").GetValue(menus), Is.True);
            hud.transform.Find("Desktop Menus/Pause Screen/Pause Composition/Backpack")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That((bool)inventory.GetType().GetProperty("MenuOpen").GetValue(inventory), Is.True);
            Assert.That((bool)menus.GetType().GetProperty("BlockGameplay").GetValue(menus), Is.True);
            hud.transform.Find("Backpack/Open Journal/Close")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That((bool)menus.GetType().GetProperty("IsPaused").GetValue(menus), Is.True);
        }

        [UnityTest]
        public IEnumerator GamepadCanOpenCharacterSelectionFromTitle()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("Continue"));
            Set(gamepad.dpad, Vector2.down);
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("Play"));
            Set(gamepad.dpad, Vector2.zero);
            Set(gamepad.buttonSouth, 1f);
            yield return null;
            Assert.That(hud.transform.Find("Desktop Menus/Character Selection").gameObject.activeSelf,
                Is.True);
            Set(gamepad.buttonSouth, 0f);
            menus.GetType().GetMethod("Continue").Invoke(menus, null);
        }

        [UnityTest]
        public IEnumerator CharacterDeletionRequiresConfirmationAndClearsContinuePair()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Play")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Transform selection = hud.transform.Find("Desktop Menus/Character Selection");
            Transform row = selection.Find(
                "Iron Selection Card/Characters/Character List/Viewport/Content").GetChild(0);
            int worldsBefore = ((System.Collections.IList)session.GetType()
                .GetProperty("Worlds").GetValue(session)).Count;
            row.GetChild(1).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(selection.Find("Delete Confirmation").gameObject.activeSelf, Is.True);
            Assert.That(((System.Collections.IList)session.GetType()
                .GetProperty("Characters").GetValue(session)).Count, Is.EqualTo(1));
            selection.Find("Delete Confirmation/Confirmation Card/Cancel")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(((System.Collections.IList)session.GetType()
                .GetProperty("Characters").GetValue(session)).Count, Is.EqualTo(1));
            row.GetChild(1).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            selection.Find("Delete Confirmation/Confirmation Card/Delete")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(((System.Collections.IList)session.GetType()
                .GetProperty("Characters").GetValue(session)).Count, Is.Zero);
            Assert.That(((System.Collections.IList)session.GetType()
                .GetProperty("Worlds").GetValue(session)).Count, Is.EqualTo(worldsBefore));
            Assert.That((bool)session.GetType().GetProperty("HasActivePair")
                .GetValue(session), Is.False);
            Assert.That((bool)session.GetType().GetProperty("HasLastPair")
                .GetValue(session), Is.False);
        }

        [UnityTest]
        public IEnumerator WorldDeletionRequiresConfirmationAndKeepsCharacter()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Play")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Transform selection = hud.transform.Find("Desktop Menus/Character Selection");
            selection.Find("Iron Selection Card/Characters/Character List/Viewport/Content")
                .GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Transform row = selection.Find(
                "Iron Selection Card/Worlds/World List/Viewport/Content").GetChild(0);
            row.GetChild(1).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(((System.Collections.IList)session.GetType()
                .GetProperty("Worlds").GetValue(session)).Count, Is.EqualTo(1));
            selection.Find("Delete Confirmation/Confirmation Card/Delete")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(((System.Collections.IList)session.GetType()
                .GetProperty("Worlds").GetValue(session)).Count, Is.Zero);
            Assert.That(((System.Collections.IList)session.GetType()
                .GetProperty("Characters").GetValue(session)).Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PlayCanCreateKnightInFreshWorldAndReturnToOriginalPair()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            Component appearance = GameObject.Find("Player").GetComponent("PlayerAppearance");
            string firstCharacter = (string)session.GetType().GetProperty("ActiveCharacterId").GetValue(session);
            string firstWorld = (string)session.GetType().GetProperty("ActiveWorldId").GetValue(session);
            object activeData = session.GetType().GetField("_data",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            activeData.GetType().GetField("worldHours").SetValue(activeData, 32d);
            session.GetType().GetMethod("RecordLoggingChop").Invoke(session, new object[] { 5 });
            session.GetType().GetMethod("Commit").Invoke(session, null);
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            Transform selection = hud.transform.Find("Desktop Menus/Character Selection/Iron Selection Card");
            hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Play")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(selection.parent.gameObject.activeSelf, Is.True);
            selection.Find("Characters/New Character").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            yield return null;
            Transform lookList = selection.Find("Looks/Look List/Viewport/Content");
            Assert.That(lookList.childCount, Is.EqualTo(6));
            lookList.GetChild(2).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            selection.Find("Looks/Create Character").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            selection.Find("Worlds/New World").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            selection.Find("Worlds/Enter World").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            yield return WaitForPair(session, firstCharacter, firstWorld);
            string knightId = (string)session.GetType().GetProperty("ActiveCharacterId").GetValue(session);
            string secondWorld = (string)session.GetType().GetProperty("ActiveWorldId").GetValue(session);
            Assert.That(knightId, Is.Not.EqualTo(firstCharacter));
            Assert.That(secondWorld, Is.Not.EqualTo(firstWorld));
            Assert.That((string)appearance.GetType().GetProperty("CurrentId").GetValue(appearance),
                Is.EqualTo("knight"));
            Assert.That((double)session.GetType().GetProperty("WorldHours").GetValue(session),
                Is.LessThan(9d), "A fresh World begins on its own first day.");
            Assert.That((int)session.GetType().GetProperty("LoggingExperience").GetValue(session),
                Is.Zero, "Knight does not inherit Rogue's skill progress.");

            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Play")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            selection.Find("Characters/Character List/Viewport/Content").GetChild(0)
                .GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            selection.Find("Worlds/World List/Viewport/Content").GetChild(0)
                .GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            selection.Find("Worlds/Enter World").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 5f;
            while ((string)session.GetType().GetProperty("ActiveCharacterId").GetValue(session) !=
                firstCharacter && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((string)session.GetType().GetProperty("ActiveCharacterId").GetValue(session),
                Is.EqualTo(firstCharacter));
            Assert.That((string)session.GetType().GetProperty("ActiveWorldId").GetValue(session),
                Is.EqualTo(firstWorld));
            Assert.That((string)appearance.GetType().GetProperty("CurrentId").GetValue(appearance),
                Is.EqualTo("rogue"));
            Assert.That((double)session.GetType().GetProperty("WorldHours").GetValue(session),
                Is.GreaterThanOrEqualTo(32d));
            Assert.That((int)session.GetType().GetProperty("LoggingExperience").GetValue(session),
                Is.EqualTo(5));

            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Play")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            selection.Find("Characters/Character List/Viewport/Content").GetChild(1)
                .GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            selection.Find("Worlds/World List/Viewport/Content").GetChild(0)
                .GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            selection.Find("Worlds/Enter World").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            deadline = Time.realtimeSinceStartup + 5f;
            while ((string)session.GetType().GetProperty("ActiveCharacterId").GetValue(session) !=
                knightId && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((string)session.GetType().GetProperty("ActiveWorldId").GetValue(session),
                Is.EqualTo(firstWorld));
            Assert.That((double)session.GetType().GetProperty("WorldHours").GetValue(session),
                Is.GreaterThanOrEqualTo(32d), "Both Characters see the same World time.");
            Assert.That((int)session.GetType().GetProperty("LoggingExperience").GetValue(session),
                Is.Zero, "Knight keeps independent skills in the shared World.");
        }

        [UnityTest]
        public IEnumerator BackFromDraftDoesNotCreateCharacterOrWorld()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            int charactersBefore = ((System.Collections.IList)session.GetType()
                .GetProperty("Characters").GetValue(session)).Count;
            int worldsBefore = ((System.Collections.IList)session.GetType()
                .GetProperty("Worlds").GetValue(session)).Count;
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            Transform selection = hud.transform.Find("Desktop Menus/Character Selection/Iron Selection Card");
            hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Play")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            selection.Find("Characters/New Character").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            selection.Find("Looks/Create Character").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            selection.Find("Worlds/Back").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            selection.Find("Looks/Back").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            Assert.That(((System.Collections.IList)session.GetType().GetProperty("Characters")
                .GetValue(session)).Count, Is.EqualTo(charactersBefore));
            Assert.That(((System.Collections.IList)session.GetType().GetProperty("Worlds")
                .GetValue(session)).Count, Is.EqualTo(worldsBefore));
        }

        [UnityTest]
        public IEnumerator EveryOfferedLookHasAPlayableBodyAndLivePreview()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component appearance = GameObject.Find("Player").GetComponent("PlayerAppearance");
            Component menus = GameObject.Find("Loop HUD").GetComponent("GameMenus");
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            Transform stage = GameObject.Find("Character Preview Stage").transform;
            foreach (string look in new[] { "rogue", "rogue.hooded", "knight", "ranger",
                         "mage", "barbarian" })
            {
                Assert.That((bool)appearance.GetType().GetMethod("Apply")
                    .Invoke(appearance, new object[] { look }), Is.True, look);
                Assert.That((string)appearance.GetType().GetProperty("CurrentId")
                    .GetValue(appearance), Is.EqualTo(look));
                GameObject preview = (GameObject)appearance.GetType().GetMethod("CreatePreview")
                    .Invoke(appearance, new object[] { look, stage });
                Assert.That(preview, Is.Not.Null, look);
                Assert.That(preview.GetComponentsInChildren<Renderer>(true).Length,
                    Is.GreaterThan(0), look);
                Animator animator = preview.GetComponentInChildren<Animator>(true);
                Assert.That(animator.avatar, Is.Not.Null, look);
                Assert.That(animator.avatar.isValid, Is.True, look);
                UnityEngine.Object.Destroy(preview);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator PreviewStaysOnChosenLookWhilePointerMovesAndModelRotates()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject hud = GameObject.Find("Loop HUD");
            Component menus = hud.GetComponent("GameMenus");
            Component picker = hud.GetComponent("CharacterWorldMenu");
            menus.GetType().GetMethod("ShowTitle").Invoke(menus, null);
            Component stage = GameObject.Find("Main Menu Stage").GetComponent("MainMenuStage");
            hud.transform.Find("Desktop Menus/Title Screen/Title Composition/Play")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Transform selection = hud.transform.Find("Desktop Menus/Character Selection/Iron Selection Card");
            selection.Find("Characters/New Character").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            yield return null;
            Transform choices = selection.Find("Looks/Look List/Viewport/Content");
            choices.GetChild(5).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            choices.GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            string selected = (string)picker.GetType().GetField("_selectedLookId",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(picker);
            Assert.That(selected, Is.EqualTo("rogue"));
            var hover = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute<IPointerEnterHandler>(choices.GetChild(5).gameObject,
                hover, ExecuteEvents.pointerEnterHandler);
            Assert.That((string)picker.GetType().GetField("_selectedLookId",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(picker),
                Is.EqualTo("rogue"), "Passing over another row must not swap models.");
            GameObject preview = (GameObject)stage.GetType().GetProperty("CurrentModel")
                .GetValue(stage);
            Assert.That(preview.name, Does.Contain("Rogue"));
            selection.Find("Looks/Rotate").GetComponent<UnityEngine.UI.Button>()
                .onClick.Invoke();
            Assert.That((string)picker.GetType().GetField("_selectedLookId",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(picker),
                Is.EqualTo("rogue"));
            Assert.That(preview.transform.localEulerAngles.y, Is.EqualTo(225f).Within(1f));
        }

        static IEnumerator WaitForPair(Component session, string oldCharacter, string oldWorld)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (((string)session.GetType().GetProperty("ActiveCharacterId").GetValue(session) ==
                    oldCharacter ||
                    (string)session.GetType().GetProperty("ActiveWorldId").GetValue(session) ==
                    oldWorld) && Time.realtimeSinceStartup < deadline)
                yield return null;
        }
    }
}
