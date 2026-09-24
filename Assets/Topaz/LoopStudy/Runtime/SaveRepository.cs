using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>One versioned local slot, with an atomic replacement and backup.</summary>
    public sealed class SaveRepository
    {
        [Serializable]
        sealed class VersionEnvelope { public int version; }

        [Serializable]
        sealed class LegacySaveData
        {
            public int version;
            public int day;
            public int loggingExperience;
            public string equippedTool;
            public float playerX;
            public float playerZ;
            public bool pendingChest;
            public List<ItemStackRecord> backpack;
            public List<NodeStateRecord> nodes;
            public List<LegacyStructure> structures;
        }

        [Serializable]
        sealed class LegacyStructure
        {
            public string instanceId;
            public string definitionId;
            public float x;
            public float z;
            public int woodStored;
        }

        readonly string _path;
        readonly object _gate = new object();
        Task _worker = Task.CompletedTask;
        string _pendingJson;
        Exception _backgroundError;
        bool _working;

        public SaveRepository(string directory)
        {
            _path = Path.Combine(directory, "topaz-save.json");
        }

        public string PathOnDisk => _path;
        public Exception BackgroundError
        {
            get { lock (_gate) return _backgroundError; }
        }

        public TopazSaveData Load()
        {
            Flush();
            if (!File.Exists(_path)) return new TopazSaveData();
            try
            {
                return Parse(File.ReadAllText(_path));
            }
            catch (Exception primaryError)
            {
                string backup = _path + ".bak";
                if (File.Exists(backup))
                {
                    try { return Parse(File.ReadAllText(backup)); }
                    catch (Exception backupError)
                    {
                        throw new InvalidDataException("Both Topaz save copies are unreadable.",
                            new AggregateException(primaryError, backupError));
                    }
                }
                throw new InvalidDataException("Topaz save is unreadable; it has not been overwritten.", primaryError);
            }
        }

        public void Save(TopazSaveData data)
        {
            Flush();
            WriteJson(Serialize(data));
        }

        public void QueueSave(TopazSaveData data)
        {
            string snapshot = Serialize(data); // JsonUtility runs on Unity's main thread.
            lock (_gate)
            {
                if (_backgroundError != null)
                    throw new IOException("Topaz background save failed.", _backgroundError);
                _pendingJson = snapshot; // Coalesce rapid gameplay changes into the newest snapshot.
                if (_working) return;
                _working = true;
                _worker = Task.Run(Drain);
            }
        }

        public void Flush()
        {
            Task worker;
            lock (_gate) worker = _worker;
            worker.GetAwaiter().GetResult();
            Exception error = BackgroundError;
            if (error != null) throw new IOException("Topaz background save failed.", error);
        }

        void Drain()
        {
            while (true)
            {
                string snapshot;
                lock (_gate)
                {
                    snapshot = _pendingJson;
                    _pendingJson = null;
                    if (snapshot == null)
                    {
                        _working = false;
                        return;
                    }
                }
                try { WriteJson(snapshot); }
                catch (Exception error)
                {
                    lock (_gate)
                    {
                        _backgroundError = error;
                        _pendingJson = null;
                        _working = false;
                    }
                    return;
                }
            }
        }

        static string Serialize(TopazSaveData data)
        {
            if (data == null || data.version != TopazSaveData.CurrentVersion)
                throw new InvalidDataException("Cannot write an unsupported Topaz save version.");
            return JsonUtility.ToJson(data, true);
        }

        void WriteJson(string json)
        {
            string directory = Path.GetDirectoryName(_path);
            Directory.CreateDirectory(directory);
            string temporary = _path + ".tmp";
            File.WriteAllText(temporary, json);
            if (File.Exists(_path)) File.Replace(temporary, _path, _path + ".bak");
            else File.Move(temporary, _path);
        }

        static TopazSaveData Parse(string json)
        {
            VersionEnvelope envelope = JsonUtility.FromJson<VersionEnvelope>(json);
            TopazSaveData data = envelope != null && envelope.version == 1
                ? MigrateV1(JsonUtility.FromJson<LegacySaveData>(json))
                : JsonUtility.FromJson<TopazSaveData>(json);
            if (data != null && envelope != null && envelope.version == 2)
            {
                data.pickups = new List<PickupStateRecord>();
            }
            if (data != null && envelope != null && (envelope.version == 2 || envelope.version == 3))
            {
                data.version = TopazSaveData.CurrentVersion;
                data.regionId = TopazSaveData.HomeRegion;
            }
            if (data == null || data.version != TopazSaveData.CurrentVersion ||
                data.day < 1 || data.backpackSlots == null || data.backpackSlots.Count > 16 ||
                data.nodes == null || data.structures == null || data.pickups == null ||
                (data.regionId != TopazSaveData.HomeRegion &&
                 data.regionId != TopazSaveData.ExpeditionRegion))
                throw new InvalidDataException("Topaz save format or version is invalid.");
            foreach (StructureStateRecord structure in data.structures)
                if (structure == null || structure.slots == null || structure.slots.Count > 12)
                    throw new InvalidDataException("Topaz structure slots are invalid.");
            foreach (PickupStateRecord pickup in data.pickups)
                if (pickup == null || string.IsNullOrEmpty(pickup.instanceId) ||
                    string.IsNullOrEmpty(pickup.itemId) || pickup.count < 1)
                    throw new InvalidDataException("Topaz pickup state is invalid.");
            return data;
        }

        static TopazSaveData MigrateV1(LegacySaveData old)
        {
            if (old == null || old.day < 1 || old.backpack == null ||
                old.nodes == null || old.structures == null)
                throw new InvalidDataException("Topaz version 1 save is invalid.");
            var migrated = new TopazSaveData
            {
                day = old.day,
                loggingExperience = old.loggingExperience,
                equippedTool = old.equippedTool,
                playerX = old.playerX,
                playerZ = old.playerZ,
                pendingChest = old.pendingChest,
                backpackSlots = SplitLegacyStacks(old.backpack, 16),
                nodes = old.nodes
            };
            foreach (LegacyStructure structure in old.structures)
            {
                if (structure == null || structure.woodStored < 0)
                    throw new InvalidDataException("Topaz version 1 structure is invalid.");
                var placed = new StructureStateRecord
                {
                    instanceId = structure.instanceId,
                    definitionId = structure.definitionId,
                    x = structure.x,
                    z = structure.z
                };
                if (structure.woodStored > 0)
                    placed.slots = SplitLegacyStacks(new List<ItemStackRecord> {
                        new ItemStackRecord { itemId = "material.wood", count = structure.woodStored }
                    }, 12);
                migrated.structures.Add(placed);
            }
            return migrated;
        }

        static List<ItemStackRecord> SplitLegacyStacks(List<ItemStackRecord> old, int capacity)
        {
            var result = new List<ItemStackRecord>();
            foreach (ItemStackRecord stack in old)
            {
                if (stack == null || stack.count < 0 ||
                    (stack.count > 0 && string.IsNullOrEmpty(stack.itemId)))
                    throw new InvalidDataException("Topaz version 1 stack is invalid.");
                int remaining = stack.count;
                while (remaining > 0)
                {
                    int count = Math.Min(remaining, 20);
                    result.Add(new ItemStackRecord { itemId = stack.itemId, count = count });
                    remaining -= count;
                    if (result.Count > capacity)
                        throw new InvalidDataException("Legacy inventory exceeds the new slot capacity; save was not changed.");
                }
            }
            return result;
        }
    }
}
