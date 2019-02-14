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

        private void _Commit(Namespace @namespace, string k, Record o, Record n, User user)
        {
#if !DEBUG
            var verb = "Modified";
            if (o is null)
                verb = "Added";
            else if (n is null)
                verb = "Deleted";

            var message = $@"In {@namespace.ToString().ToLower()}: {verb} '{k}'.

Previous value: {o?.ToString(k)}
Current value: {n?.ToString(k)}";

            _RepoClient.Commit(message, user.ToGitIdentity());
            _RepoClient.Push();
#endif
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

        [HttpHead("{namespace}/{raw}")]
        public IActionResult Head(
            [SingleNamespace] Namespace @namespace, 
            [AcceptableRaw] string raw)
        {
            var dic = _Database[@namespace];
            var rec = dic.Find(raw);

            if (rec is null)
                return NotFound();

            return NoContent();
        }

        [HttpGet("{namespace}/{raw}")]
        public ActionResult<Record> Get(
            [SingleNamespace] Namespace @namespace, 
            [AcceptableRaw] string raw)
        {
            var dic = _Database[@namespace];
            var rec = dic.Find(raw);

            if (rec is null)
                return NotFound();

            return rec;
        }

        [HttpDelete("{namespace}/{raw}")]
        [ServiceFilter(typeof(GitHubIdentityFilter))]
        public ActionResult<Record> Delete(
            [SingleNamespace] Namespace @namespace,
            [AcceptableRaw] string raw,
            [FromHeader] User user)
        {
            var dic = _Database[@namespace];
            var found = dic.Find(raw);
            if (found is null)
                return NotFound();

            dic.Remove(raw);
            dic.Save();

            _Commit(@namespace, raw, found, null, user);
            return NoContent();
        }

        [HttpPost("{namespace}/{raw}")]
        [ServiceFilter(typeof(GitHubIdentityFilter))]
        public ActionResult<Record> Post(
            [SingleNamespace] Namespace @namespace,
            [AcceptableRaw] string raw,
            [AcceptableTranslation, FromBody] Record record,
            [FromHeader] User user)
        {
            var dic = _Database[@namespace];
            var replaced = dic.Find(raw);

            if (replaced != null)
                return UnprocessableEntity(new { record = "Record with same 'raw' is in the wiki, use PUT to update the record." });

            dic.AddOrReplace(raw, record);
            dic.Save();

            _Commit(@namespace, raw, null, record, user);
            return Created($"/api/database/{@namespace.ToString().ToLower()}/{raw}", record);
        }

        [HttpPut("{namespace}/{raw}")]
        [ServiceFilter(typeof(GitHubIdentityFilter))]
        public ActionResult<Record> Put(
            [SingleNamespace] Namespace @namespace,
            [AcceptableRaw] string raw,
            [AcceptableTranslation, FromBody] Record record,
            [FromHeader] User user)
        {
            var dic = _Database[@namespace];
            var replaced = dic.Find(raw);

            if (replaced is null)
                return NotFound(new { record = "Record with same 'raw' is not found in the wiki, use POST to insert the record." });

            dic.AddOrReplace(raw, record);
            dic.Save();

            if (record.ToString(raw) == replaced.ToString(raw))
                return NoContent();

            _Commit(@namespace, raw, replaced, record, user);
            return Ok(record);
        }
    }
}
