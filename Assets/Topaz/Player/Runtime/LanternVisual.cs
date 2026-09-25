using UnityEngine;

namespace Topaz.FeelStudy
{
    /// <summary>The carried lantern's authored light-emitting surface, independent of mesh names.</summary>
    public sealed class LanternVisual : MonoBehaviour
    {
        [SerializeField] Renderer ember;
        public void SetLit(bool lit) { if (ember != null) ember.enabled = lit; }
    }
}
