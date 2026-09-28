using UnityEngine;
using UnityEngine.Rendering;

namespace Topaz.Gameplay
{
    public sealed class HomeRoofVisibility : MonoBehaviour
    {
        Transform _player;
        Renderer[] _renderers;
        ShadowCastingMode[] _original;
        public void Bind(Transform player)
        {
            _player = player;
            Restore();
            _renderers = GetComponentsInChildren<Renderer>();
            _original = new ShadowCastingMode[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _original[i] = _renderers[i].shadowCastingMode;
        }

        void OnDisable() => Restore();
        void Restore()
        {
            if (_renderers == null || _original == null) return;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].shadowCastingMode = _original[i];
        }

        void LateUpdate()
        {
            if (_player == null || _renderers == null) return;
            Vector3 delta = _player.position - transform.position;
            delta.y = 0f;
            bool visible = delta.sqrMagnitude > 4.5f * 4.5f;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].shadowCastingMode = visible ? _original[i] : ShadowCastingMode.ShadowsOnly;
        }
    }
}
