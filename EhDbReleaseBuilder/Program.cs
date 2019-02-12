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
        public static void Main(string[] args)
        {
            Console.WriteLine($@"EhDbReleaseBuilder started.
  Source: {args[0]}
  Target: {args[1]}
");
            new GitHubApiClient(args[0], args[1]).Publish();
        }
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
            var encoding = new UTF8Encoding(false);
            using (var writer = new StreamWriter(new MemoryStream(), encoding))
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

                using (var json = File.OpenWrite(Path.Combine(_Target, "db.json")))
                {
                    writer.BaseStream.Position = 0;
                    writer.BaseStream.CopyTo(json);
                    Console.WriteLine($"Created: db.json ({json.Position} bytes)");
                }

                using (var gziped = File.OpenWrite(Path.Combine(_Target, "db.json.gz")))
                {
                    using (var gzip = new GZipStream(gziped, CompressionLevel.Optimal, true))
                    {
                        writer.BaseStream.Position = 0;
                        writer.BaseStream.CopyTo(gzip);
                        Console.WriteLine($"Created: db.json.gz ({gziped.Position} bytes)");
                    }
                }

                using (var jsonp = File.OpenWrite(Path.Combine(_Target, "db.js")))
                {
                    jsonp.Write(encoding.GetBytes("load_ehtagtranslation_database("));
                    writer.BaseStream.Position = 0;
                    writer.BaseStream.CopyTo(jsonp);
                    jsonp.Write(encoding.GetBytes(");"));
                    Console.WriteLine($"Created: db.js ({jsonp.Position} bytes)");
                }
            }
        }
    }
}
