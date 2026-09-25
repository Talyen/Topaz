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

        public void OverrideVisual(GameObject prefab, Material material)
        {
            if (prefab == null) return;
            if (visual != null) visual.gameObject.SetActive(false);
            GameObject instance = Instantiate(prefab, transform);
            instance.name = "Reward Visual";
            instance.transform.localPosition = new Vector3(0f, .45f, 0f);
            instance.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            instance.transform.localScale = Vector3.one * .75f;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                if (material != null) renderer.sharedMaterial = material;
            foreach (Collider collision in instance.GetComponentsInChildren<Collider>(true))
                collision.enabled = false;
            visual = instance.transform;
            _visualRest = visual.localPosition;
        }

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
