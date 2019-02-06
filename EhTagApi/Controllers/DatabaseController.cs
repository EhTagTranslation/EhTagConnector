using EhTagApi.Filters;
using EhTagClient;
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
        public ActionResult<RecordDictionary> Get([SingleNamespace]Namespace @namespace)
        {
            return this.database[@namespace];
        }

        [HttpGet("{namespace}/{key}")]
        public ActionResult<Record> Get([SingleNamespace]Namespace @namespace, string key)
        {
            var dic = this.database[@namespace];
            var rec = dic.Find(key);

            if (rec is null)
                return NotFound();

            return rec;
        }

        [HttpDelete("{namespace}/{key}")]
        public ActionResult<Record> Delete([SingleNamespace]Namespace @namespace, string key,
            [Required][MinLength(1)][FromQuery]string username,
            [Required][EmailAddress][FromQuery]string email)
        {
            var dic = this.database[@namespace];
            var found = dic.Find(key);
            if (found is null)
                return NotFound();

            dic.Remove(key);
            dic.Save();

            var message = $@"In {@namespace.ToString().ToLower()}: Deleted '{key}'.

Previous value: {found}
Current value: (deleted)";
            RepositoryClient.Commit(message, new LibGit2Sharp.Identity(username, email));
            RepositoryClient.Push();
            return NoContent();
        }

        [HttpPost("{namespace}")]
        public ActionResult<Record> Post([SingleNamespace]Namespace @namespace,
            [Required][MinLength(1)][FromQuery]string username,
            [Required][EmailAddress][FromQuery]string email,
            [AcceptableTranslation][FromBody] Record record)
        {
            var dic = this.database[@namespace];
            var replaced = dic.Find(record.Original);

            if (replaced != null)
                return UnprocessableEntity(new { record = "Record with same 'original' is in the wiki, use PUT to update the record." });

            dic.AddOrReplace(record);
            dic.Save();

            var message = $@"In {@namespace.ToString().ToLower()}: Added '{record.Original}'.

Previous value: (non-existence)
Current value: {record}";
            RepositoryClient.Commit(message, new LibGit2Sharp.Identity(username, email));
            RepositoryClient.Push();
            return Created($"api/database/{@namespace.ToString().ToLower()}/{record.Original}", record);
        }

        [HttpPut("{namespace}")]
        public ActionResult<Record> Put([SingleNamespace]Namespace @namespace,
            [Required][MinLength(1)][FromQuery]string username,
            [Required][EmailAddress][FromQuery]string email,
            [AcceptableTranslation][FromBody] Record record)
        {
            var dic = this.database[@namespace];
            var replaced = dic.Find(record.Original);

            if (replaced == null)
                return UnprocessableEntity(new { record = "Record with same 'original' is not found in the wiki, use POST to insert the record." });

            dic.AddOrReplace(record);
            dic.Save();

            if (record.ToString() == replaced.ToString())
                return NoContent();

            var message = $@"In {@namespace.ToString().ToLower()}: Modified '{record.Original}'.

Previous value: {replaced}
Current value: {record}";
            RepositoryClient.Commit(message, new LibGit2Sharp.Identity(username, email));
            RepositoryClient.Push();
            return Ok(record);
        }
    }
}
