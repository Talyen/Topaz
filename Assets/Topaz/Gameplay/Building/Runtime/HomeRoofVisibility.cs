using UnityEngine;
using UnityEngine.Rendering;

namespace Topaz.Gameplay
{
    public sealed class HomeRoofVisibility : MonoBehaviour
    {
        Transform _player;
        Renderer[] _renderers;
        ShadowCastingMode[] _original;
        bool hidden;
        public bool IsCutAway => hidden;
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
            hidden=false;
            if (_renderers == null || _original == null) return;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].shadowCastingMode = _original[i];
        }

        void LateUpdate()
        {
            if (_player == null || _renderers == null) return;
            Bounds bounds=new Bounds();bool first=true;
            foreach(var renderer in _renderers)
                if(renderer!=null){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
            Vector3 p=_player.position;
            float margin=hidden?.35f:0;
            // Membership follows the roof footprint and its interior below, including rotated/moved builds.
            hidden=!first && p.x>=bounds.min.x-margin && p.x<=bounds.max.x+margin &&
                p.z>=bounds.min.z-margin && p.z<=bounds.max.z+margin &&
                p.y<bounds.max.y && p.y>bounds.min.y-5;
            bool visible=!hidden;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].shadowCastingMode = visible ? _original[i] : ShadowCastingMode.ShadowsOnly;
        }
    }
}
