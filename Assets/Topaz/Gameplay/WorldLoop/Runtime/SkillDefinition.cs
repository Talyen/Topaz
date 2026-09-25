using System;
using System.Collections.Generic;
using UnityEngine;

namespace Topaz.LoopStudy
{
    [CreateAssetMenu(menuName = "Topaz/Progression/Skill")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [SerializeField] string stableId;
        [SerializeField] string displayName;
        [SerializeField, TextArea(2, 4)] string levelBenefit;
        [SerializeField] float handlingPerLevel;
        [SerializeField] float benefitAtFive;
        [SerializeField] float benefitAtTen;
        [SerializeField] TalentDefinition[] talents = Array.Empty<TalentDefinition>();

        public string StableId => stableId;
        public string DisplayName => displayName;
        public string LevelBenefit => levelBenefit;
        public float HandlingPerLevel => handlingPerLevel;
        public float BenefitAtFive => benefitAtFive;
        public float BenefitAtTen => benefitAtTen;
        public IReadOnlyList<TalentDefinition> Talents => talents;
        public TalentDefinition Talent(string id)
        {
            foreach (TalentDefinition talent in talents)
                if (talent.id == id) return talent;
            return null;
        }

#if UNITY_EDITOR
        public void Configure(string id, string label, string benefit, float handling,
            float atFive, float atTen, TalentDefinition[] choices)
        {
            stableId = id;
            displayName = label;
            levelBenefit = benefit;
            handlingPerLevel = handling;
            benefitAtFive = atFive;
            benefitAtTen = atTen;
            talents = choices;
        }
#endif
    }
}
