using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Shift.Progression
{
    /// <summary>
    /// Personal bests, persisted as JSON next to the player's other Unity data.
    /// </summary>
    /// <remarks>
    /// A file rather than PlayerPrefs: on Windows PlayerPrefs is the registry, which is invisible,
    /// un-diffable, and impossible to copy between machines — all wrong for a game whose pillar is
    /// speedrunning. A record is also structured (id + N splits + timestamp), which PlayerPrefs
    /// would force into flattened string keys.
    ///
    /// This type is a WRITE-ONLY SINK as far as gameplay is concerned. Nothing may ever read it to
    /// decide world state, or a player with an edited file changes the world — which matters the
    /// moment this game is host-authoritative.
    /// </remarks>
    public class RunRecordStore
    {
        private const string FileName = "records.json";

        private readonly string _path;
        private readonly List<RunRecord> _records = new List<RunRecord>();
        private bool _loaded;

        public RunRecordStore(string directory = null)
        {
            string folder = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
            _path = System.IO.Path.Combine(folder, FileName);
        }

        public string Path => _path;

        public RunRecord GetBest(string zoneId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(zoneId)) return null;

            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].ZoneId == zoneId) return _records[i];
            }

            return null;
        }

        /// <summary>Stores <paramref name="record"/> if it beats the existing best. True when it did.</summary>
        public bool Submit(RunRecord record)
        {
            EnsureLoaded();
            if (record == null || string.IsNullOrEmpty(record.ZoneId)) return false;

            RunRecord existing = GetBest(record.ZoneId);
            if (existing != null && existing.TotalSeconds <= record.TotalSeconds) return false;

            if (existing != null) _records.Remove(existing);
            _records.Add(record);

            Save();
            return true;
        }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            // A missing or corrupt file must mean "no personal bests", never an exception on
            // startup — the records are a nicety, the game is not.
            try
            {
                if (!File.Exists(_path)) return;

                RunRecordFile file = JsonUtility.FromJson<RunRecordFile>(File.ReadAllText(_path));
                if (file?.Records == null) return;

                for (int i = 0; i < file.Records.Length; i++)
                {
                    if (file.Records[i] != null) _records.Add(file.Records[i]);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not read {_path}, starting with no records. {exception.Message}");
                _records.Clear();
            }
        }

        private void Save()
        {
            RunRecordFile file = new RunRecordFile { Version = 1, Records = _records.ToArray() };
            string json = JsonUtility.ToJson(file, true);
            string temporary = _path + ".tmp";

            // Written aside then swapped, so an Alt-F4 mid-write cannot cost every PB in the file.
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path));
                File.WriteAllText(temporary, json);

                if (File.Exists(_path)) File.Replace(temporary, _path, null);
                else File.Move(temporary, _path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not write {_path}. {exception.Message}");
            }
        }
    }
}
