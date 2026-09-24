using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>Atomic collection snapshots keep Character and World transfers together.</summary>
    public sealed class ProfileRepository
    {
        readonly string _path;
        readonly string _directory;
        readonly object _gate = new object();
        Task _worker = Task.CompletedTask;
        string _pendingJson;
        Exception _backgroundError;
        bool _working;

        public ProfileRepository(string directory)
        {
            _directory = directory;
            _path = Path.Combine(directory, "topaz-collection.json");
        }

        public string PathOnDisk => _path;
        public Exception BackgroundError
        {
            get { lock (_gate) return _backgroundError; }
        }

        public TopazProfileData Load()
        {
            Flush();
            if (File.Exists(_path) || File.Exists(_path + ".bak"))
            {
                try { return Parse(File.ReadAllText(_path)); }
                catch (Exception primaryError)
                {
                    try
                    {
                        TopazProfileData recovered = Parse(File.ReadAllText(_path + ".bak"));
                        if (File.Exists(_path))
                        {
                            string temporary = _path + ".recovery.tmp";
                            File.Copy(_path + ".bak", temporary, true);
                            File.Replace(temporary, _path,
                                _path + ".corrupt-" + Guid.NewGuid().ToString("N"));
                        }
                        return recovered;
                    }
                    catch (Exception backupError)
                    {
                        throw new InvalidDataException("Character and World collection is unreadable.",
                            new AggregateException(primaryError, backupError));
                    }
                }
            }

            var legacy = new SaveRepository(_directory);
            if (!File.Exists(legacy.PathOnDisk) && !File.Exists(legacy.PathOnDisk + ".bak"))
                return new TopazProfileData();
            TopazProfileData migrated = TopazProfileData.FromLegacy(legacy.Load());
            Save(migrated); // The old file remains untouched for recovery.
            return migrated;
        }

        public void Save(TopazProfileData data)
        {
            Flush();
            WriteJson(Serialize(data));
        }

        public void QueueSave(TopazProfileData data)
        {
            string snapshot = Serialize(data); // JsonUtility stays on Unity's main thread.
            lock (_gate)
            {
                if (_backgroundError != null)
                    throw new IOException("Background collection write failed.", _backgroundError);
                _pendingJson = snapshot;
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
            if (error != null) throw new IOException("Background collection write failed.", error);
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

        static string Serialize(TopazProfileData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.Validate();
            return JsonUtility.ToJson(data, true);
        }

        static TopazProfileData Parse(string json)
        {
            TopazProfileData data = JsonUtility.FromJson<TopazProfileData>(json);
            if (data == null) throw new InvalidDataException("Collection is empty.");
            data.Validate();
            return data;
        }

        void WriteJson(string json)
        {
            Directory.CreateDirectory(_directory);
            string temporary = _path + ".tmp";
            File.WriteAllText(temporary, json);
            if (File.Exists(_path)) File.Replace(temporary, _path, _path + ".bak");
            else File.Move(temporary, _path);
        }
    }
}
