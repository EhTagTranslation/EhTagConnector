using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EhTagClient;
using Newtonsoft.Json;

namespace EhDbReleaseBuilder
{
    internal static class TagChecker
    {
        private static readonly Dictionary<(Namespace ns, string raw), Tag> _TagCache = new Dictionary<(Namespace ns, string raw), Tag>();

        private class TagSuggest
        {
            public Dictionary<int, Tag> tags { get; set; }
        }

        private class Tag
        {
            public Namespace ns { get; set; }
            public string tn { get; set; }
            public int? mid { get; set; }
            public Namespace? mns { get; set; }
            public string mtn { get; set; }
        }


        private static readonly HttpClient _HttpClient = _BuildClient();

        private static HttpClient _BuildClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:74.0) Gecko/20100101 Firefox/74.0");
            return client;
        }

        private static void _FillCache(TagSuggest tagSuggest)
        {
            foreach (var item in tagSuggest.tags.Values)
            {
                _TagCache[(item.ns, item.tn)] = item;
            }
        }

        private static Tag _FindCache(Namespace ns, string raw)
        {
            _TagCache.TryGetValue((ns, raw), out var tag);
            return tag;
        }

        private static async Task<TagSuggest> _PostApiAsync(Namespace? ns, string raw)
        {
            var text = raw.Length > 50 ? raw.Substring(0, 50) : raw;
            if (ns != null)
                text = ns + ":" + raw;
            var response = await _HttpClient.PostAsync("https://api.e-hentai.org/api.php", new StringContent(JsonConvert.SerializeObject(new
            {
                method = "tagsuggest",
                text,
            })));
            var resultStr = await response.Content.ReadAsStringAsync();
            if (resultStr.Contains("{\"tags\":[]}"))
            {
                if (raw.Contains('.'))
                {
                    return await _PostApiAsync(ns, raw.Substring(0, raw.IndexOf('.') - 1));
                }
                else
                {
                    return null;
                }
            }

            var result = JsonConvert.DeserializeObject<TagSuggest>(resultStr);
            _FillCache(result);
            return result;
        }

        public static async Task<(Namespace, string)> _CheckTagAsync(Namespace ns, string raw)
        {
            if (raw.Length <= 2)
                return (ns, raw);

            var match = _FindCache(ns, raw);

            if (match is null)
            {
                var result = await _PostApiAsync(null, raw);
                if (result != null)
                {
                    // check exact match first
                    match = result.tags.Values.FirstOrDefault(tag => tag.ns == ns && tag.tn == raw)
                        // find from misc ns and master in this ns
                        ?? result.tags.Values.FirstOrDefault(tag => tag.ns == Namespace.Misc && tag.tn == raw && tag.mns == ns);
                }
            }

            if (match is null)
            {
                var result = await _PostApiAsync(ns, raw);
                if (result != null)
                {
                    // check exact match first
                    match = result.tags.Values.FirstOrDefault(tag => tag.ns == ns && tag.tn == raw)
                        // find from misc ns and master in this ns
                        ?? result.tags.Values.FirstOrDefault(tag => tag.ns == Namespace.Misc && tag.tn == raw && tag.mns == ns);
                }
            }

            if (match is null)
                return default;
            if (match.mid is null)
                return (ns, raw);
            return (match.mns ?? Namespace.Misc, match.mtn);
        }

        private static void _LogFailed(RecordDictionary db, string key, Record value, Namespace newNs, string newKey)
        {
            db.AddOrReplace(key, new Record(value.Name.Raw, value.Intro.Raw, value.Links.Raw
                + $"\nNow should be {newNs}:{newKey}"));
            Console.WriteLine(" -> Failed");
        }
        private static async Task _CheckNsAsync(Database database, Namespace ns)
        {
            var db = database[ns];
            Console.WriteLine($"Checking namespace {ns} with {db.RawData.Count} lines");
            for (var i = 0; i < db.RawData.Count; i++)
            {
                var data = db.RawData[i];
                Console.Write($"  [{i,4}/{db.RawData.Count}] {data.Key}");
                if (string.IsNullOrWhiteSpace(data.Key))
                {
                    Console.WriteLine(" -> Skipped empty tag");
                    continue;
                }
                var (newNs, newKey) = await _CheckTagAsync(ns, data.Key);
                if (newKey is null)
                {
                    db.Remove(data.Key, false);
                    Console.WriteLine(" -> Delete");
                }
                else if (newNs != ns)
                {
                    Console.Write($" -> Move to {newNs}:{newKey}");
                    try
                    {
                        database[newNs].Add(newKey, data.Value);
                        db.Remove(data.Key, true);
                        Console.WriteLine(" -> Succeed");
                    }
                    catch
                    {
                        _LogFailed(db, data.Key, data.Value, newNs, newKey);
                    }
                }
                else if (newKey != data.Key)
                {
                    Console.Write($" -> Rename to {newKey}");
                    try
                    {
                        db.Rename(data.Key, newKey);
                        Console.WriteLine(" -> Succeed");
                    }
                    catch
                    {
                        _LogFailed(db, data.Key, data.Value, newNs, newKey);
                    }
                }
                else
                {
                    Console.WriteLine(" -> Valid");
                }
            }
        }

        public static async Task CheckAsync(Database database, Namespace checkTags)
        {
            try
            {
                // skip rows & reclass
                foreach (var ns in database.Keys.Where(v => v > Namespace.Reclass && checkTags.HasFlag(v)))
                {
                    await _CheckNsAsync(database, ns);
                    database.Save();
                }
            }
            finally
            {
                database.Save();
            }
        }
    }
}
