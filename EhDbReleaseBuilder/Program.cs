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
            var client = new GitHubApiClient(args[0], args[1]);
            var s = Consts.SerializerSettings;
            client.Publish(s, "full");
            var c = new MdConverter();
            s.Converters.Add(c);
            c.Type = MdConverter.ConvertType.Html;
            client.Publish(s, "html");
            c.Type = MdConverter.ConvertType.Raw;
            client.Publish(s, "raw");
            c.Type = MdConverter.ConvertType.Text;
            client.Publish(s, "text");
            c.Type = MdConverter.ConvertType.Ast;
            client.Publish(s, "ast");
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

        public void Publish(JsonSerializerSettings settings, string fileModifier)
        {
            var serializer = JsonSerializer.Create(settings);
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
                serializer.Serialize(writer, uploadData);
                writer.Flush();

                using (var json = File.OpenWrite(Path.Combine(_Target, $"db.{fileModifier}.json")))
                {
                    writer.BaseStream.Position = 0;
                    writer.BaseStream.CopyTo(json);
                    Console.WriteLine($"Created: db.{fileModifier}.json ({json.Position} bytes)");
                }

                using (var gziped = File.OpenWrite(Path.Combine(_Target, $"db.{fileModifier}.json.gz")))
                {
                    using (var gzip = new GZipStream(gziped, CompressionLevel.Optimal, true))
                    {
                        writer.BaseStream.Position = 0;
                        writer.BaseStream.CopyTo(gzip);
                        Console.WriteLine($"Created: db.{fileModifier}.json.gz ({gziped.Position} bytes)");
                    }
                }

                using (var jsonp = File.OpenWrite(Path.Combine(_Target, $"db.{fileModifier}.js")))
                {
                    jsonp.Write(encoding.GetBytes("load_ehtagtranslation_database("));
                    writer.BaseStream.Position = 0;
                    writer.BaseStream.CopyTo(jsonp);
                    jsonp.Write(encoding.GetBytes(");"));
                    Console.WriteLine($"Created: db.{fileModifier}.js ({jsonp.Position} bytes)");
                }
            }
        }
    }
}
