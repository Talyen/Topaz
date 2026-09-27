using Topaz.Gameplay;
using UnityEngine;

namespace Topaz.Generation
{
    /// <summary>A discoverable supply cache. Stock deadlines belong to WorldSession, not this streamed object.</summary>
    public sealed class DiscoveryCache : MonoBehaviour
    {
        public string StableId { get; private set; }
        WorldSession session;
        public bool Stocked => session!=null && session.WorldHours>=session.GetOrCreateNodeState(StableId).readyAtWorldHours;
        public void Initialize(WorldSession owner,string id)
        {session=owner;StableId=id;owner.RegisterDiscoveryCache(this);}
        public bool Open()=>session!=null && session.TryClaimDiscoverySupplies(StableId);
        void OnDestroy(){if(session!=null)session.UnregisterDiscoveryCache(this);}
    }
}
