using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KVP = System.Collections.Generic.KeyValuePair<string, EhTagClient.Record>;

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

        public struct DataDic : IReadOnlyDictionary<string, Record>
        {
            private readonly RecordDictionary _Parent;

            internal DataDic(RecordDictionary parent) => _Parent = parent;

            public Record this[string key] =>_Parent.Find(key);

            public IEnumerable<string> Keys
            {
                get
                {
                    foreach (var item in _Parent.MapData.Keys)
                    {
                        yield return item;
                    }
                }
            }
            public IEnumerable<Record> Values
            {
                get
                {
                    foreach (var item in Keys)
                    {
                        yield return this[item];
                    }
                }
            }
            public int Count => _Parent.Count;

            public bool ContainsKey(string key) => _Parent.MapData.ContainsKey(key);

            public bool TryGetValue(string key, out Record value)
            {
                value = _Parent.Find(key);
                return !(value is null);

            }

            public IEnumerator<KVP> GetEnumerator()
            {
                foreach (var item in _Parent.MapData.Values)
                {
                    yield return _Parent.RawData[item];
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public DataDic Data => new DataDic(this);

        [JsonIgnore]
        public List<KVP> RawData { get; } = new List<KVP>();

        private Dictionary<string, int> MapData { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

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
                        if (record.Key is null)
                            continue;
                        else
                        {
                            state = 1;
                            continue;
                        }
                    case 1:
                        prefix.AppendLine(line);
                        if (record.Key is null)
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
                        if (record.Key is null)
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
                if (!string.IsNullOrWhiteSpace(item.Key))
                    MapData[item.Key] = i;
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
                    if (item.Key is null)
                        continue;
                    sw.WriteLine(item.Value.ToString(item.Key));
                }
                sw.Write(Suffix);
            }
        }

        public Record Find(string key)
        {
            if (!MapData.TryGetValue(key, out var index))
                return null;
            return RawData[index].Value;
        }

        public Record AddOrReplace(string key, Record record)
        {
            key = key?.Trim();
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (record is null)
                throw new ArgumentNullException(nameof(record));

            if (MapData.TryGetValue(key, out var index))
            {
                var old = RawData[index];
                RawData[index] = KeyValuePair.Create(key, record);
                return old.Value;
            }
            else
            {
                MapData.Add(key, RawData.Count);
                RawData.Add(KeyValuePair.Create(key, record));
                return null;
            }
        }

        public bool Remove(string key)
        {
            if (!MapData.TryGetValue(key, out var index))
                return false;
            MapData.Remove(key);
            RawData[index] = default;

            return true;
        }
    }
}
