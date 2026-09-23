using System;
using System.IO;
using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>One versioned local slot, with an atomic replacement and backup.</summary>
    public sealed class SaveRepository
    {
        readonly string _path;

        public SaveRepository(string directory)
        {
            _path = Path.Combine(directory, "topaz-save.json");
        }

        public string PathOnDisk => _path;

        public TopazSaveData Load()
        {
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
            if (data == null || data.version != TopazSaveData.CurrentVersion)
                throw new InvalidDataException("Cannot write an unsupported Topaz save version.");

            string directory = Path.GetDirectoryName(_path);
            Directory.CreateDirectory(directory);
            string temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            if (File.Exists(_path)) File.Replace(temporary, _path, _path + ".bak");
            else File.Move(temporary, _path);
        }

        static TopazSaveData Parse(string json)
        {
            TopazSaveData data = JsonUtility.FromJson<TopazSaveData>(json);
            if (data == null || data.version != TopazSaveData.CurrentVersion ||
                data.day < 1 || data.backpack == null || data.nodes == null || data.structures == null)
                throw new InvalidDataException("Topaz save format or version is invalid.");
            return data;
        }
    }
}
