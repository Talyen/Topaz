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
        [SerializeField] UnityEngine.UI.Button craftButton;
        [SerializeField] UnityEngine.UI.Button depositButton;
        [SerializeField] UnityEngine.UI.Button withdrawButton;
        [SerializeField] UnityEngine.UI.Button craftCloseButton;
        [SerializeField] UnityEngine.UI.Button chestCloseButton;

        WorldSession _session;
        float _statusUntil;

        public bool MenuOpen => (craftPanel != null && craftPanel.activeSelf) ||
            (chestPanel != null && chestPanel.activeSelf);

        void Awake() => ClosePanels();

        public void Bind(WorldSession session)
        {
            _session = session;
            craftButton.onClick.AddListener(() => session.TryCraftChest());
            depositButton.onClick.AddListener(session.DepositAllWood);
            withdrawButton.onClick.AddListener(session.WithdrawAllWood);
            craftCloseButton.onClick.AddListener(ClosePanels);
            chestCloseButton.onClick.AddListener(ClosePanels);
            Refresh();
        }

        public void Refresh()
        {
            if (_session == null) return;
            dayLabel.text = $"DAY {_session.CurrentDay}";
            woodLabel.text = $"WOOD  {_session.WoodCount}";
            loggingLabel.text = $"LOGGING  LV {_session.LoggingLevel}  •  {_session.LoggingExperience} XP";
            toolLabel.text = $"TOOL  {_session.EquippedToolName.ToUpperInvariant()}  •  1 / 2 or Y";
            contextLabel.text = _session.ContextPrompt();
            craftDescription.text = $"Storage chest  •  {_session.ChestCost} Wood\n" +
                (_session.ChestPlaced ? "Already placed" :
                    _session.PendingChest ? "Ready to place" : $"You have {_session.WoodCount} Wood");
            chestDescription.text = $"Backpack: {_session.WoodCount} Wood\n" +
                $"Chest: {_session.ChestWood} / {_session.ChestCapacity} Wood";
            craftButton.interactable = _session.CanCraftChest;
            depositButton.interactable = _session.WoodCount > 0 &&
                _session.ChestWood < _session.ChestCapacity;
            withdrawButton.interactable = _session.ChestWood > 0;

            if (_session.SaveProblem != null)
            {
                statusLabel.text = _session.SaveProblem;
                return;
            }
            if (Time.time >= _statusUntil) statusLabel.text = "";
        }

        public void ShowStatus(string message)
        {
            statusLabel.text = message;
            _statusUntil = Time.time + 3.5f;
        }

        public void ShowCraftPanel()
        {
            chestPanel.SetActive(false);
            craftPanel.SetActive(true);
            Refresh();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(craftButton.gameObject);
        }

        public void ShowChestPanel()
        {
            craftPanel.SetActive(false);
            chestPanel.SetActive(true);
            Refresh();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(depositButton.gameObject);
        }

        public void ClosePanels()
        {
            if (craftPanel != null) craftPanel.SetActive(false);
            if (chestPanel != null) chestPanel.SetActive(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}
