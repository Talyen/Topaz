using UnityEngine;

namespace Topaz.LoopStudy
{
    public sealed class HomeRoofVisibility : MonoBehaviour
    {
        Transform _player;
        Renderer[] _renderers;
        public void Bind(Transform player)
        {
            _player = player;
            _renderers = GetComponentsInChildren<Renderer>();
        }

        void LateUpdate()
        {
            if (_player == null || _renderers == null) return;
            Vector3 delta = _player.position - transform.position;
            delta.y = 0f;
            bool visible = delta.sqrMagnitude > 4.5f * 4.5f;
            foreach (Renderer renderer in _renderers)
                if (renderer != null) renderer.enabled = visible;
        }
    }
}
