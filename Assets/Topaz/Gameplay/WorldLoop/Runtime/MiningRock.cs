using UnityEngine;
using UnityEngine.AI;

namespace Topaz.LoopStudy
{
    /// <summary>An authored, World-owned Stone and Iron deposit.</summary>
    public sealed class MiningRock : MonoBehaviour
    {
        [SerializeField] string stableObjectId;
        [SerializeField] MiningDefinition definition;
        [SerializeField] GameObject visualRoot;
        [SerializeField] Collider rockCollider;
        [SerializeField] NavMeshObstacle obstacle;

        WorldSession _session;
        NodeStateRecord _state;

        public string StableObjectId => stableObjectId;
        public bool IsAvailable => _state != null && _state.readyAtWorldHours == 0d &&
            !(_session?.HomeBlocksResource(transform.position, .75f) ?? false);
        public int StrikesRemaining => _state == null ? 0 :
            Mathf.Max(0, (definition != null ? definition.WorkRequired : 2) - _state.chops);

        public void Bind(WorldSession session)
        {
            _session = session;
            _state = session.GetOrCreateNodeState(stableObjectId);
            RefreshForTime();
        }

        void Update()
        {
            if (_state != null && _state.readyAtWorldHours > 0d &&
                _session.WorldHours >= _state.readyAtWorldHours)
            {
                RefreshForTime();
                _session.Commit();
            }
        }

        public bool TryStrike(Vector3 attackerPosition, Vector3 direction, float range,
            float arcDegrees)
        {
            if (_session == null || !IsAvailable || definition == null) return false;
            Vector3 toRock = transform.position - attackerPosition;
            toRock.y = 0f;
            if (toRock.sqrMagnitude < 0.001f || toRock.sqrMagnitude > range * range ||
                Vector3.Angle(direction, toRock) > arcDegrees * 0.5f) return false;

            _state.chops += 1 + Mathf.RoundToInt(
                _session.TalentAmount(SkillIds.Mining, "mining.heavy-pick"));
            if (_state.chops >= definition.WorkRequired)
            {
                _state.chops = 0;
                _state.readyAtWorldHours = _session.WorldHours + definition.RegrowthHours;
                _session.DropMining(transform.position, definition);
                _session.RecordSkillCompletion(SkillIds.Mining, definition.Experience,
                    definition.SourceLevel);
                ApplyAvailability();
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
            if (rockCollider != null) rockCollider.enabled = available;
            if (obstacle != null) obstacle.enabled = available;
        }
    }
}
