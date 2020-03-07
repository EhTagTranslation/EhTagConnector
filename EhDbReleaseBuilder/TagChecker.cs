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

        public static async Task<(Namespace, string)> CheckAsync(Namespace ns, string raw)
        {
            if (raw.Length <= 2)
                return (ns, raw);

            var match = _FindCache(ns, raw);

            if (match is null)
            {
                var response = await _HttpClient.PostAsync("https://api.e-hentai.org/api.php", new StringContent(JsonConvert.SerializeObject(new
                {
                    method = "tagsuggest",
                    text = $"{(raw.Length > 50 ? raw.Substring(0, 50) : raw)}",
                })));
                var resultStr = await response.Content.ReadAsStringAsync();
                if (!resultStr.Contains("{\"tags\":[]}"))
                {
                    var result = JsonConvert.DeserializeObject<TagSuggest>(resultStr);
                    _FillCache(result);
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
    }
}
