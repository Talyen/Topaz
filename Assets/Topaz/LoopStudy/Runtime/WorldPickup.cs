using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>A visible, persistent item drop collected by walking near it.</summary>
    public sealed class WorldPickup : MonoBehaviour
    {
        [SerializeField] Transform visual;

        WorldSession _session;
        PickupStateRecord _state;
        ItemDefinition _item;
        Vector3 _visualRest;
        float _collectAfter;

        public PickupStateRecord State => _state;
        public ItemDefinition Item => _item;

        void Awake()
        {
            if (visual != null) _visualRest = visual.localPosition;
        }

        public void Bind(WorldSession session, PickupStateRecord state, ItemDefinition item)
        {
            _session = session;
            _state = state;
            _item = item;
            transform.position = new Vector3(state.x, 0f, state.z);
            _collectAfter = Time.time + 0.45f;
        }

        void Update()
        {
            if (_session == null || _state == null) return;
            if (visual != null)
                visual.localPosition = _visualRest + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.07f);
            if (Time.time < _collectAfter) return;
            Vector3 delta = _session.transform.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 1.15f * 1.15f)
                _session.TryCollect(this);
        }
    }
}
