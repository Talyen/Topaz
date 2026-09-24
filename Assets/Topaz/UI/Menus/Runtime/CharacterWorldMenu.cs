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
            root.SetActive(false);
        }

        public void Open()
        {
            _selectedCharacterId = null;
            _draftAppearanceId = null;
            _selectedWorldId = null;
            _newWorld = false;
            _entering = false;
            root.SetActive(true);
            ShowCharacters();
        }

        public void Close()
        {
            root.SetActive(false);
        }

        public void Back()
        {
            if (_entering) return;
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
                    Button button = AddChoice(characterList, character.label,
                        () => ChooseCharacter(character.id));
                    ScrollOnSelect(button, characterScroll, i, characters.Count);
                }
            }
            Focus(characterList.childCount > 0
                ? characterList.GetChild(0).GetComponent<Button>() : newCharacterButton);
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
                    Button button = AddChoice(worldList, world.label + "  ·  Day " + day,
                        () => ChooseWorld(world.id));
                    button.gameObject.AddComponent<MenuChoiceFocus>().SetAction(
                        () => ChooseWorld(world.id));
                    ScrollOnSelect(button, worldScroll, i, worlds.Count);
                }
            }
            ChooseNewWorld();
            Focus(worldList.childCount > 0
                ? worldList.GetChild(0).GetComponent<Button>() : newWorldButton);
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
            button.onClick.AddListener(action);
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
