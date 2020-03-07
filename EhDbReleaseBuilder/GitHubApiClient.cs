using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EhTagClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EhDbReleaseBuilder
{
    class GitHubApiClient
    {
        public GitHubApiClient(string source, string target)
        {
            _Target = target;
            _RepoClient = new RepoClient(source);
            _Database = new Database(_RepoClient);
        }

        private readonly RepoClient _RepoClient;
        private readonly Database _Database;
        private readonly string _Target;
        private static readonly Encoding _Encoding = new UTF8Encoding(false);

        private class UploadData
        {
            public class Signature
            {
                public string Name { get; set; }
                public string Email { get; set; }
                public DateTimeOffset When { get; set; }

                public static implicit operator LibGit2Sharp.Signature(Signature v) => new LibGit2Sharp.Signature(v.Name, v.Email, v.When);
                public static implicit operator Signature(LibGit2Sharp.Signature v) => new Signature { Name = v.Name, Email = v.Email, When = v.When };
            }

            public class HeadData
            {
                public Signature Author { get; set; }
                public Signature Committer { get; set; }
                public string Sha { get; set; }
                public string Message { get; set; }
            }
            public string Repo { get; set; }
            public JRaw Data { get; set; }
        }

        public Task CheckAsync(Namespace checkTags) => TagChecker.CheckAsync(_Database, checkTags);

        private class FullUploadData : UploadData
        {
            public HeadData Head { get; set; }
            public int Version { get; set; }
        };

        public void Normalize()
        {
            _Database.Save();
        }

        public void Publish()
        {
            foreach (var item in Directory.CreateDirectory(_Target).GetFiles())
            {
                item.Delete();
            }

            _PublishOne("full");
            _PublishOne("html");
            _PublishOne("raw");
            _PublishOne("text");
            _PublishOne("ast");

            var message = $"{_RepoClient.Head.Sha}";
            if (Environment.GetEnvironmentVariable("GITHUB_ACTION") != null)
            {
                Directory.CreateDirectory(Path.Join(_Target, ".github"));
                File.WriteAllText(Path.Join(_Target, ".github", "message.md"), message);
            }
        }

        private void _PublishOne(string mid)
        {
            var fulldata = _Serialize(mid);
            var byteData = _Encoding.GetBytes(fulldata);
            _WriteFile(byteData, "db", mid, "json");
            _WriteFile(byteData, "db", mid, "json.gz");
            _WriteFile(byteData, "db", mid, "js");
        }

        private string _Serialize(string mid)
        {
            var settings = Consts.SerializerSettings;
            switch (mid)
            {
                case "full":
                    break;
                case "html":
                    settings.Converters.Add(new MdConverter(MdConverter.ConvertType.Html));
                    break;
                case "raw":
                    settings.Converters.Add(new MdConverter(MdConverter.ConvertType.Raw));
                    break;
                case "text":
                    settings.Converters.Add(new MdConverter(MdConverter.ConvertType.Text));
                    break;
                case "ast":
                    settings.Converters.Add(new MdConverter(MdConverter.ConvertType.Ast));
                    break;
                default:
                    throw new ArgumentException("Unsupported mid.");
            }
            var newHead = new UploadData.HeadData
            {
                Author = _RepoClient.Head.Author,
                Committer = _RepoClient.Head.Committer,
                Sha = _RepoClient.Head.Sha,
                Message = _RepoClient.Head.Message,
            };
            var newdataStr = JsonConvert.SerializeObject(_Database.Values, settings);
            var fullUpData = new FullUploadData
            {
                Repo = _RepoClient.RemotePath,
                Version = _Database.GetVersion(),
                Head = newHead,
                Data = new JRaw(newdataStr),
            };
            return JsonConvert.SerializeObject(fullUpData, settings);
        }

        private void _WriteFile(byte[] jsonData, string pre, string mid, string suf)
        {
            switch (suf)
            {
                case "json":
                    using (var json = File.OpenWrite(Path.Combine(_Target, $"{pre}.{mid}.json")))
                    {
                        json.Write(jsonData);
                        json.Flush();
                        Console.WriteLine($"Created: {pre}.{mid}.json ({json.Position} bytes)");
                        return;
                    }
                case "json.gz":
                    using (var gziped = File.OpenWrite(Path.Combine(_Target, $"{pre}.{mid}.json.gz")))
                    using (var gzip = new GZipStream(gziped, CompressionLevel.Optimal, true))
                    {
                        gzip.Write(jsonData);
                        gzip.Flush();
                        gziped.Flush();
                        Console.WriteLine($"Created: {pre}.{mid}.json.gz ({gziped.Position} bytes)");
                        return;
                    }
                case "js":
                    using (var jsonp = File.OpenWrite(Path.Combine(_Target, $"{pre}.{mid}.js")))
                    {
                        if (pre == "diff")
                        {
                            jsonp.Write(_Encoding.GetBytes($"load_ehtagtranslation_{pre}_{mid}("));
                            jsonp.Write(jsonData);
                            jsonp.Write(_Encoding.GetBytes(");"));
                        }
                        else
                        {
                            jsonp.Write(_Encoding.GetBytes($"(function(){{var d={{c:'load_ehtagtranslation_{pre}_{mid}',d:'"));
                            using (var gziped = new MemoryStream())
                            using (var gzip = new GZipStream(gziped, CompressionLevel.Optimal, true))
                            {
                                gzip.Write(jsonData);
                                gzip.Flush();
                                gziped.Flush();
                                jsonp.Write(_Encoding.GetBytes(Convert.ToBase64String(gziped.GetBuffer(), 0, (int)gziped.Length, Base64FormattingOptions.None)));
                            }
                            jsonp.Write(_Encoding.GetBytes($"'}};"));
                            using (var pako = Assembly.GetExecutingAssembly().GetManifestResourceStream("EhDbReleaseBuilder.pako.min.js"))
                            {
                                pako.CopyTo(jsonp);
                            }
                            jsonp.Write(_Encoding.GetBytes("})();"));
                        }
                        jsonp.Flush();
                        Console.WriteLine($"Created: {pre}.{mid}.js ({jsonp.Position} bytes)");
                        return;
                    }
                default:
                    throw new ArgumentException("Unsupported suffix.");
            }
        }
    }
}
