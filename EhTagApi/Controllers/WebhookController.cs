using EhTagClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace EhTagApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : Controller
    {
        private readonly ILogger logger;

        public WebhookController(ILogger<WebhookController> logger)
        {
            this.logger = logger;
        }

        [HttpPost]
        public IActionResult Post(
            [FromHeader(Name = "X-GitHub-Event")] string ev,
            [FromHeader(Name = "X-GitHub-Delivery")] Guid delivery,
            [FromBody]dynamic payload)
        {
            if (delivery == Guid.Empty)
                return BadRequest($"Wrong X-GitHub-Delivery");
            if (string.IsNullOrWhiteSpace(ev))
                return BadRequest($"Missing X-GitHub-Event");

            if (ev == "ping")
                return NoContent();

            if (ev != "push")
                return BadRequest($"Unsupported X-GitHub-Event");

            using (var repo = RepositoryClient.Get())
            {
                var head = repo.Commits.First();
                if (head.Sha.Equals((string)payload.after.Value, StringComparison.OrdinalIgnoreCase))
                    return Ok("Already up-to-date.");
            }

            var start = DateTimeOffset.Now;
            RepositoryClient.Pull();
            var log = $"Pulled form github in {(DateTimeOffset.Now - start).TotalMilliseconds}ms.";
            this.logger.LogInformation(log);
            return Ok(log);
        }
    }
}
