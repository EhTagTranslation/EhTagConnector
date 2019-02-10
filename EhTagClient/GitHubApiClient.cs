using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EhTagClient
{
    public class GitHubApiClient
    {
        public GitHubApiClient(RepoClient repoClient, Database database)
        {
            this.repoClient = repoClient;
            this.database = database;
        }

        private readonly HttpClient _HttpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://api.github.com/"),
        };
        private readonly RepoClient repoClient;
        private readonly Database database;

        private HttpClient HttpClient
        {
            get
            {
                var headers = _HttpClient.DefaultRequestHeaders;
                headers.Authorization = new AuthenticationHeaderValue("token", Consts.Token);
                headers.UserAgent.Clear();
                headers.UserAgent.Add(new ProductInfoHeaderValue(Consts.Username, "1.0"));
                return _HttpClient;
            }
        }

        public async Task Publish()
        {
            var payload = new
            {
                tag_name = $"commit-{repoClient.CurrentSha}",
                target_commitish = repoClient.CurrentSha,
                name = $"EhTagConnector Auto Release of {repoClient.CurrentSha}",
            };
            var create_response = JsonConvert.DeserializeObject<dynamic>(await (await HttpClient.PostAsync(
                $"/repos/{Consts.OWNER}/{Consts.REPO}/releases", 
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                )).Content.ReadAsStringAsync());
            var upload_url = (string)create_response.upload_url.Value;
            upload_url = upload_url.Replace("{?name,label}", $"?name=db.json");

            var head = repoClient.Head;
            var upload_data = new
            {
                Remote = repoClient.RemotePath,
                Head = new
                {
                    head.Author,
                    head.Committer,
                    head.Sha,
                    head.Message,
                },
                Version = database.GetVersion(),
                Data = database.Values,
            };

            await HttpClient.PostAsync(upload_url, new StringContent(JsonConvert.SerializeObject(upload_data), Encoding.UTF8, "application/json"));
        }
    }
}
