using System;
using TMPro;
using Topaz.LoopStudy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Topaz.Menus
{
    /// <summary>Character and World choices are drafts until Enter World succeeds.</summary>
    public sealed class CharacterWorldMenu : MonoBehaviour
    {
        enum Page { Characters, Looks, Worlds }

        [SerializeField] GameMenus menus;
        [SerializeField] WorldSession session;
        [SerializeField] MainMenuStage stage;
        [SerializeField] GameObject root;
        [SerializeField] GameObject charactersPage;
        [SerializeField] GameObject looksPage;
        [SerializeField] GameObject worldsPage;
        [SerializeField] RectTransform characterList;
        [SerializeField] RectTransform lookList;
        [SerializeField] RectTransform worldList;
        [SerializeField] ScrollRect characterScroll;
        [SerializeField] ScrollRect lookScroll;
        [SerializeField] ScrollRect worldScroll;
        [SerializeField] GameObject buttonPrefab;
        [SerializeField] Button newCharacterButton;
        [SerializeField] Button createCharacterButton;
        [SerializeField] Button rotateButton;
        [SerializeField] Button newWorldButton;
        [SerializeField] Button enterWorldButton;
        [SerializeField] Button charactersBackButton;
        [SerializeField] Button looksBackButton;
        [SerializeField] Button worldsBackButton;
        [SerializeField] TMP_Text worldChoiceLabel;
        [SerializeField] TMP_Text errorLabel;

        Page _page;
        string _selectedCharacterId;
        string _draftAppearanceId;
        string _selectedWorldId;
        string _selectedLookId = CharacterLooks.Rogue;
        bool _newWorld;
        bool _entering;
        GameObject _confirmation;
        TMP_Text _confirmationText;
        Button _confirmDeleteButton;
        Button _cancelDeleteButton;
        Button _returnFocus;
        string _pendingDeleteId;
        bool _deleteCharacter;
        void Awake()
        {
            if (menus == null || session == null || stage == null || root == null ||
                buttonPrefab == null || rotateButton == null)
            {
                Debug.LogError("Character and World menu references are incomplete.", this);
                enabled = false;
                return;
            }
            newCharacterButton.onClick.AddListener(OpenLooks);
            createCharacterButton.onClick.AddListener(CreateCharacter);
            rotateButton.onClick.AddListener(RotatePreview);
            newWorldButton.onClick.AddListener(ChooseNewWorld);
            newWorldButton.gameObject.AddComponent<MenuChoiceFocus>().SetAction(ChooseNewWorld);
            enterWorldButton.onClick.AddListener(EnterWorld);
            charactersBackButton.onClick.AddListener(Back);
            looksBackButton.onClick.AddListener(Back);
            worldsBackButton.onClick.AddListener(Back);
            BuildConfirmation();
            root.SetActive(false);
        }

        public void Open()
        {
            _selectedCharacterId = null;
            _draftAppearanceId = null;
            _selectedWorldId = null;
            _newWorld = false;
            _entering = false;
            CloseConfirmation();
            root.SetActive(true);
            ShowCharacters();
        }

        public void Close()
        {
            CloseConfirmation();
            root.SetActive(false);
        }

        public void Back()
        {
            if (_entering) return;
            if (_confirmation.activeSelf)
            {
                CloseConfirmation();
                return;
            }
            if (_page == Page.Characters) menus.ShowTitle();
            else if (_page == Page.Looks) ShowCharacters();
            else if (_draftAppearanceId != null) ShowLooks();
            else ShowCharacters();
        }

        void ShowCharacters()
        {
            Show(Page.Characters);
            stage.ShowLook(session.LastAppearanceId);
            Clear(characterList);
            var characters = session.Characters;
            if (characters != null)
            {
                for (int i = 0; i < characters.Count; i++)
                {
                    TopazCharacterData character = characters[i];
                    Button button = AddDeletableChoice(characterList, character.label,
                        () => ChooseCharacter(character.id),
                        delete => AskDelete(character.id, character.label, true, delete));
                    ScrollOnSelect(button, characterScroll, i, characters.Count);
                }
            }
            Focus(characterList.childCount > 0
                ? characterList.GetChild(0).GetChild(0).GetComponent<Button>() : newCharacterButton);
        }

        void ChooseCharacter(string id)
        {
            _selectedCharacterId = id;
            _draftAppearanceId = null;
            var characters = session.Characters;
            if (characters != null)
                foreach (TopazCharacterData character in characters)
                    if (character.id == id) stage.ShowLook(character.appearanceId);
            ShowWorlds();
        }

        void OpenLooks()
        {
            _selectedCharacterId = null;
            _draftAppearanceId = null;
            _selectedLookId = CharacterLooks.Rogue;
            stage.ResetRotation();
            ShowLooks();
        }

        void ShowLooks()
        {
            Show(Page.Looks);
            Clear(lookList);
            for (int i = 0; i < CharacterLooks.All.Length; i++)
            {
                string id = CharacterLooks.All[i];
                Button button = AddChoice(lookList, CharacterLooks.Label(id),
                    () => SelectLook(id));
                var focus = button.gameObject.AddComponent<MenuChoiceFocus>();
                focus.SetAction(() => SelectLook(id));
                ScrollOnSelect(button, lookScroll, i, CharacterLooks.All.Length);
            }
            SelectLook(_selectedLookId);
            int focusedLook = Array.IndexOf(CharacterLooks.All, _selectedLookId);
            Focus(lookList.GetChild(Mathf.Max(0, focusedLook)).GetComponent<Button>());
        }

        void SelectLook(string id)
        {
            if (_selectedLookId == id && stage.CurrentLookId == id) return;
            _selectedLookId = id;
            stage.ShowLook(id);
        }

        void RotatePreview() => stage.Rotate();

        void CreateCharacter()
        {
            _draftAppearanceId = _selectedLookId;
            ShowWorlds();
        }

        void ShowWorlds()
        {
            Show(Page.Worlds);
            _newWorld = false;
            _selectedWorldId = null;
            Clear(worldList);
            var worlds = session.Worlds;
            if (worlds != null)
            {
                for (int i = 0; i < worlds.Count; i++)
                {
                    TopazWorldData world = worlds[i];
                    int day = 1 + (int)Math.Floor((world.worldHours - WorldClock.StartingHour) /
                        WorldClock.HoursPerDay);
                    Button button = AddDeletableChoice(worldList,
                        world.label + "  ·  Day " + day, () => ChooseWorld(world.id),
                        delete => AskDelete(world.id, world.label, false, delete));
                    button.gameObject.AddComponent<MenuChoiceFocus>().SetAction(
                        () => ChooseWorld(world.id));
                    ScrollOnSelect(button, worldScroll, i, worlds.Count);
                }
            }
            ChooseNewWorld();
            Focus(worldList.childCount > 0
                ? worldList.GetChild(0).GetChild(0).GetComponent<Button>() : newWorldButton);
        }

        void ChooseWorld(string id)
        {
            _selectedWorldId = id;
            _newWorld = false;
            TopazWorldData world = session.Worlds == null ? null :
                System.Linq.Enumerable.FirstOrDefault(session.Worlds, value => value.id == id);
            worldChoiceLabel.text = world != null ? "Enter " + world.label : "Choose a World";
            enterWorldButton.interactable = world != null;
        }

        void ChooseNewWorld()
        {
            _selectedWorldId = null;
            _newWorld = true;
            worldChoiceLabel.text = "Start a fresh World";
            enterWorldButton.interactable = true;
        }

        void EnterWorld()
        {
            if (_entering || (_selectedCharacterId == null && _draftAppearanceId == null) ||
                (!_newWorld && _selectedWorldId == null)) return;
            _entering = true;
            enterWorldButton.interactable = false;
            errorLabel.text = "Entering World…";
            StartCoroutine(session.EnterPair(_selectedCharacterId, _selectedWorldId,
                _draftAppearanceId, _newWorld, success =>
                {
                    _entering = false;
                    if (success) menus.EnterFromSelection();
                    else
                    {
                        errorLabel.text = session.SaveProblem ?? "Could not enter this World.";
                        enterWorldButton.interactable = true;
                    }
                }));
        }

        void Show(Page page)
        {
            _page = page;
            charactersPage.SetActive(page == Page.Characters);
            looksPage.SetActive(page == Page.Looks);
            worldsPage.SetActive(page == Page.Worlds);
            if (errorLabel != null) errorLabel.text = session.SaveProblem ?? "";
        }

        Button AddDeletableChoice(Transform parent, string label,
            UnityEngine.Events.UnityAction choose, Action<Button> delete)
        {
            var row = new GameObject(label + " Row", typeof(RectTransform),
                typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 67f;
            var group = row.GetComponent<HorizontalLayoutGroup>();
            group.spacing = 10f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            Button choice = AddChoice(row.transform, label, choose);
            choice.GetComponent<LayoutElement>().flexibleWidth = 1f;
            Button deleteButton = AddChoice(row.transform, "Delete", null);
            deleteButton.GetComponent<LayoutElement>().preferredWidth = 135f;
            deleteButton.GetComponentInChildren<TMP_Text>().color = new Color(1f, .58f, .52f);
            deleteButton.onClick.AddListener(() => delete(deleteButton));
            return choice;
        }

        void BuildConfirmation()
        {
            _confirmation = new GameObject("Delete Confirmation", typeof(RectTransform),
                typeof(Image));
            _confirmation.transform.SetParent(root.transform, false);
            var overlay = _confirmation.GetComponent<RectTransform>();
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            _confirmation.GetComponent<Image>().color = new Color(0f, 0f, 0f, .82f);

            var panel = new GameObject("Confirmation Card", typeof(RectTransform),
                typeof(Image));
            panel.transform.SetParent(overlay, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(760f, 340f);
            panel.GetComponent<Image>().color = new Color32(41, 33, 29, 255);

            _confirmationText = Instantiate(worldChoiceLabel, panel.transform);
            _confirmationText.name = "Confirmation Message";
            _confirmationText.alignment = TextAlignmentOptions.Center;
            _confirmationText.fontSize = 30f;
            _confirmationText.textWrappingMode = TextWrappingModes.Normal;
            _confirmationText.rectTransform.anchorMin =
                _confirmationText.rectTransform.anchorMax = new Vector2(.5f, .5f);
            _confirmationText.rectTransform.pivot = new Vector2(.5f, .5f);
            _confirmationText.rectTransform.anchoredPosition = new Vector2(0f, 55f);
            _confirmationText.rectTransform.sizeDelta = new Vector2(690f, 180f);
            _confirmDeleteButton = ConfirmationButton(panel.transform, "Delete", -165f);
            _cancelDeleteButton = ConfirmationButton(panel.transform, "Cancel", 165f);
            _confirmDeleteButton.onClick.AddListener(ConfirmDelete);
            _cancelDeleteButton.onClick.AddListener(CloseConfirmation);
            _confirmation.SetActive(false);
        }

        Button ConfirmationButton(Transform parent, string label, float x)
        {
            Button button = AddChoice(parent, label, null);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, -105f);
            rect.sizeDelta = new Vector2(280f, 70f);
            return button;
        }

        void AskDelete(string id, string label, bool character, Button returnFocus)
        {
            if (_entering) return;
            _pendingDeleteId = id;
            _deleteCharacter = character;
            _returnFocus = returnFocus;
            _confirmationText.text = character
                ? $"Delete Character \"{label}\"?\nTheir progress and visits in every World will be lost."
                : $"Delete World \"{label}\"?\nIts progress and every Character's visits there will be lost.";
            charactersPage.SetActive(false);
            looksPage.SetActive(false);
            worldsPage.SetActive(false);
            _confirmation.SetActive(true);
            Focus(_cancelDeleteButton);
        }

        void ConfirmDelete()
        {
            if (string.IsNullOrEmpty(_pendingDeleteId)) return;
            bool character = _deleteCharacter;
            bool success = character ? session.DeleteCharacter(_pendingDeleteId) :
                session.DeleteWorld(_pendingDeleteId);
            CloseConfirmation();
            if (success)
            {
                if (character) ShowCharacters();
                else ShowWorlds();
            }
            else errorLabel.text = session.SaveProblem ?? "Could not delete this selection.";
        }

        void CloseConfirmation()
        {
            if (_confirmation == null || !_confirmation.activeSelf) return;
            _confirmation.SetActive(false);
            _pendingDeleteId = null;
            charactersPage.SetActive(_page == Page.Characters);
            looksPage.SetActive(_page == Page.Looks);
            worldsPage.SetActive(_page == Page.Worlds);
            Focus(_returnFocus);
            _returnFocus = null;
        }

        Button AddChoice(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject instance = Instantiate(buttonPrefab, parent);
            instance.name = label;
            instance.SetActive(true);
            var button = instance.GetComponent<Button>();
            var text = instance.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = label;
            var layout = instance.GetComponent<LayoutElement>();
            if (layout == null) layout = instance.AddComponent<LayoutElement>();
            layout.preferredHeight = 67f;
            if (action != null) button.onClick.AddListener(action);
            return button;
        }

        static void ScrollOnSelect(Button button, ScrollRect scroll, int index, int count)
        {
            if (scroll == null) return;
            var focus = button.GetComponent<MenuChoiceFocus>();
            if (focus == null) focus = button.gameObject.AddComponent<MenuChoiceFocus>();
            focus.AddAction(() => scroll.verticalNormalizedPosition = count <= 1
                ? 1f : 1f - (float)index / (count - 1));
        }

        static void Clear(Transform content)
        {
            while (content.childCount > 0)
            {
                Transform child = content.GetChild(0);
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        static void Focus(Button button) => EventSystem.current?.SetSelectedGameObject(
            button == null ? null : button.gameObject);
    }

    /// <summary>Keyboard and gamepad focus update the selected look.</summary>
    public sealed class MenuChoiceFocus : MonoBehaviour, ISelectHandler
    {
        Action _action;

        public void SetAction(Action action) => _action = action;
        public void AddAction(Action action) => _action += action;
        public void OnSelect(BaseEventData eventData) => _action?.Invoke();
    }
}
