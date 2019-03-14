using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EhTagClient;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
            return Ok();
        }

        [HttpPost("normalize")]
        public IActionResult Normalize([FromBody] Record record)
        {
            return Ok(record);
        }

        [HttpPost("serialize/{raw}")]
        public IActionResult Serialize(
            [AcceptableRaw] string raw,
            [FromBody] Record record)
        {
            return Ok(record.ToString(raw));
        }

        [HttpPost("parse")]
        public IActionResult Parse([FromBody][Required] string tableRow)
        {
            var lines = tableRow.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length != 1)
            {
                var error = new ModelStateDictionary();
                error.AddModelError(nameof(tableRow), "Input must be in only one line.");
                return BadRequest(error);
            }
            var r = Record.TryParse(lines[0]);
            if (r.Key is null)
            {
                var error = new ModelStateDictionary();
                error.AddModelError(nameof(tableRow), "Failed to parse it as a markdown table row.");
                return BadRequest(error);
            }
            return Ok(r);
        }
    }
}
