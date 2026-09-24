using TMPro;
using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>Small, readable uGUI view over the first loop; it owns no game state.</summary>
    public sealed class LoopHud : MonoBehaviour
    {
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] TMP_Text craftDescription;
        [SerializeField] TMP_Text chestDescription;
        [SerializeField] GameObject craftPanel;
        [SerializeField] GameObject chestPanel;
        [SerializeField] GameObject inventoryPanel;
        [SerializeField] GameObject visualOptionsPanel;
        [SerializeField] TMP_Text[] backpackSlotLabels;
        [SerializeField] UnityEngine.UI.Button[] backpackSlotButtons;
        [SerializeField] TMP_Text backpackCapacityLabel;
        [SerializeField] TMP_Text selectedItemName;
        [SerializeField] TMP_Text selectedItemDetail;
        [SerializeField] UnityEngine.UI.Image selectedItemIcon;
        [SerializeField] TMP_Text[] chestSlotLabels;
        [SerializeField] UnityEngine.UI.Button craftButton;
        [SerializeField] UnityEngine.UI.Button depositButton;
        [SerializeField] UnityEngine.UI.Button withdrawButton;
        [SerializeField] UnityEngine.UI.Button craftCloseButton;
        [SerializeField] UnityEngine.UI.Button chestCloseButton;
        [SerializeField] UnityEngine.UI.Button inventoryCloseButton;

        WorldSession _session;
        float _statusUntil;
        int _selectedBackpackSlot;
        bool _listenersBound;

        public bool MenuOpen => (craftPanel != null && craftPanel.activeSelf) ||
            (chestPanel != null && chestPanel.activeSelf) ||
            (inventoryPanel != null && inventoryPanel.activeSelf) ||
            (visualOptionsPanel != null && visualOptionsPanel.activeSelf);

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
                depositButton.onClick.AddListener(() => _session.DepositAllItems());
                withdrawButton.onClick.AddListener(() => _session.WithdrawAllItems());
                craftCloseButton.onClick.AddListener(ClosePanels);
                chestCloseButton.onClick.AddListener(ClosePanels);
                inventoryCloseButton.onClick.AddListener(ClosePanels);
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
            Refresh();
        }

        public void Refresh()
        {
            if (_session == null) return;
            Set(craftDescription, $"Storage chest  •  {_session.ChestCost} Wood\n" +
                (_session.ChestPlaced ? "Already placed" :
                    _session.PendingChest ? "Ready to place" : $"You have {_session.WoodCount} Wood"));
            Set(chestDescription, $"Backpack: {_session.WoodCount} Wood  •  Chest: {_session.ChestWood} Wood");
            UpdateSlots(backpackSlotLabels, _session.BackpackSlots);
            if (backpackCapacityLabel != null && _session.BackpackSlots != null)
            {
                int used = 0;
                foreach (ItemStackRecord slot in _session.BackpackSlots)
                    if (slot != null && slot.count > 0) used++;
                Set(backpackCapacityLabel, $"{used} / {_session.BackpackSlots.Count} slots");
            }
            RefreshSelectedItem();
            UpdateSlots(chestSlotLabels, _session.ChestSlots);
            craftButton.interactable = _session.CanCraftChest;
            TMP_Text craftAction = craftButton.GetComponentInChildren<TMP_Text>();
            if (craftAction != null)
                Set(craftAction, _session.ChestPlaced ? "Already built" : "Craft chest");
            depositButton.interactable = _session.BackpackHasItems && _session.ChestPlaced;
            withdrawButton.interactable = _session.ChestHasItems;

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

        void SelectBackpackSlot(int index)
        {
            _selectedBackpackSlot = index;
            RefreshSelectedItem();
        }

        void RefreshSelectedItem()
        {
            if (selectedItemName == null || selectedItemDetail == null ||
                _session?.BackpackSlots == null) return;
            var slots = _session.BackpackSlots;
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
                _selectedBackpackSlot = 0;
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
                    backpackSlotButtons != null && _selectedBackpackSlot < backpackSlotButtons.Length &&
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
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}
