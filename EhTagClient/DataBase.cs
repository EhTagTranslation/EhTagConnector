using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace EhTagClient
{
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
#if DEBUG
            Save();
#endif
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

        public void Render()
        {
            Context.Database = this;
            foreach (var item in Values)
            {
                item.Render();
            }
        }

        public void Save()
        {
            Context.Database = this;
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
