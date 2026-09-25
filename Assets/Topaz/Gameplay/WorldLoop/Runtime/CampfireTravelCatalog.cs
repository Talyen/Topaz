using System;
using System.Collections.Generic;
using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>Build-generated metadata for fires in scenes that are not loaded yet.</summary>
    public sealed class CampfireTravelCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Destination
        {
            public string stableId;
            public string regionId;
            public string sceneName;
            public string label;
        }

        [SerializeField] List<Destination> destinations = new List<Destination>();

        public IReadOnlyList<Destination> Destinations => destinations;

        public Destination Find(string stableId) => destinations.Find(value =>
            value.stableId == stableId);
    }
}
