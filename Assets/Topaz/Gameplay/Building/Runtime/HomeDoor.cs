using UnityEngine;

namespace Topaz.Gameplay
{
    /// <summary>Opens a home doorway for a nearby player without an extra prompt.</summary>
    public sealed class HomeDoor : MonoBehaviour
    {
        [SerializeField] Transform panel;
        [SerializeField] Collider panelCollider;
        Transform _player;
        SpatialShelter _shelter;
        GameObject _structure;
        bool _wasOpen;
        bool _shelterSettled;
        public void BindShelter(SpatialShelter shelter, GameObject structure) { _shelter = shelter; _structure = structure; }

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
            if (open != _wasOpen)
            {
                _wasOpen = open;
                _shelterSettled = false;
                if (_shelter != null) { Physics.SyncTransforms(); _shelter.Invalidate(_shelter.BoundsOf(_structure)); }
            }
            if (!_shelterSettled && Quaternion.Angle(panel.localRotation,target) < .1f)
            {
                _shelterSettled = true;
                if (_shelter != null) { Physics.SyncTransforms(); _shelter.Invalidate(_shelter.BoundsOf(_structure)); }
            }
        }
    }
}
