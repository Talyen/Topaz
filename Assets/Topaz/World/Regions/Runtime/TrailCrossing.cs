using UnityEngine;

namespace Topaz.Gameplay
{
    /// <summary>Retired authoring marker retained for the source scene; the continuous world has no trail transitions.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TrailCrossing : MonoBehaviour
    {
        [SerializeField] string destinationRegion;

#if UNITY_EDITOR
        public void Configure(string regionId) => destinationRegion = regionId;
#endif
    }
}
