using TMPro;
using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>Small, readable uGUI view over the first loop; it owns no game state.</summary>
    public sealed class LoopHud : MonoBehaviour
    {
        [SerializeField] TMP_Text dayLabel;
        [SerializeField] TMP_Text woodLabel;
        [SerializeField] TMP_Text loggingLabel;
        [SerializeField] TMP_Text toolLabel;
        [SerializeField] TMP_Text contextLabel;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] TMP_Text craftDescription;
        [SerializeField] TMP_Text chestDescription;
        [SerializeField] GameObject craftPanel;
        [SerializeField] GameObject chestPanel;
        [SerializeField] GameObject inventoryPanel;
        [SerializeField] TMP_Text[] backpackSlotLabels;
        [SerializeField] TMP_Text[] chestSlotLabels;
        [SerializeField] UnityEngine.UI.Button craftButton;
        [SerializeField] UnityEngine.UI.Button depositButton;
        [SerializeField] UnityEngine.UI.Button withdrawButton;
        [SerializeField] UnityEngine.UI.Button craftCloseButton;
        [SerializeField] UnityEngine.UI.Button chestCloseButton;
        [SerializeField] UnityEngine.UI.Button inventoryCloseButton;

        WorldSession _session;
        float _statusUntil;

        public bool MenuOpen => (craftPanel != null && craftPanel.activeSelf) ||
            (chestPanel != null && chestPanel.activeSelf) ||
            (inventoryPanel != null && inventoryPanel.activeSelf);

        void Awake() => ClosePanels();

        public void Bind(WorldSession session)
        {
            _session = session;
            craftButton.onClick.AddListener(() => session.TryCraftChest());
            depositButton.onClick.AddListener(session.DepositAllItems);
            withdrawButton.onClick.AddListener(session.WithdrawAllItems);
            craftCloseButton.onClick.AddListener(ClosePanels);
            chestCloseButton.onClick.AddListener(ClosePanels);
            inventoryCloseButton.onClick.AddListener(ClosePanels);
            Refresh();
        }

        public void Refresh()
        {
            if (_session == null) return;
            Set(dayLabel, $"DAY {_session.CurrentDay}");
            Set(woodLabel, $"WOOD  {_session.WoodCount}");
            Set(loggingLabel, $"LOGGING  LV {_session.LoggingLevel}  •  {_session.LoggingExperience} XP");
            Set(toolLabel, $"TOOL  {_session.EquippedToolName.ToUpperInvariant()}  •  1 / 2 or Y");
            Set(contextLabel, _session.ContextPrompt());
            Set(craftDescription, $"Storage chest  •  {_session.ChestCost} Wood\n" +
                (_session.ChestPlaced ? "Already placed" :
                    _session.PendingChest ? "Ready to place" : $"You have {_session.WoodCount} Wood"));
            Set(chestDescription, $"Backpack: {_session.WoodCount} Wood  •  Chest: {_session.ChestWood} Wood");
            UpdateSlots(backpackSlotLabels, _session.BackpackSlots);
            UpdateSlots(chestSlotLabels, _session.ChestSlots);
            craftButton.interactable = _session.CanCraftChest;
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
            Set(contextLabel, _session.ContextPrompt());
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
                Set(labels[i], slot == null || slot.count == 0 ? $"{i + 1}\n—" :
                    $"{i + 1}\n{_session.ItemName(slot.itemId)} x{slot.count}");
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
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(craftButton.gameObject);
        }

        public void ShowChestPanel()
        {
            craftPanel.SetActive(false);
            inventoryPanel.SetActive(false);
            chestPanel.SetActive(true);
            Refresh();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(depositButton.gameObject);
        }

        public void ToggleInventoryPanel()
        {
            bool open = !inventoryPanel.activeSelf;
            ClosePanels();
            inventoryPanel.SetActive(open);
            if (open)
            {
                Refresh();
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                    inventoryCloseButton.gameObject);
            }
        }

        public void ClosePanels()
        {
            if (craftPanel != null) craftPanel.SetActive(false);
            if (chestPanel != null) chestPanel.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}
