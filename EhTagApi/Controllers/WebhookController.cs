using EhTagClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EhTagApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : Controller
    {
        private readonly ILogger logger;
        private readonly RepoClient repoClient;
        private readonly GitHubApiClient gitHubApiClient;
        private readonly Database database;

        public WebhookController(ILogger<WebhookController> logger, RepoClient repoClient, GitHubApiClient gitHubApiClient, Database database)
        {
            this.logger = logger;
            this.repoClient = repoClient;
            this.gitHubApiClient = gitHubApiClient;
            this.database = database;
        }

        [HttpPost]
        public IActionResult Post(
            [FromHeader(Name = "X-GitHub-Event"), Required] string ev,
            [FromHeader(Name = "X-GitHub-Delivery"), Required] Guid delivery,
            [FromBody, Required]dynamic payload)
        {
            if (delivery == Guid.Empty)
                return BadRequest($"Wrong X-GitHub-Delivery");
            if (string.IsNullOrWhiteSpace(ev))
                return BadRequest($"Missing X-GitHub-Event");

            if (ev == "ping")
                return NoContent();

            if (ev != "push")
                return BadRequest($"Unsupported X-GitHub-Event");

            string log;
            if (!repoClient.CurrentSha.Equals((string)payload.after.Value, StringComparison.OrdinalIgnoreCase))
            {
                var start = DateTimeOffset.Now;
                repoClient.Pull();
                log = $"Pulled form github in {(DateTimeOffset.Now - start).TotalMilliseconds}ms.";
                logger.LogInformation(log);
                database.Load();
            }
            else
            {
                log = "Already up-to-date.";
            }

            _ = gitHubApiClient.Publish();
            return Ok(log);
        }
    }
}
