using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>An authored recovery point with a visible activation area and safe arrival.</summary>
    public sealed class Campfire : MonoBehaviour
    {
        public const string HomeId = "campfire.home";
        public const string ClearingId = "campfire.expedition.clearing";
        public const string CryptId = "campfire.dungeon.home-crypt";

        [SerializeField] string stableId = HomeId;
        [SerializeField] string regionId = TopazSaveData.HomeRegion;
        [SerializeField] string travelLabel;
        [SerializeField, Min(0.1f)] float activationRadius = 1.6f;
        [SerializeField] Transform arrival;
        [SerializeField] ParticleSystem embers;

        public string StableId => stableId;
        public string RegionId => regionId;
        public string TravelLabel => !string.IsNullOrWhiteSpace(travelLabel) ? travelLabel :
            stableId == HomeId ? "Home" : stableId == ClearingId ? "Graveyard" :
            stableId == CryptId ? "Crypt" : string.Empty;
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
