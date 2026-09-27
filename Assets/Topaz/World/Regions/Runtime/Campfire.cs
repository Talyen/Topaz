using UnityEngine;

namespace Topaz.Gameplay
{
    /// <summary>An authored recovery point with a visible activation area and safe arrival.</summary>
    public sealed class Campfire : MonoBehaviour
    {
        public const string HomeId = "campfire.home";
        public const string ClearingId = "campfire.expedition.clearing";

        [SerializeField] string stableId = HomeId;
        [SerializeField] string regionId = TopazSaveData.HomeRegion;
        [SerializeField] string travelLabel;
        [SerializeField, Min(0.1f)] float activationRadius = 1.6f;
        [SerializeField] Transform arrival;
        [SerializeField] ParticleSystem embers;

        public string StableId => stableId;
        public string RegionId => regionId;
        public string TravelLabel => !string.IsNullOrWhiteSpace(travelLabel) ? travelLabel :
            stableId == HomeId ? "Home" : stableId == ClearingId ? "Woodland" :
            string.Empty;
        void Awake() { if (GetComponent<Topaz.Combat.CampSafety>() == null) gameObject.AddComponent<Topaz.Combat.CampSafety>(); }
        public void Configure(string id, string region, string label)
        {
            stableId = id; regionId = region; travelLabel = string.IsNullOrEmpty(label) ? "Camp " + id.Substring(0, 6) : label;
            if (arrival == null) { arrival = new GameObject("Arrival").transform; arrival.SetParent(transform, false); arrival.localPosition = Vector3.forward * 1.25f; }
        }

        public Vector3 ArrivalPosition => arrival != null ? arrival.position : transform.position;
        public bool HasArrival => arrival != null;

        public bool Contains(Vector3 position)
        {
            Vector3 delta = position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= activationRadius * activationRadius;
        }

        public void ShowActivation()
        {
            if (embers != null) embers.Emit(12);
        }
    }
}
