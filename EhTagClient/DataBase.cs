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
        public RecordDictionary(Namespace ns)
        {
            Namespace = ns;
        }

        [JsonIgnore]
        public string FilePath => $"{RepositoryClient.REPO_PATH}/database/{Namespace.ToString().ToLower()}.md";

        public Namespace Namespace { get; }

        [JsonIgnore]
        public string Prefix { get; set; }

        [JsonIgnore]
        public string Suffix { get; set; }

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
                            this.RawData.Add(record);
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
                if (!string.IsNullOrWhiteSpace(item.Original))
                    MapData[item.Original] = i;
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

        public int Count => this.MapData.Count;

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

            var key = record.Original;
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Invalied record.Original");

            if (MapData.TryGetValue(key, out var index))
            {
                var old = this.RawData[index];
                this.RawData[index] = record;
                return old;
            }
            else
            {
                this.MapData.Add(key, this.RawData.Count);
                this.RawData.Add(record);
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
        public Database()
        {
            var keys = Enum.GetValues(typeof(Namespace)).Cast<Namespace>().ToList();
            keys.Remove(Namespace.Unknown);
            Keys = keys.AsReadOnly();

            Values = new ReadOnlyCollection<RecordDictionary>(keys.Select(k => this[k]).ToArray());
        }

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

        public RecordDictionary Reclass { get; } = new RecordDictionary(Namespace.Reclass);
        public RecordDictionary Language { get; } = new RecordDictionary(Namespace.Language);
        public RecordDictionary Parody { get; } = new RecordDictionary(Namespace.Parody);
        public RecordDictionary Character { get; } = new RecordDictionary(Namespace.Character);
        public RecordDictionary Group { get; } = new RecordDictionary(Namespace.Group);
        public RecordDictionary Artist { get; } = new RecordDictionary(Namespace.Artist);
        public RecordDictionary Male { get; } = new RecordDictionary(Namespace.Male);
        public RecordDictionary Female { get; } = new RecordDictionary(Namespace.Female);
        public RecordDictionary Misc { get; } = new RecordDictionary(Namespace.Misc);

        public int GetVersion()
        {
            if (!int.TryParse(File.ReadAllText(RepositoryClient.REPO_PATH + "/version"), out var ver))
                return -1;
            return ver;
        }

        public IEnumerable<Namespace> Keys { get; }
        public IEnumerable<RecordDictionary> Values { get; }
        public int Count => Keys.Count();

        public RecordDictionary this[Namespace key]
        {
            get
            {
                switch (key)
                {
                case Namespace.Reclass: return Reclass;
                case Namespace.Language: return Language;
                case Namespace.Parody: return Parody;
                case Namespace.Character: return Character;
                case Namespace.Group: return Group;
                case Namespace.Artist: return Artist;
                case Namespace.Male: return Male;
                case Namespace.Female: return Female;
                case Namespace.Misc: return Misc;
                default:
                    throw new KeyNotFoundException();
                }
            }
        }

        public bool ContainsKey(Namespace key) => Keys.Contains(key);
        public bool TryGetValue(Namespace key, out RecordDictionary value)
        {
            if (Keys.Contains(key))
            {
                value = this[key];
                return true;
            }

            value = default;
            return false;
        }
        public IEnumerator<KeyValuePair<Namespace, RecordDictionary>> GetEnumerator()
        {
            foreach (var key in Keys)
            {
                yield return new KeyValuePair<Namespace, RecordDictionary>(key, this[key]);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
