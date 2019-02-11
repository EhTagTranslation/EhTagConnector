﻿using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EhTagClient
{
    public class GitHubApiClient
    {
        public GitHubApiClient(RepoClient repoClient, Database database, JsonSerializer jsonSerializer)
        {
            _RepoClient = repoClient;
            _Database = database;
            _JsonSerializer = jsonSerializer;
        }

        private readonly RepoClient _RepoClient;
        private readonly Database _Database;
        private readonly JsonSerializer _JsonSerializer;
        private readonly GitHubClient _GitHubClient = new GitHubClient(new Octokit.ProductHeaderValue(Consts.Username, "1.0"))
        {
            Credentials = new Credentials(Consts.Token),
        };

        public async Task Publish()
        {
            var releaseClient = _GitHubClient.Repository.Release;

            var release = await releaseClient.Create(Consts.OWNER, Consts.REPO, new NewRelease($"commit-{_RepoClient.CurrentSha}")
            {
                TargetCommitish = _RepoClient.CurrentSha,
                Name = $"EhTagConnector Auto Release of {_RepoClient.CurrentSha.Substring(0, 7)}",
            });

            using (var writer = new StreamWriter(new MemoryStream(), Encoding.UTF8))
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

                using (var gziped = new MemoryStream())
                {
                    using (var gzip = new GZipStream(gziped, CompressionLevel.Optimal, true))
                    {
                        writer.BaseStream.Position = 0;
                        writer.BaseStream.CopyTo(gzip);
                    }
                    gziped.Position = 0;
                    await releaseClient.UploadAsset(release, new ReleaseAssetUpload("db.json.gz", "application/json+gzip", gziped, null));
                }

                writer.BaseStream.Position = 0;
                await releaseClient.UploadAsset(release, new ReleaseAssetUpload("db.json", "application/json", writer.BaseStream, null));
            }
        }
    }
}
