using EhTagClient;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EhTagApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseController : ControllerBase
    {
        public Database Database { get; }

        public DatabaseController()
        {
            RepositoryClient.Pull();
            Database = new Database();
            Database.Load();
        }

        [HttpGet]
        public ActionResult<IEnumerable<Namespace>> Get()
        {
            return new JsonResult(Database.Keys);
        }

        [HttpGet("{namespace}")]
        public ActionResult<RecordDictionary> Get(Namespace @namespace)
        {
            try
            {
                return Database[@namespace];
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
    }
}
