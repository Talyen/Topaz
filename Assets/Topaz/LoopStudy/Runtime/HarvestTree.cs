using UnityEngine;
using UnityEngine.AI;

namespace Topaz.LoopStudy
{
    /// <summary>One authored tree; its saved ID is independent of its scene instance ID.</summary>
    public sealed class HarvestTree : MonoBehaviour
    {
        [SerializeField] string stableObjectId = "topaz.training.tree.01";
        [SerializeField] HarvestDefinition definition;
        [SerializeField] GameObject visualRoot;
        [SerializeField] Renderer trunkRenderer;
        [SerializeField] Collider trunkCollider;
        [SerializeField] NavMeshObstacle obstacle;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color IdleColor = new Color(0.44f, 0.67f, 0.43f);
        static readonly Color ChopColor = new Color(0.91f, 0.83f, 0.46f);

        WorldSession _session;
        NodeStateRecord _state;
        MaterialPropertyBlock _properties;
        float _flashUntil;

        public string StableObjectId => stableObjectId;
        public bool IsAvailable => _state != null && _state.nextAvailableDay == 0;
        public int ChopsRemaining => _state == null || definition == null ? 0 :
            Mathf.Max(0, definition.ChopsRequired - _state.chops);

        void Awake() => _properties = new MaterialPropertyBlock();

        void Update()
        {
            if (_flashUntil <= 0f || Time.time < _flashUntil) return;
            _flashUntil = 0f;
            SetColor(IdleColor);
        }

        public void Bind(WorldSession session)
        {
            _session = session;
            _state = session.GetOrCreateNodeState(stableObjectId);
            RefreshForDay();
        }

        public bool TryChop(Vector3 attackerPosition, Vector3 direction, float range, float arcDegrees)
        {
            if (_session == null || !IsAvailable || definition == null || definition.YieldItem == null)
                return false;

            Vector3 toTree = transform.position - attackerPosition;
            toTree.y = 0f;
            if (toTree.sqrMagnitude > range * range || toTree.sqrMagnitude < 0.001f ||
                Vector3.Angle(direction, toTree) > arcDegrees * 0.5f) return false;

            _state.chops++;
            if (_state.chops >= definition.ChopsRequired)
            {
                _state.chops = 0;
                _state.nextAvailableDay = _session.CurrentDay + definition.RegrowthDays;
                _session.CompleteHarvest(definition);
                ApplyAvailability();
            }
            else
            {
                _flashUntil = Time.time + 0.18f;
                SetColor(ChopColor);
                _session.Commit();
            }
            return true;
        }

        public void RefreshForDay()
        {
            if (_state == null) return;
            if (_state.nextAvailableDay != 0 && _session.CurrentDay >= _state.nextAvailableDay)
            {
                _state.nextAvailableDay = 0;
                _state.chops = 0;
            }
            ApplyAvailability();
        }

        void ApplyAvailability()
        {
            bool available = IsAvailable;
            if (visualRoot != null) visualRoot.SetActive(available);
            if (trunkCollider != null) trunkCollider.enabled = available;
            if (obstacle != null) obstacle.enabled = available;
            if (available) SetColor(IdleColor);
        }

        void SetColor(Color color)
        {
            if (trunkRenderer == null) return;
            _properties.SetColor(BaseColor, color);
            trunkRenderer.SetPropertyBlock(_properties);
        }
    }
}
