using UnityEngine;

namespace Topaz.Player
{
    /// <summary>The carried lantern's authored light-emitting surface, independent of mesh names.</summary>
    public sealed class LanternVisual : MonoBehaviour
    {
        [SerializeField] Renderer ember;
        public Vector3 LightPosition => ember != null ? ember.bounds.center : transform.position;
        public void SetLit(bool lit) { if (ember != null) ember.enabled = lit; }
    }
}
