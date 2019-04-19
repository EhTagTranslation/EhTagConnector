﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EhTagClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EhDbReleaseBuilder
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine($@"EhDbReleaseBuilder started.
  Source: {args[0]}
  Target: {args[1]}
");
            var client = new GitHubApiClient(args[0], args[1]);
            client.Normalize();
            await client.Publish();
        }
    }

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
        private static readonly JsonDiffPatch.JsonDiffer _JsonDiffer = new JsonDiffPatch.JsonDiffer();
        private static readonly HttpClient _HttpClient = new HttpClient();

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
            public string Remote { get; set; }
            public JRaw Data { get; set; }
        }

        private class FullUploadData : UploadData
        {
            public HeadData Head { get; set; }
            public int Version { get; set; }
        };

        private class PatchUploadData : UploadData
        {
            public HeadData OldHead { get; set; }
            public int OldVersion { get; set; }
            public HeadData NewHead { get; set; }
            public int NewVersion { get; set; }
        }

        public void Normalize()
        {
            _Database.Save();
        }

        public async Task Publish()
        {
            foreach (var item in Directory.CreateDirectory(_Target).GetFiles())
            {
                item.Delete();
            }
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("EhDbReleaseBuilder"))
            {
                // Credentials = new Octokit.Credentials(_GithubToken)
            };
            var release = await client.Repository.Release.GetLatest("EhTagTranslation", "Database");

            await _PublishOne(release, "full");
            await _PublishOne(release, "html");
            await _PublishOne(release, "raw");
            await _PublishOne(release, "text");
            await _PublishOne(release, "ast");

            Process.Start("appveyor", $"SetVariable -Name GITHUB_RELEASE_MESSAGE -Value {_RepoClient.Head.Sha}...{release.TargetCommitish}").WaitForExit();
        }

        private async Task _PublishOne(Octokit.Release oldRelease, string mid)
        {
            var downName = $"db.{mid}.json.gz";
            var uri = oldRelease.Assets.First(a => a.Name == downName).BrowserDownloadUrl;
            Console.WriteLine($"Downloading old {downName}");
            var res = await _HttpClient.GetAsync(uri);
            using (var stream = new GZipStream(await res.Content.ReadAsStreamAsync(), CompressionMode.Decompress, false))
            using (var reader = new StreamReader(stream, _Encoding))
            {
                var oldData = JsonConvert.DeserializeObject<FullUploadData>(reader.ReadToEnd());
                Console.WriteLine($"Downloaded old {downName}");

                var (fulldata, patchdata) = _Serialize(oldData, mid);
                {
                    var byteData = _Encoding.GetBytes(fulldata);
                    _WriteFile(byteData, "db", mid, "json");
                    _WriteFile(byteData, "db", mid, "json.gz");
                    _WriteFile(byteData, "db", mid, "js");
                }
                {
                    var byteData = _Encoding.GetBytes(patchdata);
                    _WriteFile(byteData, "diff", mid, "json");
                    _WriteFile(byteData, "diff", mid, "json.gz");
                    _WriteFile(byteData, "diff", mid, "js");
                }
            }
        }

        private (string full, string patch) _Serialize(FullUploadData old, string mid)
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
            var olddataStr = old.Data.ToString();
            var fullUpData = new FullUploadData
            {
                Remote = _RepoClient.RemotePath,
                Version = _Database.GetVersion(),
                Head = newHead,
                Data = new JRaw(newdataStr),
            };
            var patchUpData = new PatchUploadData
            {
                Remote = _RepoClient.RemotePath,
                NewVersion = _Database.GetVersion(),
                OldVersion = old.Version,
                NewHead = newHead,
                OldHead = old.Head,
                Data = new JRaw(_JsonDiffer.Diff(JToken.Parse(olddataStr), JToken.Parse(newdataStr), false).ToString(settings.Formatting)),
            };
            return (JsonConvert.SerializeObject(fullUpData, settings), JsonConvert.SerializeObject(patchUpData, settings));
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
                {
                    using (var gzip = new GZipStream(gziped, CompressionLevel.Optimal, true))
                    {
                        gzip.Write(jsonData);
                        gzip.Flush();
                        gziped.Flush();
                        Console.WriteLine($"Created: {pre}.{mid}.json.gz ({gziped.Position} bytes)");
                        return;
                    }
                }
            case "js":
                using (var jsonp = File.OpenWrite(Path.Combine(_Target, $"{pre}.{mid}.js")))
                {
                    jsonp.Write(_Encoding.GetBytes($"load_ehtagtranslation_{pre}_{mid}("));
                    jsonp.Write(jsonData);
                    jsonp.Write(_Encoding.GetBytes(");"));
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
