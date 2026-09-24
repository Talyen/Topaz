using TMPro;
using Topaz.CombatStudy;
using Topaz.LoopStudy;
using UnityEngine;

namespace Topaz.UI
{
    /// <summary>Displays health briefly after damage or while health is reduced.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TransientVitalityView : MonoBehaviour
    {
        [SerializeField] PlayerVitality vitality;
        [SerializeField] WorldSession session;
        [SerializeField] TMP_Text value;

        CanvasGroup _group;

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;
        }

        void Update()
        {
            if (vitality == null || value == null) return;
            bool visible = (session == null || !session.MenuOpen) &&
                (vitality.CurrentHealth < vitality.MaximumHealth ||
                 Time.unscaledTime - vitality.LastDamageTime < 4f);
            _group.alpha = Mathf.MoveTowards(_group.alpha, visible ? 1f : 0f,
                Time.unscaledDeltaTime * 4f);
            if (visible) value.text = $"Health  {vitality.CurrentHealth} / {vitality.MaximumHealth}";
        }
    }
}
