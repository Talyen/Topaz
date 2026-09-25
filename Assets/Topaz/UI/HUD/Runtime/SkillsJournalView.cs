using TMPro;
using Topaz.LoopStudy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Topaz.LoopStudy
{
    /// <summary>Journal presentation for Character-owned levels and talent loadouts.</summary>
    public sealed class SkillsJournalView : MonoBehaviour
    {
        [SerializeField] GameObject skillsPanel;
        [SerializeField] Button equipmentTab;
        [SerializeField] Button closeButton;
        [SerializeField] Button actionButton;
        [SerializeField] TMP_Text actionLabel;
        [SerializeField] TMP_Text summary;
        [SerializeField] TMP_Text detail;
        [SerializeField] Button[] skillButtons;
        [SerializeField] TMP_Text[] skillLabels;
        [SerializeField] Button[] talentButtons;
        [SerializeField] TMP_Text[] talentLabels;

        WorldSession _session;
        LoopHud _hud;
        int _selectedSkill;
        int _selectedTalent;
        bool _bound;

        public bool IsOpen => skillsPanel != null && skillsPanel.activeSelf;

        public void Bind(WorldSession session, LoopHud hud)
        {
            _session = session;
            _hud = hud;
            if (!_bound)
            {
                equipmentTab.onClick.AddListener(() => _hud.ShowEquipmentPanel());
                closeButton.onClick.AddListener(_hud.ClosePanels);
                actionButton.onClick.AddListener(ApplyAction);
                for (int i = 0; i < skillButtons.Length; i++)
                {
                    int index = i;
                    skillButtons[i].onClick.AddListener(() =>
                    { _selectedSkill = index; _selectedTalent = 0; Refresh(); });
                }
                for (int i = 0; i < talentButtons.Length; i++)
                {
                    int index = i;
                    talentButtons[i].onClick.AddListener(() =>
                    { _selectedTalent = index; Refresh(); });
                }
                _bound = true;
            }
            Refresh();
        }

        public void Show()
        {
            _hud.ClosePanels();
            skillsPanel.SetActive(true);
            Refresh();
            EventSystem.current?.SetSelectedGameObject(skillButtons[_selectedSkill].gameObject);
        }

        public void Hide() => skillsPanel?.SetActive(false);

        void ApplyAction()
        {
            SkillDefinition skill = SelectedSkill();
            if (skill == null || _selectedTalent >= skill.Talents.Count) return;
            string talentId = skill.Talents[_selectedTalent].id;
            if (!_session.IsTalentLearned(skill.StableId, talentId))
                _session.TryLearnTalent(skill.StableId, talentId);
            else
                _session.TrySetTalentActive(skill.StableId, talentId,
                    !_session.HasTalent(skill.StableId, talentId));
            Refresh();
        }

        SkillDefinition SelectedSkill()
        {
            var definitions = _session?.SkillDefinitions;
            return definitions != null && _selectedSkill < definitions.Count
                ? definitions[_selectedSkill] : null;
        }

        public void Refresh()
        {
            if (_session == null || skillButtons == null) return;
            var definitions = _session.SkillDefinitions;
            if (definitions == null) return;
            for (int i = 0; i < skillButtons.Length; i++)
            {
                SkillDefinition skill = i < definitions.Count ? definitions[i] : null;
                bool visible = skill != null &&
                    (skill.StableId != SkillIds.Staff || _session.StaffDiscovered) &&
                    (skill.StableId != SkillIds.Crossbows || _session.CrossbowsDiscovered);
                skillButtons[i].gameObject.SetActive(visible);
                skillButtons[i].interactable = visible;
                if (!visible && _selectedSkill == i) _selectedSkill = 0;
                if (skill != null)
                {
                    int level = _session.SkillLevel(skill.StableId);
                    int choices = _session.SkillChoices(skill.StableId);
                    skillLabels[i].text = $"{skill.DisplayName}  {level}" +
                        (choices > 0 ? $"  • {choices} choice" : "");
                }
            }
            SkillDefinition selected = SelectedSkill();
            if (selected == null) return;
            string id = selected.StableId;
            int currentLevel = _session.SkillLevel(id);
            int centi = _session.SkillExperienceCenti(id);
            string progress = currentLevel == 10 ? "Mastered" :
                $"{centi / 100f:0.##} / {SkillProgression.Thresholds[currentLevel] / 100f:0.##} XP";
            summary.text = $"{selected.DisplayName}  •  Level {currentLevel}\n{progress}\n" +
                selected.LevelBenefit + "\n" +
                $"Active talents: {_session.ActiveTalentCount(id)} / " +
                (currentLevel >= 5 ? "2" : currentLevel >= 2 ? "1" : "0") +
                (_session.SkillChoices(id) > 0 ?
                    $"     {_session.SkillChoices(id)} choice ready" : "");
            for (int i = 0; i < talentButtons.Length; i++)
            {
                bool exists = i < selected.Talents.Count;
                talentButtons[i].interactable = exists;
                if (!exists) continue;
                TalentDefinition talent = selected.Talents[i];
                string state = _session.HasTalent(id, talent.id) ? "ACTIVE" :
                    _session.IsTalentLearned(id, talent.id) ? "LEARNED" :
                    _session.SkillChoices(id) > 0 ? "READY" : "LOCKED";
                talentLabels[i].text = $"{talent.label}   •   {state}";
            }
            if (_selectedTalent >= selected.Talents.Count) _selectedTalent = 0;
            TalentDefinition current = selected.Talents[_selectedTalent];
            bool learned = _session.IsTalentLearned(id, current.id);
            bool active = _session.HasTalent(id, current.id);
            detail.text = current.description + "\n" +
                (active ? "Active. Change talents freely at home." :
                    learned ? "Learned. Equip this talent at home." :
                    _session.SkillChoices(id) > 0 ? "Ready to learn." :
                    "A talent choice unlocks at levels 2, 5, and 8; all unlock at 10.");
            actionButton.interactable = !learned ? _session.SkillChoices(id) > 0 :
                _session.IsAtHome && (active || _session.ActiveTalentCount(id) <
                    (currentLevel >= 5 ? 2 : currentLevel >= 2 ? 1 : 0));
            actionLabel.text = !learned ? "Learn" : active ? "Unequip" :
                _session.IsAtHome ? "Equip" : "Return home";
        }
    }
}
