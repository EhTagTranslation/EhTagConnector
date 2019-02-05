using EhTagClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

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
            [FromHeader(Name = "X-GitHub-Delivery")] string delivery,
            [FromBody]object payload)
        {
            if (ev != "pull" || delivery == null)
                return BadRequest();

            var start = DateTimeOffset.Now;
            RepositoryClient.Pull();
            var log = $"Pulled form github in {(DateTimeOffset.Now - start).TotalMilliseconds}ms.";
            this.logger.LogInformation(log);
            return Ok(log);
        }
    }
}
