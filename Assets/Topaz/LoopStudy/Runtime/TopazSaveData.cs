using System;
using System.Collections.Generic;

namespace Topaz.LoopStudy
{
    [Serializable]
    public sealed class TopazSaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int day = 1;
        public int loggingExperience;
        public string equippedTool = "sword";
        public float playerX;
        public float playerZ;
        public bool pendingChest;
        public List<ItemStackRecord> backpack = new List<ItemStackRecord>();
        public List<NodeStateRecord> nodes = new List<NodeStateRecord>();
        public List<StructureStateRecord> structures = new List<StructureStateRecord>();
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
        public int nextAvailableDay;
    }

    [Serializable]
    public sealed class StructureStateRecord
    {
        public string instanceId;
        public string definitionId;
        public float x;
        public float z;
        public int woodStored;
    }
}
