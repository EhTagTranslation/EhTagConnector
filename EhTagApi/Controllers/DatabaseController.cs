using EhTagApi.Filters;
using EhTagClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EhTagApi.Controllers
{
    public class IdentityBuilder
    {
        [Required, EmailAddress, FromQuery(Name = "email")]
        public string Email { get; set; }
        [Required, MinLength(1), FromQuery(Name = "name")]
        public string Name { get; set; }

        public LibGit2Sharp.Identity Build() => new LibGit2Sharp.Identity(Name, Email);
    }

    [Route("api/[controller]")]
    [ApiController]
    [GitETagFilter]
    public class DatabaseController : ControllerBase
    {
        private readonly ILogger logger;
        private readonly Database database;

        public DatabaseController(ILogger<WebhookController> logger, Database database)
        {
            this.logger = logger;
            this.database = database;
        }

        [HttpHead]
        public IActionResult Head()
        {
            return NoContent();
        }

        [HttpGet]
        public IActionResult Get()
        {
            var repo = RepositoryClient.Repo;
            var head = repo.Commits.First();

            return new JsonResult(new
            {
                Repo = repo.Network.Remotes["origin"].PushUrl,
                Head = new
                {
                    head.Author,
                    head.Committer,
                    head.Sha,
                    head.Message,
                },
                Version = this.database.GetVersion(),
                Data = this.database.Values.Select(v => new { v.Namespace, v.Count }),
            });
        }

        [HttpGet("{namespace}")]
        public ActionResult<RecordDictionary> Get([SingleNamespace] Namespace @namespace)
        {
            return this.database[@namespace];
        }

        [HttpHead("{namespace}/{original}")]
        public IActionResult Head([SingleNamespace] Namespace @namespace, string original)
        {
            var dic = this.database[@namespace];
            var rec = dic.Find(original);

            if (rec is null)
                return NotFound();

            return NoContent();
        }

        [HttpGet("{namespace}/{original}")]
        public ActionResult<Record> Get([SingleNamespace] Namespace @namespace, string original)
        {
            var dic = this.database[@namespace];
            var rec = dic.Find(original);

            if (rec is null)
                return NotFound();

            return rec;
        }

        [HttpDelete("{namespace}/{original}")]
        public ActionResult<Record> Delete([SingleNamespace] Namespace @namespace,
            string original,
            [FromQuery]IdentityBuilder identity)
        {
            var dic = this.database[@namespace];
            var found = dic.Find(original);
            if (found is null)
                return NotFound();

            dic.Remove(original);
            dic.Save();

            commit(@namespace, found, null, identity);
            return NoContent();
        }

        [HttpPost("{namespace}")]
        public ActionResult<Record> Post([SingleNamespace] Namespace @namespace,
            [FromQuery]IdentityBuilder identity,
            [AcceptableTranslation, FromBody] Record record)
        {
            var dic = this.database[@namespace];
            var replaced = dic.Find(record.Original);

            if (replaced != null)
                return UnprocessableEntity(new { record = "Record with same 'original' is in the wiki, use PUT to update the record." });

            dic.AddOrReplace(record);
            dic.Save();

            commit(@namespace, null, record, identity);
            return Created($"api/database/{@namespace.ToString().ToLower()}/{record.Original}", record);
        }

        [HttpPut("{namespace}")]
        public ActionResult<Record> Put([SingleNamespace] Namespace @namespace,
            [FromQuery]IdentityBuilder identity,
            [AcceptableTranslation, FromBody] Record record)
        {
            var dic = this.database[@namespace];
            var replaced = dic.Find(record.Original);

            if (replaced is null)
                return NotFound(new { record = "Record with same 'original' is not found in the wiki, use POST to insert the record." });

            dic.AddOrReplace(record);
            dic.Save();

            if (record.ToString() == replaced.ToString())
                return NoContent();

            commit(@namespace, replaced, record, identity);
            return Ok(record);
        }

        private void commit(Namespace @namespace, Record o, Record n, IdentityBuilder identity)
        {
            var verb = "Modified";
            if (o is null)
                verb = "Added";
            else if (n is null)
                verb = "Deleted";

            var message = $@"In {@namespace.ToString().ToLower()}: {verb} '{(o ?? n).Original}'.

Previous value: {o}
Current value: {n}";

            RepositoryClient.Commit(message, identity.Build());
            RepositoryClient.Push();
        }
    }
}
