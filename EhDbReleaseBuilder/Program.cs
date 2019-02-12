using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using EhTagClient;
using Newtonsoft.Json;
using Octokit;

namespace EhDbReleaseBuilder
{
    class Program
    {
        public static void Main(string[] args) => new GitHubApiClient(args[0], args[1]).Publish();
    }

    class GitHubApiClient
    {
        public GitHubApiClient(string source, string target)
        {
            _Target = target;
            _RepoClient = new RepoClient(source);
            _Database = new Database(_RepoClient);
            _JsonSerializer = JsonSerializer.Create(Consts.SerializerSettings);
        }

        private readonly RepoClient _RepoClient;
        private readonly Database _Database;
        private readonly JsonSerializer _JsonSerializer;
        private string _Target;

        public void Publish()
        {
            Directory.CreateDirectory(_Target);
            using (var writer = new StreamWriter(new MemoryStream(), new UTF8Encoding(false)))
            {
                var head = _RepoClient.Head;
                var uploadData = new
                {
                    Remote = _RepoClient.RemotePath,
                    Head = new
                    {
                        head.Author,
                        head.Committer,
                        head.Sha,
                        head.Message,
                    },
                    Version = _Database.GetVersion(),
                    Data = _Database.Values,
                };
                _JsonSerializer.Serialize(writer, uploadData);
                writer.Flush();

                using (var gziped = File.OpenWrite(Path.Combine(_Target, "db.json.gz")))
                {
                    using (var gzip = new GZipStream(gziped, CompressionLevel.Optimal, true))
                    {
                        writer.BaseStream.Position = 0;
                        writer.BaseStream.CopyTo(gzip);
                    }
                }


                using (var json = File.OpenWrite(Path.Combine(_Target, "db.json")))
                {
                    writer.BaseStream.Position = 0;
                    writer.BaseStream.CopyTo(json);
                }


                using (var jsonp = File.OpenWrite(Path.Combine(_Target, "db.js")))
                {
                    jsonp.Write(Encoding.UTF8.GetBytes("load_ehtagtranslation_database("));
                    writer.BaseStream.Position = 0;
                    writer.BaseStream.CopyTo(jsonp);
                    jsonp.Write(Encoding.UTF8.GetBytes(");"));
                }
            }
        }
    }
}
