using UnityEngine;

namespace Topaz.FeelStudy
{
    /// <summary>Repeatable interaction target; deliberately has no inventory or skill reward.</summary>
    public sealed class PracticeNode : MonoBehaviour
    {
        [SerializeField] Renderer nodeRenderer;
        [SerializeField] GameObject rangeRing;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color IdleColor = new Color(0.83f, 0.58f, 0.29f);
        static readonly Color NearbyColor = new Color(1f, 0.78f, 0.36f);
        static readonly Color ActivatedColor = new Color(0.43f, 0.88f, 0.50f);

        MaterialPropertyBlock _properties;
        float _activatedUntil;
        bool _nearby;

        void Awake()
        {
            _properties = new MaterialPropertyBlock();
            if (rangeRing != null) rangeRing.SetActive(false);
            RefreshColor();
        }

        void Update()
        {
            if (_activatedUntil <= 0f || Time.time < _activatedUntil) return;
            _activatedUntil = 0f;
            RefreshColor();
        }

        public void SetNearby(bool nearby)
        {
            if (_nearby == nearby) return;
            _nearby = nearby;
            if (rangeRing != null) rangeRing.SetActive(nearby);
            RefreshColor();
        }

        public void Interact()
        {
            _activatedUntil = Time.time + 1.5f;
            RefreshColor();
        }

        void RefreshColor()
        {
            if (nodeRenderer == null) return;
            Color color = _activatedUntil > Time.time
                ? ActivatedColor
                : _nearby ? NearbyColor : IdleColor;
            _properties.SetColor(BaseColor, color);
            nodeRenderer.SetPropertyBlock(_properties);
        }
    }
}
