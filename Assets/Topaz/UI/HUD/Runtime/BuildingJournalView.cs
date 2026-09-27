using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Topaz.Gameplay
{
    /// <summary>Home page of the existing uGUI Journal; gameplay rules remain in WorldSession.</summary>
    public sealed class BuildingJournalView : MonoBehaviour
    {
        static readonly Color Ink = new Color(.09f, .075f, .07f, .97f);
        static readonly Color Paper = new Color(.94f, .87f, .74f);
        static readonly Color Brass = new Color(.94f, .78f, .52f);
        readonly List<(BuildCatalog.Entry entry, Button button, TMP_Text text)> _rows =
            new List<(BuildCatalog.Entry, Button, TMP_Text)>();
        readonly List<(HomeForgeCatalog.Recipe recipe, Button button)> _forgeRows =
            new List<(HomeForgeCatalog.Recipe, Button)>();
        WorldSession _session;
        LoopHud _hud;
        TMP_FontAsset _font;
        GameObject _page;
        GameObject _forgePanel;
        Button _tab;
        TMP_Text _heading;
        TMP_Text _summary;
        TMP_Text _forgeHeading;
        TMP_Text _forgeHint;
        Button _moveButton;
        Button _removeButton;
        Button _backButton;
        Button _forgeBack;
        RectTransform _pageRect;
        CanvasGroup _pageGroup;
        Vector2 _layoutSize;
        public bool IsOpen => _page != null && _page.activeSelf;

        public void Bind(WorldSession session, LoopHud hud, Button existingTab,
            TMP_FontAsset font, Sprite background)
        {
            _session = session;
            _hud = hud;
            _font = font;
            if (_page == null) Create(existingTab, background);
            Refresh();
        }

        void Create(Button existingTab, Sprite background)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            _page = new GameObject("Home Journal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _pageGroup=_page.GetComponent<CanvasGroup>();
            _page.transform.SetParent(canvas.transform, false);
            _pageRect = (RectTransform)_page.transform;
            _pageRect.anchorMin = Vector2.zero;
            _pageRect.anchorMax = Vector2.one;
            _pageRect.offsetMin = Vector2.zero;
            _pageRect.offsetMax = Vector2.zero;
            SetBackdrop(_page.GetComponent<Image>(), background);

            _heading = Label(_page.transform, "BUILD", 52, new Vector2(0f, 440f),
                new Vector2(950f, 70f), Ink);
            if(_hud.JournalHeadingFont!=null)_heading.font=_hud.JournalHeadingFont;
            _summary = Label(_page.transform, "", 29, new Vector2(0f, 362f),
                new Vector2(950f, 60f), Ink);
            for (int i = 0; i < BuildCatalog.Entries.Length; i++)
            {
                BuildCatalog.Entry entry = BuildCatalog.Entries[i];
                int column = i / 6;
                int row = i % 6;
                Vector2 position = new Vector2(column == 0 ? -260f : 260f,
                    270f - row * 112f);
                Button button = ButtonAt(_page.transform, position,
                    new Vector2(490f, 96f), out TMP_Text text);
                button.onClick.AddListener(() =>
                {
                    if (_session.BeginHomeBuild(entry.Id)) Hide();
                });
                _rows.Add((entry, button, text));
            }

            _moveButton = ButtonAt(_page.transform, new Vector2(-120f, -344f),
                new Vector2(240f, 64f), out TMP_Text moveText);
            moveText.text = "Move a build";
            _moveButton.onClick.AddListener(() => { if (_session.BeginHomeMove()) Hide(); });
            _removeButton = ButtonAt(_page.transform, new Vector2(120f, -344f),
                new Vector2(240f, 64f), out TMP_Text removeText);
            removeText.text = "Remove a build";
            _removeButton.onClick.AddListener(() => { if (_session.BeginHomeEdit()) Hide(); });
            _backButton = ButtonAt(_page.transform, new Vector2(360f, -344f),
                new Vector2(240f, 64f), out TMP_Text backText);
            backText.text = "Back to Journal";
            _backButton.onClick.AddListener(() => { Hide(); _hud.ToggleInventoryPanel(); });

            _forgePanel = new GameObject("Anvil Recipes", typeof(RectTransform), typeof(Image));
            _forgePanel.transform.SetParent(_page.transform, false);
            RectTransform forgeRect = (RectTransform)_forgePanel.transform;
            forgeRect.anchorMin = Vector2.zero;
            forgeRect.anchorMax = Vector2.one;
            forgeRect.offsetMin = Vector2.zero;
            forgeRect.offsetMax = Vector2.zero;
            SetBackdrop(_forgePanel.GetComponent<Image>(), background);
            _forgeHeading = Label(_forgePanel.transform, "BLACKSMITH'S ANVIL", 48,
                new Vector2(0f, 340f), new Vector2(950f, 70f), Ink);
            if(_hud.JournalHeadingFont!=null)_forgeHeading.font=_hud.JournalHeadingFont;
            _forgeHeading.enableAutoSizing=true;_forgeHeading.fontSizeMin=30;_forgeHeading.fontSizeMax=48;
            _forgeHint=Label(_forgePanel.transform,"FORGE & EQUIP\n\nCraft using your backpack and\nchests within 30 m.\n\nEquip new gear from the\nEquipment page.",28,Vector2.zero,new Vector2(500,350),Ink);
            for (int i = 0; i < HomeForgeCatalog.Recipes.Length; i++)
            {
                HomeForgeCatalog.Recipe recipe = HomeForgeCatalog.Recipes[i];
                Button forge = ButtonAt(_forgePanel.transform, new Vector2(0f, 190f - i * 125f),
                    new Vector2(760f, 95f), out TMP_Text forgeText);
                forgeText.text = recipe.Label + "\n" +
                    CostText(recipe.Wood, recipe.Stone, recipe.Iron);
                forge.onClick.AddListener(() => { _session.TryForge(recipe); Refresh(); });
                _forgeRows.Add((recipe, forge));
            }
            _forgeBack = ButtonAt(_forgePanel.transform, new Vector2(0f, -265f),
                new Vector2(350f, 68f), out TMP_Text forgeBackText);
            forgeBackText.text = "Close";
            _forgeBack.onClick.AddListener(Hide);
            _forgePanel.SetActive(false);

            _page.SetActive(false);

            if (existingTab == null) return;
            _tab = Instantiate(existingTab, existingTab.transform.parent);
            _tab.name = "Build Tab";
            RectTransform tabRect = _tab.GetComponent<RectTransform>();
            tabRect.anchoredPosition += new Vector2(152f, 0f);
            TMP_Text tabText = _tab.GetComponentInChildren<TMP_Text>();
            if (tabText != null) tabText.text = "Build";
            _tab.onClick.RemoveAllListeners();
            _tab.onClick.AddListener(Show);
        }

        static void SetBackdrop(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.preserveAspect = false;
            image.color = sprite != null ? Color.white : Ink;
            image.raycastTarget = true;
        }

        void Update()
        {
            if(IsOpen && _pageGroup!=null)_pageGroup.alpha=Mathf.MoveTowards(_pageGroup.alpha,1,Time.unscaledDeltaTime/.12f);
            if (IsOpen && _pageRect != null && _pageRect.rect.size != _layoutSize)
                Layout();
        }

        void Layout()
        {
            if (_pageRect == null) return;
            _layoutSize = _pageRect.rect.size;
            float width = Mathf.Max(800f, _layoutSize.x);
            float height = Mathf.Max(600f, _layoutSize.y);
            Place(_heading.rectTransform, -width*.19f, height * .5f - 100f,
                width*.33f, 70f);
            Place(_summary.rectTransform, width*.19f, height * .5f - 110f,
                width*.33f, 70f);
            float top = Mathf.Min(300f, height * .5f - 180f);
            float bottom = Mathf.Max(-240f, -height * .5f + 190f);
            float step = (top - bottom) / 5f;
            float buttonWidth = Mathf.Min(490f, width * .31f);
            float buttonHeight = Mathf.Min(96f, step - 10f);
            for (int i = 0; i < _rows.Count; i++)
            {
                int column = i / 6;
                int row = i % 6;
                Place(_rows[i].button.GetComponent<RectTransform>(),
                    (column == 0 ? -1f : 1f) * width * .19f,
                    top - row * step, buttonWidth, buttonHeight);
                _rows[i].text.rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            }
            float footerY = -height * .34f;
            float footerWidth = Mathf.Min(250f, width * .15f);
            Place(_moveButton.GetComponent<RectTransform>(), -width * .26f,
                footerY, footerWidth, 64f);
            Place(_removeButton.GetComponent<RectTransform>(), -width * .10f,
                footerY, footerWidth, 64f);
            Place(_backButton.GetComponent<RectTransform>(), width * .19f,
                footerY, footerWidth, 64f);

            Place(_forgeHeading.rectTransform, -width*.19f, height * .5f - 100f,
                width*.33f, 70f);
            Place(_forgeHint.rectTransform,width*.19f,10,width*.31f,Mathf.Min(350,height*.5f));
            float forgeWidth = Mathf.Min(600f, width * .31f);
            for (int i = 0; i < _forgeRows.Count; i++)
                Place(_forgeRows[i].button.GetComponent<RectTransform>(), -width*.19f,
                    height * .5f - 190f - i * Mathf.Min(125f, height * .15f),
                    forgeWidth, 86f);
            Place(_forgeBack.GetComponent<RectTransform>(), width*.19f, footerY,
                Mathf.Min(350f, width * .35f), 68f);


        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            TMP_Text label = rect.GetComponentInChildren<TMP_Text>();
            if (label != null && label.rectTransform != rect)
                label.rectTransform.sizeDelta = rect.sizeDelta;
        }

        public void Show()
        {
            if (_session == null || _page == null) return;
            _hud.ClosePanels();
            _page.SetActive(true);
            _pageGroup.alpha=0;
            _forgePanel.SetActive(false);
            Canvas.ForceUpdateCanvases();
            Layout();
            Refresh();
            Button first=_rows.Find(row=>row.button.interactable).button;
            EventSystem.current?.SetSelectedGameObject(first!=null?first.gameObject:_backButton.gameObject);
        }

        public void ShowSmithing()
        {
            Show();
            if (!IsOpen) return;
            _forgePanel.SetActive(true);
            Refresh();
            if (_forgeRows.Count > 0)
                EventSystem.current?.SetSelectedGameObject(_forgeRows[0].button.gameObject);
        }

        public void Hide()
        {
            if (_page != null) _page.SetActive(false);
        }

        public void Refresh()
        {
            if (_page == null || _session == null) return;
            if (_tab != null) _tab.gameObject.SetActive(_session.HasActivePair);
            if (_summary != null)
                _summary.text = $"{_session.HomeWoodCount} Wood   {_session.StoneCount} Stone   " +
                    $"{_session.IronCount} Iron\n<size=70%>Backpack + nearby chests · 30 m</size>";
            foreach (var row in _rows)
            {
                BuildCatalog.Entry entry = row.entry;
                row.text.text = "<b>" + entry.Label + "</b>\n<size=80%>" +
                    CostText(entry.Wood, entry.Stone, entry.Iron) + "</size>";
                bool available = _session.HomeWoodCount >= entry.Wood &&
                    _session.StoneCount >= entry.Stone &&
                    _session.IronCount >= entry.Iron;
                row.button.interactable = available;
                row.text.color = available ? Topaz.UI.JournalInk.Text : Topaz.UI.JournalInk.Muted;
                if(!available)row.text.text=row.text.text.Replace("</size>"," — need materials</size>");
            }
            foreach (var row in _forgeRows)
                row.button.interactable = _session.CanForge(row.recipe);

        }

        TMP_Text Label(Transform parent, string value, int size, Vector2 position,
            Vector2 dimensions, Color color)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = dimensions;
            rect.anchoredPosition = position;
            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = _font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        Button ButtonAt(Transform parent, Vector2 position, Vector2 dimensions,
            out TMP_Text label)
        {
            var obj = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = dimensions;
            rect.anchoredPosition = position;
            UnityEngine.UI.Image image = obj.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(.12f, .20f, .15f, 1f);
            Button button = obj.GetComponent<Button>();
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = Brass;
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            outline.enabled = false;
            obj.AddComponent<HomeFocusFrame>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.15f);
            colors.pressedColor = new Color(.8f,.9f,.8f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(.75f, .75f, .75f, .85f);
            button.colors = colors;
            label = Label(obj.transform, "", 27, Vector2.zero, dimensions, Topaz.UI.JournalInk.Text);
            label.enableAutoSizing=true;label.fontSizeMin=22.5f;label.fontSizeMax=27;
            Topaz.UI.JournalInk.Apply(button);
            return button;
        }

        static string CostText(int wood, int stone, int iron)
        {
            var parts = new List<string>(3);
            if (wood > 0) parts.Add(wood + " Wood");
            if (stone > 0) parts.Add(stone + " Stone");
            if (iron > 0) parts.Add(iron + " Iron");
            return string.Join("  •  ", parts);
        }
    }
}
