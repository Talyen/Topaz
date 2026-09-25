using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>An authored, World-owned one-pick forage source.</summary>
    public sealed class ForagePlant : MonoBehaviour
    {
        [SerializeField] string stableObjectId;
        [SerializeField] bool mushrooms;
        [SerializeField] GameObject harvestVisual;

        WorldSession _session;
        NodeStateRecord _state;

        public string StableObjectId => stableObjectId;
        public bool Mushrooms => mushrooms;
        public bool IsAvailable => _state != null &&
            (_state.readyAtWorldHours == 0d || _session.WorldHours >= _state.readyAtWorldHours);

        public void Bind(WorldSession session)
        {
            _session = session;
            _state = session.GetOrCreateNodeState(stableObjectId);
            RefreshForTime();
        }

        public void RefreshForTime()
        {
            if (_state == null) return;
            if (_state.readyAtWorldHours > 0d && _session.WorldHours >= _state.readyAtWorldHours)
                _state.readyAtWorldHours = 0d;
            if (harvestVisual != null) harvestVisual.SetActive(_state.readyAtWorldHours == 0d);
        }

        public bool TryForage()
        {
            if (_session == null || !IsAvailable || !_session.TryCollectForage(mushrooms))
                return false;
            _state.readyAtWorldHours = _session.WorldHours + SurvivalRules.ForageRenewalHours;
            RefreshForTime();
            _session.Commit();
            return true;
        }
    }
}
