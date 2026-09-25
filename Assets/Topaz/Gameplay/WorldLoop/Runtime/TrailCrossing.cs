using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>Walk-through boundary between the two outdoor authored regions.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TrailCrossing : MonoBehaviour
    {
        [SerializeField] string destinationRegion;

        void OnTriggerEnter(Collider other)
        {
            WorldSession session = other.GetComponentInParent<WorldSession>();
            if (session != null) session.RequestTrailCrossing(destinationRegion);
        }

#if UNITY_EDITOR
        public void Configure(string regionId) => destinationRegion = regionId;
#endif
    }
}
