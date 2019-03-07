using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EhTagClient;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EhTagApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [FormatFilter]
    public class ToolsController : ControllerBase
    {
        [HttpHead("status")]
        public IActionResult Status()
        {
            return NoContent();
        }

        [HttpPost("normalize")]
        public IActionResult Normalize([FromBody] Record record)
        {
            return Ok(record);
        }
    }
}
