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
    public class DatabaseController : ControllerBase
    {
        public Database Database { get; }

        private readonly ILogger logger;

        public DatabaseController(ILogger<WebhookController> logger)
        {
            Database = new Database();
            this.logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            using (var repo = RepositoryClient.Get())
            {
                var head = repo.Commits.First();
                return new JsonResult(new
                {
                    Head = new
                    {
                        head.Author,
                        head.Committer,
                        head.Sha,
                        head.Message
                    },
                    Version = Database.GetVersion(),
                    Namespaces = Database.Values.Select(v => new { v.Namespace, v.Count }),
                });
            }
        }

        [HttpGet("{namespace}")]
        public ActionResult<RecordDictionary> Get(Namespace @namespace)
        {
            try
            {
                var db = Database[@namespace];
                db.Load();
                return db;
            }
            catch (KeyNotFoundException)
            {
                return BadRequest(new { @namespace = new[] { $"The value '{@namespace}' is not valid." } });
            }
        }

        [HttpGet("{namespace}/{key}")]
        public ActionResult<Record> Get(Namespace @namespace, string key)
        {
            try
            {
                var dic = Database[@namespace];
                dic.Load();
                var rec = dic.Find(key);

                if (rec is null)
                    return NotFound();

                return rec;
            }
            catch (KeyNotFoundException)
            {
                return BadRequest(new { @namespace = new[] { $"The value '{@namespace}' is not valid." } });
            }
        }

        [HttpDelete("{namespace}")]
        public ActionResult<Record> Delete(Namespace @namespace, [FromQuery]string username, [EmailAddress][FromQuery]string email, [FromBody] Record record)
        {
            try
            {
                var dic = Database[@namespace];
                dic.Load();
                var found = dic.Find(record.Original);

                if (found is null)
                    return NotFound();

                if (record.ToString() != found.ToString())
                    return Conflict(new
                    {
                        Exist = found,
                        Request = record
                    });

                dic.Remove(record.Original);
                dic.Save();
                try
                {
                    var message = $@"In {@namespace.ToString().ToLower()}: Deleted '{record.Original}'.

Previous value: {found}
Current value: (deleted)";
                    RepositoryClient.Commit(message, new LibGit2Sharp.Identity(username, email));
                    RepositoryClient.Push();
                    return Ok(record);
                }
                catch (Exception ex)
                {
                    return Conflict(ex);
                }
            }
            catch (KeyNotFoundException)
            {
                return BadRequest(new { @namespace = new[] { $"The value '{@namespace}' is not valid." } });
            }
        }

        [HttpPost("{namespace}")]
        public ActionResult<Record> Post(Namespace @namespace, [FromQuery]string username, [EmailAddress][FromQuery]string email, [FromBody] Record record)
        {
            try
            {
                var errors = new ModelStateDictionary();
                if (string.IsNullOrEmpty(record.Original))
                    errors.AddModelError("original", "original should not be empty");
                if (string.IsNullOrEmpty(record.TranslatedRaw))
                    errors.AddModelError("translated", "translated should not be empty");
                if (!errors.IsValid)
                    return ValidationProblem(errors);

                var dic = Database[@namespace];
                dic.Load();
                var replaced = dic.AddOrReplace(record);

                if (replaced != null && record.ToString() == replaced.ToString())
                    return NoContent();

                dic.Save();
                try
                {
                    var message = $@"In {@namespace.ToString().ToLower()}: {(replaced is null ? "Added" : "Modified")} '{record.Original}'.

Previous value: {(object)replaced ?? "(non-existence)"}
Current value: {record}";
                    RepositoryClient.Commit(message, new LibGit2Sharp.Identity(username, email));
                    RepositoryClient.Push();
                    return Created($"api/database/{@namespace.ToString().ToLower()}/{record.Original}", record);
                }
                catch (Exception ex)
                {
                    return Conflict(ex);
                }
            }
            catch (KeyNotFoundException)
            {
                return BadRequest(new { @namespace = new[] { $"The value '{@namespace}' is not valid." } });
            }
        }
    }
}
