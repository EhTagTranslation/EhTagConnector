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
using User = Octokit.User;


namespace EhTagApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ServiceFilter(typeof(GitETagFilter))]
    public class DatabaseController : ControllerBase
    {
        private readonly ILogger _Logger;
        private readonly RepoClient _RepoClient;
        private readonly Database _Database;

        private void _Commit(Namespace @namespace, Record o, Record n, User user)
        {
            var verb = "Modified";
            if (o is null)
                verb = "Added";
            else if (n is null)
                verb = "Deleted";

            var message = $@"In {@namespace.ToString().ToLower()}: {verb} '{(o ?? n).Raw}'.

Previous value: {o}
Current value: {n}";

            _RepoClient.Commit(message, user.ToGitIdentity());
            _RepoClient.Push();
        }

        public DatabaseController(ILogger<WebhookController> logger, RepoClient repoClient, Database database)
        {
            _Logger = logger;
            _RepoClient = repoClient;
            _Database = database;
        }

        [HttpHead]
        public IActionResult Head() => NoContent();

        [HttpGet]
        public IActionResult Get()
        {
            var repo = _RepoClient.Repo;
            var head = _RepoClient.Head;

            return new JsonResult(new
            {
                Repo = _RepoClient.RemotePath,
                Head = new
                {
                    head.Author,
                    head.Committer,
                    head.Sha,
                    head.Message,
                },
                Version = _Database.GetVersion(),
                Data = _Database.Values.Select(v => new { v.Namespace, v.Count }),
            });
        }

        [HttpGet("{namespace}")]
        public IActionResult Get([SingleNamespace] Namespace @namespace)
        {
            var dic = _Database[@namespace];
            return new JsonResult(new { dic.Namespace, dic.Count });
        }

        [HttpHead("{namespace}/{original}")]
        public IActionResult Head([SingleNamespace] Namespace @namespace, string original)
        {
            var dic = _Database[@namespace];
            var rec = dic.Find(original);

            if (rec is null)
                return NotFound();

            return NoContent();
        }

        [HttpGet("{namespace}/{original}")]
        public ActionResult<Record> Get([SingleNamespace] Namespace @namespace, string original)
        {
            var dic = _Database[@namespace];
            var rec = dic.Find(original);

            if (rec is null)
                return NotFound();

            return rec;
        }

        [HttpDelete("{namespace}/{original}")]
        [ServiceFilter(typeof(GitHubIdentityFilter))]
        public ActionResult<Record> Delete(
            [SingleNamespace] Namespace @namespace,
            string original,
            [FromHeader] User user)
        {
            var dic = _Database[@namespace];
            var found = dic.Find(original);
            if (found is null)
                return NotFound();

            dic.Remove(original);
            dic.Save();

            _Commit(@namespace, found, null, user);
            return NoContent();
        }

        [HttpPost("{namespace}")]
        [ServiceFilter(typeof(GitHubIdentityFilter))]
        public ActionResult<Record> Post(
            [SingleNamespace] Namespace @namespace,
            [AcceptableTranslation, FromBody] Record record,
            [FromHeader] User user)
        {
            var dic = _Database[@namespace];
            var replaced = dic.Find(record.Raw);

            if (replaced != null)
                return UnprocessableEntity(new { record = "Record with same 'original' is in the wiki, use PUT to update the record." });

            dic.AddOrReplace(record);
            dic.Save();

            _Commit(@namespace, null, record, user);
            return Created($"api/database/{@namespace.ToString().ToLower()}/{record.Raw}", record);
        }

        [HttpPut("{namespace}")]
        [ServiceFilter(typeof(GitHubIdentityFilter))]
        public ActionResult<Record> Put(
            [SingleNamespace] Namespace @namespace,
            [AcceptableTranslation, FromBody] Record record,
            [FromHeader] User user)
        {
            var dic = _Database[@namespace];
            var replaced = dic.Find(record.Raw);

            if (replaced is null)
                return NotFound(new { record = "Record with same 'original' is not found in the wiki, use POST to insert the record." });

            dic.AddOrReplace(record);
            dic.Save();

            if (record.ToString() == replaced.ToString())
                return NoContent();

            _Commit(@namespace, replaced, record, user);
            return Ok(record);
        }
    }
}
