using System;
using System.Collections.Generic;
using TMPro;
using Topaz.CombatStudy;
using Topaz.LoopStudy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Topaz.LoopStudy
{
    /// <summary>Focused uGUI pages for equipped gear and the finite home rack.</summary>
    public sealed class EquipmentJournalView : MonoBehaviour
    {
        [SerializeField] GameObject equipmentPanel;
        [SerializeField] GameObject rackPanel;
        [SerializeField] Button backpackTab;
        [SerializeField] Button skillsTab;
        [SerializeField] Button equipmentClose;
        [SerializeField] Button rackClose;
        [SerializeField] Button equipButton;
        [SerializeField] Button unequipButton;
        [SerializeField] Button[] slotButtons;
        [SerializeField] TMP_Text[] slotLabels;
        [SerializeField] Button[] packButtons;
        [SerializeField] TMP_Text[] packLabels;
        [SerializeField] Button[] rackButtons;
        [SerializeField] TMP_Text[] rackLabels;
        [SerializeField] TMP_Text selectedName;
        [SerializeField] TMP_Text totals;
        [SerializeField] TMP_Text skills;
        [SerializeField] Button swordToolButton;
        [SerializeField] Button axeToolButton;
        [SerializeField] Button pickaxeToolButton;
        [SerializeField] Image portrait;
        [SerializeField] Sprite[] portraits;

        WorldSession _session;
        LoopHud _hud;
        int _selectedPack = -1;
        int _selectedSlot;
        bool _bound;

        public bool IsOpen => equipmentPanel != null && equipmentPanel.activeSelf;
        public bool IsRackOpen => rackPanel != null && rackPanel.activeSelf;

        public void Bind(WorldSession session, LoopHud hud)
        {
            _session = session;
            _hud = hud;
            if (!_bound)
            {
                backpackTab.onClick.AddListener(() => { _hud.ClosePanels(); _hud.ToggleInventoryPanel(); });
                if (skillsTab != null) skillsTab.onClick.AddListener(_hud.ShowSkillsPanel);
                equipmentClose.onClick.AddListener(_hud.ClosePanels);
                rackClose.onClick.AddListener(_hud.ClosePanels);
                equipButton.onClick.AddListener(() =>
                {
                    if (_session.TryEquipFromBackpack(_selectedPack)) Refresh();
                });
                unequipButton.onClick.AddListener(() =>
                {
                    if (_session.TryUnequip(EquipmentState.Slots[_selectedSlot])) Refresh();
                    else _hud.ShowStatus("Make space in the backpack first.");
                });
                if (swordToolButton != null) swordToolButton.onClick.AddListener(() =>
                    SelectTool("sword"));
                if (axeToolButton != null) axeToolButton.onClick.AddListener(() =>
                    SelectTool("axe"));
                if (pickaxeToolButton != null) pickaxeToolButton.onClick.AddListener(() =>
                    SelectTool("pickaxe"));
                for (int i = 0; i < slotButtons.Length; i++)
                {
                    int index = i;
                    slotButtons[i].onClick.AddListener(() => SelectSlot(index));
                }
                for (int i = 0; i < packButtons.Length; i++)
                {
                    int index = i;
                    packButtons[i].onClick.AddListener(() => SelectPack(index));
                }
                for (int i = 0; i < rackButtons.Length; i++)
                {
                    int index = i;
                    rackButtons[i].onClick.AddListener(() =>
                    {
                        string id = _session.RackItems[index];
                        if (!_session.TryClaimRackItem(id)) _hud.ShowStatus("Make space in the backpack first.");
                        Refresh();
                    });
                }
                _bound = true;
            }
            Refresh();
        }

        public void Show()
        {
            _hud.ClosePanels();
            equipmentPanel.SetActive(true);
            _selectedPack = -1;
            _selectedSlot = 0;
            Refresh();
            EventSystem.current?.SetSelectedGameObject(slotButtons[0].gameObject);
        }

        public void ShowRack()
        {
            _hud.ClosePanels();
            rackPanel.SetActive(true);
            Refresh();
            for (int i = 0; i < rackButtons.Length; i++)
                if (rackButtons[i].interactable)
                {
                    EventSystem.current?.SetSelectedGameObject(rackButtons[i].gameObject);
                    return;
                }
            EventSystem.current?.SetSelectedGameObject(rackClose.gameObject);
        }

        public void Hide()
        {
            if (equipmentPanel != null) equipmentPanel.SetActive(false);
            if (rackPanel != null) rackPanel.SetActive(false);
        }

        void SelectSlot(int index)
        {
            _selectedSlot = index;
            _selectedPack = -1;
            Refresh();
        }

        void SelectPack(int index)
        {
            _selectedPack = index;
            Refresh();
        }

        void SelectTool(string id)
        {
            if (!_session.SelectManualTool(id)) _hud.ShowStatus("Tool is unavailable right now.");
            Refresh();
        }

        public void Refresh()
        {
            if (_session?.Equipped == null) return;
            for (int i = 0; i < slotLabels.Length; i++)
            {
                EquipmentSlot slot = EquipmentState.Slots[i];
                string id = _session.Equipped.Get(slot);
                slotLabels[i].text = $"{slot}\n{(string.IsNullOrEmpty(id) ? "Empty" : _session.ItemName(id))}";
            }
            IReadOnlyList<ItemStackRecord> pack = _session.BackpackSlots;
            for (int i = 0; i < packLabels.Length; i++)
            {
                ItemStackRecord item = i < pack.Count ? pack[i] : null;
                packLabels[i].text = item == null || item.count == 0 ? "—" :
                    _session.ItemName(item.itemId);
            }
            EquipmentStats stats = _session.Stats;
            ItemDefinition candidate = _selectedPack >= 0 && _selectedPack < pack.Count
                ? _session.Item(pack[_selectedPack].itemId) : null;
            EquipmentStats preview = stats;
            if (candidate != null && candidate.EquipmentSlot != EquipmentSlot.None)
            {
                ItemDefinition displaced = _session.Item(_session.Equipped.Get(candidate.EquipmentSlot));
                preview += EquipmentState.EffectiveStats(candidate);
                preview -= EquipmentState.EffectiveStats(displaced);
                if (candidate.EquipmentSlot == EquipmentSlot.Weapon &&
                    candidate.Weapon?.TwoHanded == true)
                    preview -= EquipmentState.EffectiveStats(_session.Item(
                        _session.Equipped.offhandId));
                if (candidate.EquipmentSlot == EquipmentSlot.Offhand &&
                    _session.CurrentWeapon?.TwoHanded == true)
                    preview -= EquipmentState.EffectiveStats(_session.Item(
                        _session.Equipped.weaponId));
            }
            WeaponDefinition previewWeapon = candidate?.EquipmentSlot == EquipmentSlot.Weapon
                ? candidate.Weapon : candidate?.EquipmentSlot == EquipmentSlot.Offhand &&
                  _session.CurrentWeapon?.TwoHanded == true ? null : _session.CurrentWeapon;
            int currentAttack = stats.Attack + SkillAttackBonus(_session.CurrentWeapon);
            int previewAttack = preview.Attack + SkillAttackBonus(previewWeapon);
            totals.text = $"Attack {Stat(currentAttack, previewAttack)}     " +
                $"Attack Speed {Stat(stats.AttackSpeed, preview.AttackSpeed)}\n" +
                $"Armor {Stat(stats.Armor, preview.Armor)}     " +
                $"Move Speed {Stat(stats.MoveSpeed, preview.MoveSpeed)}\n" +
                $"Dodge {Stat(stats.Dodge, preview.Dodge)}     " +
                $"Logging {Stat(stats.Logging, preview.Logging)}";
            if (skills != null)
                skills.text = $"Swords { _session.SwordsLevel}  ({_session.SwordsExperience} XP)     " +
                    $"Axes {_session.AxesLevel}  ({_session.AxesExperience} XP)\n" +
                    $"Logging {_session.LoggingLevel}  ({_session.LoggingExperience} XP)     " +
                    $"Mining {_session.MiningLevel}  ({_session.MiningExperience} XP)";
            SetToolLabel(swordToolButton, "Weapon", "sword");
            SetToolLabel(axeToolButton, "Logging Axe", "axe");
            SetToolLabel(pickaxeToolButton, "Pickaxe", "pickaxe");
            if (_selectedPack >= 0 && _selectedPack < pack.Count && pack[_selectedPack].count > 0)
                selectedName.text = _session.ItemName(pack[_selectedPack].itemId);
            else
            {
                string selectedId = _session.Equipped.Get(EquipmentState.Slots[_selectedSlot]);
                selectedName.text = string.IsNullOrEmpty(selectedId) ? "Empty slot" :
                    _session.ItemName(selectedId);
            }
            equipButton.interactable = candidate != null &&
                candidate.EquipmentSlot != EquipmentSlot.None && pack[_selectedPack].count == 1;
            unequipButton.interactable = !string.IsNullOrEmpty(_session.Equipped.Get(
                EquipmentState.Slots[_selectedSlot]));
            for (int i = 0; i < rackButtons.Length; i++)
            {
                string id = _session.RackItems[i];
                bool available = _session.RackHas(id);
                rackLabels[i].text = available ? _session.ItemName(id) : "Taken";
                rackButtons[i].interactable = available;
            }
            if (portrait != null && portraits != null && portraits.Length == CharacterLooks.All.Length)
            {
                int look = Array.IndexOf(CharacterLooks.All, _session.ActiveAppearanceId);
                portrait.sprite = portraits[Mathf.Max(0, look)];
                portrait.enabled = portrait.sprite != null;
            }
        }

        int SkillAttackBonus(WeaponDefinition weapon) => weapon == null ? 0 :
            Mathf.RoundToInt(_session.SkillOutputBonus(weapon.Skill == WeaponSkill.Axes
                ? SkillIds.Axes : SkillIds.Swords));

        static string Stat(int current, int preview) => current == preview
            ? current.ToString() : current + " → " + preview;

        void SetToolLabel(Button button, string label, string id)
        {
            if (button == null) return;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = (_session.SelectedManualTool == id ? "● " : "") + label;
            button.interactable = id == "axe" ? _session.HasAxe :
                id == "pickaxe" ? _session.HasPickaxe : _session.HasWeapon;
        }
    }
}
