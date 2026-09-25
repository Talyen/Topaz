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
        [SerializeField] Color idleTint = new Color(0.44f, 0.67f, 0.43f);
        [SerializeField] Color chopTint = new Color(0.91f, 0.83f, 0.46f);

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        WorldSession _session;
        NodeStateRecord _state;
        MaterialPropertyBlock _properties;
        float _flashUntil;

        public string StableObjectId => stableObjectId;
        public string RequiredToolId => definition != null ? definition.RequiredToolId : null;
        public bool IsAvailable => _state != null && _state.readyAtWorldHours == 0d;
        public int ChopsRemaining => _state == null || definition == null ? 0 :
            Mathf.Max(0, definition.ChopsRequired - _state.chops);

        void Awake() => _properties = new MaterialPropertyBlock();

        void Update()
        {
            if (_state != null && _state.readyAtWorldHours > 0d &&
                _session.WorldHours >= _state.readyAtWorldHours)
            {
                RefreshForTime();
                _session.Commit();
            }
            if (_flashUntil > 0f && Time.time >= _flashUntil)
            {
                _flashUntil = 0f;
                SetColor(idleTint);
            }
        }

        public void Bind(WorldSession session)
        {
            _session = session;
            _state = session.GetOrCreateNodeState(stableObjectId);
            RefreshForTime();
        }

        public bool TryChop(Vector3 attackerPosition, Vector3 direction, float range, float arcDegrees)
        {
            if (_session == null || !IsAvailable || definition == null || definition.YieldItem == null)
                return false;

            Vector3 toTree = transform.position - attackerPosition;
            toTree.y = 0f;
            if (toTree.sqrMagnitude > range * range || toTree.sqrMagnitude < 0.001f ||
                Vector3.Angle(direction, toTree) > arcDegrees * 0.5f) return false;

            _state.chops += Mathf.Max(1, _session.Stats.Logging) +
                Mathf.RoundToInt(_session.TalentAmount(SkillIds.Logging, "logging.deep-bite"));
            if (_state.chops >= definition.ChopsRequired)
            {
                _state.chops = 0;
                double regrowth = WorldClock.RegrowthHours(definition.RegrowthDays);
                if (_session.HasTalent(SkillIds.Logging, "logging.stewardship"))
                    regrowth = System.Math.Max(8d, regrowth -
                        _session.TalentAmount(SkillIds.Logging, "logging.stewardship"));
                _state.readyAtWorldHours = _session.WorldHours + regrowth;
                _session.DropHarvest(definition, transform.position);
                _session.RecordSkillCompletion(SkillIds.Logging,
                    definition.CompletionExperience, definition.SourceLevel);
                ApplyAvailability();
            }
            else
            {
                _flashUntil = Time.time + 0.18f;
                SetColor(chopTint);
            }
            _session.Commit();
            return true;
        }

        public void RefreshForTime()
        {
            if (_state == null) return;
            if (_state.readyAtWorldHours > 0d &&
                _session.WorldHours >= _state.readyAtWorldHours)
            {
                _state.readyAtWorldHours = 0d;
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
            if (available) SetColor(idleTint);
        }

        void SetColor(Color color)
        {
            if (trunkRenderer == null) return;
            _properties.SetColor(BaseColor, color);
            trunkRenderer.SetPropertyBlock(_properties);
        }
    }
}
