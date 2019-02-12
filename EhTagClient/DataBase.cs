using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace EhTagClient
{
    public class RecordDictionary
    {
        public RecordDictionary(Namespace ns, RepoClient repoClient)
        {
            Namespace = ns;
            FilePath = Path.Combine(repoClient.LocalPath, $"database/{Namespace.ToString().ToLower()}.md");
        }

        [JsonIgnore]
        public string FilePath { get; }

        public Namespace Namespace { get; }

        [JsonIgnore]
        public string Prefix { get; set; }

        [JsonIgnore]
        public string Suffix { get; set; }

        public int Count => MapData.Count;

        public IEnumerable<Record> Data
        {
            get
            {
                foreach (var item in MapData.Values)
                {
                    yield return RawData[item];
                }
            }
        }

        [JsonIgnore]
        public List<Record> RawData { get; } = new List<Record>();

        private Dictionary<string, int> MapData { get; } = new Dictionary<string, int>();

        public void Load()
        {
            RawData.Clear();

            var state = 0;
            var prefix = new StringBuilder();
            var suffix = new StringBuilder();

            using (var sr = new StreamReader(FilePath))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var record = Record.TryParse(line);

                    switch (state)
                    {
                    case 0:
                        prefix.AppendLine(line);
                        if (record is null)
                            continue;
                        else
                        {
                            state = 1;
                            continue;
                        }
                    case 1:
                        prefix.AppendLine(line);
                        if (record is null)
                        {
                            state = 0;
                            continue;
                        }
                        else
                        {
                            state = 2;
                            continue;
                        }
                    case 2:
                        if (record is null)
                        {
                            suffix.AppendLine(line);
                            state = 3;
                            continue;
                        }
                        else
                        {
                            RawData.Add(record);
                            continue;
                        }
                    default:
                        suffix.AppendLine(line);
                        continue;
                    }
                }
            }

            Prefix = prefix.ToString();
            Suffix = suffix.ToString();

            MapData.Clear();

            var i = 0;
            foreach (var item in RawData)
            {
                if (!string.IsNullOrWhiteSpace(item.Raw))
                    MapData[item.Raw] = i;
                i++;
            }
        }

        public void Save()
        {
            using (var sw = new StreamWriter(FilePath))
            {
                sw.Write(Prefix);
                foreach (var item in RawData)
                {
                    if (item is null)
                        continue;
                    sw.WriteLine(item);
                }
                sw.Write(Suffix);
            }
        }

        public Record Find(string key)
        {
            if (!MapData.TryGetValue(key, out var index))
                return null;
            return RawData[index];
        }

        public Record AddOrReplace(Record record)
        {
            if (record is null)
                throw new ArgumentNullException(nameof(record));

            var key = record.Raw;
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Invalied record." + nameof(Record.Raw));

            if (MapData.TryGetValue(key, out var index))
            {
                var old = RawData[index];
                RawData[index] = record;
                return old;
            }
            else
            {
                MapData.Add(key, RawData.Count);
                RawData.Add(record);
                return null;
            }
        }

        public bool Remove(string key)
        {
            if (!MapData.TryGetValue(key, out var index))
                return false;
            MapData.Remove(key);
            RawData[index] = null;

            return true;
        }
    }

    public class Database : IReadOnlyDictionary<Namespace, RecordDictionary>
    {
        public Database(RepoClient repoClient)
        {
            _RepoClient = repoClient;
            _Keys = (Namespace[])Enum.GetValues(typeof(Namespace));
            Array.Sort(_Keys);
            _Values = new RecordDictionary[_Keys.Length];
            for (var i = 0; i < _Keys.Length; i++)
            {
                _Values[i] = new RecordDictionary(_Keys[i], repoClient);
            }
            Load();
        }

        private readonly Namespace[] _Keys;
        private readonly RecordDictionary[] _Values;
        private readonly RepoClient _RepoClient;

        public void Load()
        {
            foreach (var item in Values)
            {
                item.Load();
            }
        }

        public void Save()
        {
            foreach (var item in Values)
            {
                item.Save();
            }
        }

        public int GetVersion()
        {
            var path = Path.Combine(_RepoClient.LocalPath, $"version");
            if (!int.TryParse(File.ReadAllText(path), out var ver))
                return -1;
            return ver;
        }

        public IEnumerable<Namespace> Keys => _Keys;
        public IEnumerable<RecordDictionary> Values => _Values;
        public int Count => _Keys.Length;

        public RecordDictionary this[Namespace key]
        {
            get
            {
                var i = Array.BinarySearch(_Keys, key);
                if (i < 0)
                    throw new KeyNotFoundException();
                return _Values[i];
            }
        }

        public bool ContainsKey(Namespace key) => Array.BinarySearch(_Keys, key) >= 0;
        public bool TryGetValue(Namespace key, out RecordDictionary value)
        {
            var i = Array.BinarySearch(_Keys, key);
            if (i < 0)
            {
                value = default;
                return false;
            }
            value = _Values[i];
            return true;
        }
        public IEnumerator<KeyValuePair<Namespace, RecordDictionary>> GetEnumerator()
        {
            for (var i = 0; i < _Keys.Length; i++)
            {
                yield return KeyValuePair.Create(_Keys[i], _Values[i]);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
