using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>Opens a home doorway for a nearby player without an extra prompt.</summary>
    public sealed class HomeDoor : MonoBehaviour
    {
        [SerializeField] Transform panel;
        [SerializeField] Collider panelCollider;
        Transform _player;

        public void Bind(Transform panel, Collider panelCollider, Transform player)
        {
            this.panel = panel;
            this.panelCollider = panelCollider;
            _player = player;
        }

        void Update()
        {
            if (panel == null || _player == null) return;
            Vector3 offset = _player.position - transform.position;
            offset.y = 0f;
            bool open = offset.sqrMagnitude < 1.8f * 1.8f;
            Quaternion target = Quaternion.Euler(0f, open ? 95f : 0f, 0f);
            panel.localRotation = Quaternion.RotateTowards(panel.localRotation, target,
                300f * Time.deltaTime);
            if (panelCollider != null) panelCollider.enabled = !open;
        }
    }
}
