using TMPro;
using Topaz.UI;
using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>Small, readable uGUI view over the first loop; it owns no game state.</summary>
    public sealed class LoopHud : MonoBehaviour
    {
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] TMP_Text crossbowReloadLabel;
        [SerializeField] TMP_Text craftDescription;
        [SerializeField] Sprite homeJournalBackground;
        [SerializeField] TMP_Text chestDescription;
        [SerializeField] GameObject craftPanel;
        [SerializeField] GameObject chestPanel;
        [SerializeField] GameObject inventoryPanel;
        [SerializeField] GameObject visualOptionsPanel;
        [SerializeField] TMP_Text[] backpackSlotLabels;
        [SerializeField] UnityEngine.UI.Button[] backpackSlotButtons;
        [SerializeField] UnityEngine.UI.Image[] backpackSlotIcons;
        [SerializeField] TMP_Text backpackCapacityLabel;
        [SerializeField] TMP_Text selectedItemName;
        [SerializeField] TMP_Text selectedItemDetail;
        [SerializeField] UnityEngine.UI.Image selectedItemIcon;
        [SerializeField] UnityEngine.UI.Button lanternButton;
        [SerializeField] TMP_Text lanternButtonLabel;
        [SerializeField] TMP_Text[] chestSlotLabels;
        [SerializeField] UnityEngine.UI.Button[] chestSlotButtons;
        [SerializeField] TMP_Text[] chestBackpackLabels;
        [SerializeField] UnityEngine.UI.Button[] chestBackpackButtons;
        [SerializeField] UnityEngine.UI.Button storeGearButton;
        [SerializeField] UnityEngine.UI.Button takeGearButton;
        [SerializeField] UnityEngine.UI.Button craftButton;
        [SerializeField] UnityEngine.UI.Button pathButton;
        [SerializeField] UnityEngine.UI.Button anvilButton;
        [SerializeField] UnityEngine.UI.Button editHomeButton;
        [SerializeField] UnityEngine.UI.Button depositButton;
        [SerializeField] UnityEngine.UI.Button withdrawButton;
        [SerializeField] UnityEngine.UI.Button craftCloseButton;
        [SerializeField] UnityEngine.UI.Button chestCloseButton;
        [SerializeField] UnityEngine.UI.Button inventoryCloseButton;
        [SerializeField] UnityEngine.UI.Button equipmentTabButton;
        [SerializeField] EquipmentJournalView equipmentView;
        [SerializeField] SkillsJournalView skillsView;
        [SerializeField] UnityEngine.UI.Button skillsTabButton;

        WorldSession _session;
        float _statusUntil;
        int _selectedBackpackSlot;
        int _selectedChestPackSlot = -1;
        int _selectedChestSlot = -1;
        bool _listenersBound;
        HomeJournalView _homeJournal;
        CampfireTravelView _travelView;
        StatusJournalView _statusJournal;
        UnityEngine.UI.Button _eatButton;
        GameObject _staminaMeter;
        UnityEngine.UI.Image _staminaFill;

        public bool TravelOpen => _travelView != null && _travelView.IsOpen;

        public bool MenuOpen => (craftPanel != null && craftPanel.activeSelf) ||
            (chestPanel != null && chestPanel.activeSelf) ||
            (inventoryPanel != null && inventoryPanel.activeSelf) ||
            (visualOptionsPanel != null && visualOptionsPanel.activeSelf) ||
            (equipmentView != null && (equipmentView.IsOpen || equipmentView.IsRackOpen)) ||
            (skillsView != null && skillsView.IsOpen) ||
            (_homeJournal != null && _homeJournal.IsOpen) ||
            (_statusJournal != null && _statusJournal.IsOpen) || TravelOpen;

        void Awake()
        {
            ClosePanels();
            if (statusLabel != null) statusLabel.text = "";
        }

        public void Bind(WorldSession session)
        {
            _session = session;
            if (!_listenersBound)
            {
                craftButton.onClick.AddListener(() => _session.TryCraftChest());
                if (pathButton != null) pathButton.onClick.AddListener(() =>
                    _session.BeginHomeBuild(HomeBuilds.PathId));
                if (anvilButton != null) anvilButton.onClick.AddListener(() =>
                    _session.BeginHomeBuild(HomeBuilds.AnvilId));
                if (editHomeButton != null) editHomeButton.onClick.AddListener(() =>
                    _session.BeginHomeEdit());
                depositButton.onClick.AddListener(() => _session.DepositAllItems());
                withdrawButton.onClick.AddListener(() => _session.WithdrawAllItems());
                craftCloseButton.onClick.AddListener(ClosePanels);
                chestCloseButton.onClick.AddListener(ClosePanels);
                inventoryCloseButton.onClick.AddListener(ClosePanels);
                if (storeGearButton != null) storeGearButton.onClick.AddListener(() =>
                {
                    if (!_session.TryTransferGear(true, _selectedChestPackSlot))
                        ShowStatus("Cannot store that gear here.");
                    Refresh();
                });
                if (takeGearButton != null) takeGearButton.onClick.AddListener(() =>
                {
                    if (!_session.TryTransferGear(false, _selectedChestSlot))
                        ShowStatus("Make space in the backpack first.");
                    Refresh();
                });
                if (chestBackpackButtons != null)
                    for (int i = 0; i < chestBackpackButtons.Length; i++)
                    {
                        int index = i;
                        chestBackpackButtons[i].onClick.AddListener(() =>
                        { _selectedChestPackSlot = index; Refresh(); });
                    }
                if (chestSlotButtons != null)
                    for (int i = 0; i < chestSlotButtons.Length; i++)
                    {
                        int index = i;
                        chestSlotButtons[i].onClick.AddListener(() =>
                        { _selectedChestSlot = index; Refresh(); });
                    }
                if (equipmentTabButton != null && equipmentView != null)
                    equipmentTabButton.onClick.AddListener(equipmentView.Show);
                if (skillsTabButton != null && skillsView != null)
                    skillsTabButton.onClick.AddListener(skillsView.Show);
                if (lanternButton != null)
                    lanternButton.onClick.AddListener(ToggleLantern);
                if (backpackSlotButtons != null)
                {
                    for (int i = 0; i < backpackSlotButtons.Length; i++)
                    {
                        int index = i;
                        if (backpackSlotButtons[i] != null)
                            backpackSlotButtons[i].onClick.AddListener(() => SelectBackpackSlot(index));
                    }
                }
                _listenersBound = true;
            }
            equipmentView?.Bind(session, this);
            skillsView?.Bind(session, this);
            if (_homeJournal == null)
            {
                _homeJournal = gameObject.GetComponent<HomeJournalView>();
                if (_homeJournal == null) _homeJournal = gameObject.AddComponent<HomeJournalView>();
            }
            _homeJournal.Bind(session, this, skillsTabButton,
                craftDescription != null ? craftDescription.font : null,
                homeJournalBackground);
            if (_travelView == null)
            {
                _travelView = GetComponent<CampfireTravelView>();
                if (_travelView == null) _travelView = gameObject.AddComponent<CampfireTravelView>();
            }
            _travelView.Bind(session, statusLabel != null ? statusLabel.font : null,
                homeJournalBackground);
            if (_statusJournal == null)
                _statusJournal = GetComponent<StatusJournalView>() ??
                    gameObject.AddComponent<StatusJournalView>();
            _statusJournal.Bind(session, this, skillsTabButton,
                statusLabel != null ? statusLabel.font : null, homeJournalBackground);
            CreateEatButton();
            CreateStaminaMeter();
            Refresh();
        }

        public void Refresh()
        {
            if (_session == null) return;
            Set(craftDescription, $"Storage chest  •  {_session.ChestCost} Wood\n" +
                (_session.ChestPlaced ? "Already placed" :
                    _session.PendingChest ? "Ready to place" : $"You have {_session.WoodCount} Wood"));
            Set(chestDescription, $"Backpack: {_session.WoodCount} Wood, " +
                $"{_session.StoneCount - _session.ChestStone} Stone, " +
                $"{_session.IronCount - _session.ChestIron} Iron\n" +
                $"Chest: {_session.ChestWood} Wood, {_session.ChestStone} Stone, " +
                $"{_session.ChestIron} Iron");
            UpdateSlots(backpackSlotLabels, _session.BackpackSlots);
            RefreshBackpackIcons();
            if (backpackCapacityLabel != null && _session.BackpackSlots != null)
            {
                int used = 0;
                foreach (ItemStackRecord slot in _session.BackpackSlots)
                    if (slot != null && slot.count > 0) used++;
                Set(backpackCapacityLabel, $"{used} / {_session.BackpackSlots.Count} slots");
            }
            RefreshSelectedItem();
            equipmentView?.Refresh();
            skillsView?.Refresh();
            _homeJournal?.Refresh();
            Set(lanternButtonLabel, _session.LanternOn ? "Lantern  •  On" : "Lantern  •  Off");
            UpdateSlots(chestSlotLabels, _session.ChestSlots);
            UpdateSlots(chestBackpackLabels, _session.BackpackSlots);
            craftButton.interactable = _session.CanCraftChest;
            if (pathButton != null) pathButton.interactable = _session.StoneCount >= 1;
            if (anvilButton != null) anvilButton.interactable =
                _session.StoneCount >= 6 && _session.IronCount >= 2;
            if (editHomeButton != null) editHomeButton.interactable = true;
            TMP_Text craftAction = craftButton.GetComponentInChildren<TMP_Text>();
            if (craftAction != null)
                Set(craftAction, _session.ChestPlaced ? "Already built" : "Craft chest");
            depositButton.interactable = _session.BackpackHasMaterials && _session.ChestPlaced;
            withdrawButton.interactable = _session.ChestHasMaterials;
            if (storeGearButton != null)
                storeGearButton.interactable = IsSelectedGear(_session.BackpackSlots, _selectedChestPackSlot);
            if (takeGearButton != null)
                takeGearButton.interactable = IsSelectedGear(_session.ChestSlots, _selectedChestSlot);

            if (_session.SaveProblem != null)
            {
                statusLabel.text = _session.SaveProblem;
                return;
            }
            if (Time.time >= _statusUntil) statusLabel.text = "";
        }

        public void Tick()
        {
            if (_session == null) return;
            if (_session.SaveProblem == null && Time.time >= _statusUntil) Set(statusLabel, "");
            if (crossbowReloadLabel != null)
                Set(crossbowReloadLabel, _session.IsCrossbowEquipped &&
                    _session.CrossbowReloadProgress < 1f
                    ? $"Crossbow reload  {Mathf.RoundToInt(_session.CrossbowReloadProgress * 100f)}%"
                    : "");
            if (_staminaMeter != null)
            {
                _staminaMeter.SetActive(_session.StaminaCueVisible && !MenuOpen);
                if (_staminaFill != null)
                    _staminaFill.fillAmount = Mathf.Clamp01(
                        _session.Stamina / _session.MaximumStamina);
            }
        }

        void CreateEatButton()
        {
            if (_eatButton != null || selectedItemDetail == null) return;
            var source = selectedItemDetail.rectTransform;
            var obj = new GameObject("Eat Selected Food", typeof(RectTransform),
                typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            obj.transform.SetParent(source.parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.anchoredPosition = source.anchoredPosition + new Vector2(0f, -85f);
            rect.sizeDelta = new Vector2(190f, 60f);
            obj.GetComponent<UnityEngine.UI.Image>().color =
                new Color32(83, 53, 35, 245);
            _eatButton = obj.GetComponent<UnityEngine.UI.Button>();
            var label = new GameObject("Eat Label", typeof(RectTransform),
                typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(obj.transform, false);
            var textRect = (RectTransform)label.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            label.text = "Eat";
            label.font = selectedItemDetail.font;
            label.fontSize = 28f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var outline = obj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color32(239, 199, 132, 255);
            outline.enabled = false;
            obj.AddComponent<TopazFocusIndicator>();
            _eatButton.onClick.AddListener(() =>
            {
                if (_session.TryEat(_selectedBackpackSlot)) Refresh();
            });
            _eatButton.gameObject.SetActive(false);
        }

        void CreateStaminaMeter()
        {
            if (_staminaMeter != null) return;
            _staminaMeter = new GameObject("Contextual Stamina", typeof(RectTransform),
                typeof(UnityEngine.UI.Image));
            _staminaMeter.transform.SetParent(transform, false);
            var rect = (RectTransform)_staminaMeter.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 92f);
            rect.sizeDelta = new Vector2(200f, 15f);
            _staminaMeter.GetComponent<UnityEngine.UI.Image>().color =
                new Color32(23, 19, 18, 220);
            var fill = new GameObject("Fill", typeof(RectTransform),
                typeof(UnityEngine.UI.Image));
            fill.transform.SetParent(_staminaMeter.transform, false);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            _staminaFill = fill.GetComponent<UnityEngine.UI.Image>();
            _staminaFill.color = new Color32(239, 199, 132, 255);
            _staminaFill.type = UnityEngine.UI.Image.Type.Filled;
            _staminaFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            _staminaMeter.SetActive(false);
        }

        static void Set(TMP_Text label, string value)
        {
            if (label != null && label.text != value) label.text = value;
        }

        void UpdateSlots(TMP_Text[] labels, System.Collections.Generic.IReadOnlyList<ItemStackRecord> slots)
        {
            if (labels == null) return;
            for (int i = 0; i < labels.Length; i++)
            {
                ItemStackRecord slot = slots != null && i < slots.Count ? slots[i] : null;
                Set(labels[i], slot == null || slot.count == 0 ? "—" :
                    $"{_session.ItemName(slot.itemId)}\n×{slot.count}");
            }
        }

        void RefreshBackpackIcons()
        {
            if (backpackSlotIcons == null || backpackSlotLabels == null ||
                _session?.BackpackSlots == null) return;
            var slots = _session.BackpackSlots;
            for (int i = 0; i < backpackSlotIcons.Length && i < backpackSlotLabels.Length; i++)
            {
                UnityEngine.UI.Image image = backpackSlotIcons[i];
                if (image == null) continue;
                ItemStackRecord slot = i < slots.Count ? slots[i] : null;
                Sprite icon = slot != null && slot.count > 0
                    ? _session.ItemJournalIcon(slot.itemId) : null;
                image.sprite = icon;
                image.enabled = icon != null;
                if (icon != null)
                {
                    Set(backpackSlotLabels[i], "×" + slot.count);
                    backpackSlotLabels[i].alignment = TextAlignmentOptions.BottomRight;
                }
                else backpackSlotLabels[i].alignment = TextAlignmentOptions.Center;
            }
        }

        void SelectBackpackSlot(int index)
        {
            _selectedBackpackSlot = index;
            RefreshSelectedItem();
        }

        bool IsSelectedGear(System.Collections.Generic.IReadOnlyList<ItemStackRecord> slots, int index)
        {
            if (slots == null || index < 0 || index >= slots.Count || slots[index].count != 1)
                return false;
            ItemDefinition item = _session.Item(slots[index].itemId);
            return item != null && item.EquipmentSlot != EquipmentSlot.None;
        }

        void ToggleLantern()
        {
            _selectedBackpackSlot = -1;
            _session?.ToggleLantern();
            Refresh();
        }

        void RefreshSelectedItem()
        {
            if (selectedItemName == null || selectedItemDetail == null ||
                _session?.BackpackSlots == null) return;
            var slots = _session.BackpackSlots;
            if (_eatButton != null)
                _eatButton.gameObject.SetActive(_selectedBackpackSlot >= 0 &&
                    _selectedBackpackSlot < slots.Count &&
                    (slots[_selectedBackpackSlot].itemId == SurvivalRules.BerriesId ||
                     slots[_selectedBackpackSlot].itemId == SurvivalRules.StewId));
            if (_eatButton != null && _eatButton.gameObject.activeSelf)
                _eatButton.interactable = _session.CanEat(
                    slots[_selectedBackpackSlot].itemId);
            if (_selectedBackpackSlot == -1)
            {
                Set(selectedItemName, "Lantern");
                Set(selectedItemDetail, _session.LanternOn
                    ? "Lit  •  Select the lantern to turn it off"
                    : "Unlit  •  Select the lantern to turn it on");
                if (selectedItemIcon != null) selectedItemIcon.enabled = false;
                return;
            }
            ItemStackRecord slot = _selectedBackpackSlot >= 0 && _selectedBackpackSlot < slots.Count
                ? slots[_selectedBackpackSlot] : null;
            if (slot == null || slot.count == 0)
            {
                Set(selectedItemName, "Empty slot");
                Set(selectedItemDetail, "");
                if (selectedItemIcon != null) selectedItemIcon.enabled = false;
                return;
            }
            Set(selectedItemName, _session.ItemName(slot.itemId));
            Set(selectedItemDetail, $"{slot.count} in this stack  •  {_session.ItemStackLimit(slot.itemId)} max");
            if (selectedItemIcon != null)
            {
                selectedItemIcon.sprite = _session.ItemJournalIcon(slot.itemId);
                selectedItemIcon.enabled = selectedItemIcon.sprite != null;
            }
        }

        public void ShowStatus(string message)
        {
            statusLabel.text = message;
            _statusUntil = Time.time + 3.5f;
        }

        public void ShowCraftPanel()
        {
            chestPanel.SetActive(false);
            inventoryPanel.SetActive(false);
            craftPanel.SetActive(true);
            Refresh();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                (craftButton.interactable ? craftButton : craftCloseButton).gameObject);
        }

        public void ShowChestPanel()
        {
            craftPanel.SetActive(false);
            inventoryPanel.SetActive(false);
            chestPanel.SetActive(true);
            _selectedChestPackSlot = -1;
            _selectedChestSlot = -1;
            Refresh();
            UnityEngine.UI.Button first = depositButton.interactable ? depositButton :
                withdrawButton.interactable ? withdrawButton : chestCloseButton;
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(first.gameObject);
        }

        public void ToggleInventoryPanel()
        {
            bool open = !inventoryPanel.activeSelf;
            ClosePanels();
            inventoryPanel.SetActive(open);
            if (open)
            {
                Refresh();
                _selectedBackpackSlot = -1;
                if (_session?.BackpackSlots != null)
                    for (int i = 0; i < _session.BackpackSlots.Count; i++)
                        if (_session.BackpackSlots[i] != null &&
                            _session.BackpackSlots[i].count > 0)
                        {
                            _selectedBackpackSlot = i;
                            break;
                        }
                RefreshSelectedItem();
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                    _selectedBackpackSlot == -1 && lanternButton != null
                        ? lanternButton.gameObject
                        : backpackSlotButtons != null && _selectedBackpackSlot >= 0 &&
                    _selectedBackpackSlot < backpackSlotButtons.Length &&
                    backpackSlotButtons[_selectedBackpackSlot] != null
                        ? backpackSlotButtons[_selectedBackpackSlot].gameObject
                        : inventoryCloseButton.gameObject);
            }
        }

        public void ClosePanels()
        {
            if (craftPanel != null) craftPanel.SetActive(false);
            if (chestPanel != null) chestPanel.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            if (visualOptionsPanel != null) visualOptionsPanel.SetActive(false);
            equipmentView?.Hide();
            skillsView?.Hide();
            _homeJournal?.Hide();
            _statusJournal?.Hide();
            _travelView?.Hide();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void ShowGearRackPanel() => equipmentView?.ShowRack();
        public void ShowEquipmentPanel() => equipmentView?.Show();
        public void ShowSkillsPanel() => skillsView?.Show();
        public void ShowSmithingPanel() => _homeJournal?.ShowSmithing();
        public void ShowCampfirePanel() => _homeJournal?.ShowCampfire();
        public void ShowTravelPanel()
        {
            ClosePanels();
            _travelView?.Show();
        }
    }
}
