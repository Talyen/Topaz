using System;
using System.Collections.Generic;

namespace Topaz.LoopStudy
{
    [Serializable]
    public sealed class TopazSaveData
    {
        public const int CurrentVersion = 5;
        public const string HomeRegion = "home";
        public const string ExpeditionRegion = "expedition.clearing";

        public int version = CurrentVersion;
        public int day = 1; // Legacy rest counter retained for versions 1–4 migration.
        public double worldHours = WorldClock.StartingHour;
        public int loggingExperience;
        public int swordsExperience;
        public string equippedTool = "sword";
        public float playerX;
        public float playerZ;
        public string regionId = HomeRegion;
        public bool expeditionCacheClaimed;
        public bool pendingChest;
        public List<ItemStackRecord> backpackSlots = new List<ItemStackRecord>();
        public List<NodeStateRecord> nodes = new List<NodeStateRecord>();
        public List<StructureStateRecord> structures = new List<StructureStateRecord>();
        public List<PickupStateRecord> pickups = new List<PickupStateRecord>();
    }

    [Serializable]
    public sealed class ItemStackRecord
    {
        public string itemId;
        public int count;
    }

    [Serializable]
    public sealed class NodeStateRecord
    {
        public string objectId;
        public int chops;
        public int nextAvailableDay; // Legacy threshold used only during migration.
        public double readyAtWorldHours;
    }

    [Serializable]
    public sealed class StructureStateRecord
    {
        public string instanceId;
        public string definitionId;
        public float x;
        public float z;
        public List<ItemStackRecord> slots = new List<ItemStackRecord>();
    }

    [Serializable]
    public sealed class PickupStateRecord
    {
        public string instanceId;
        public string itemId;
        public int count;
        public float x;
        public float z;
    }
}
