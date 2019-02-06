using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EhTagApi.Filters
{
    public class GitETagFilterAttribute : Attribute, IActionFilter
    {
        private string CurrentETag => '"' + EhTagClient.RepositoryClient.CurrentSha + '"';

        private bool EqualsCurrentETag(StringValues eTagValue)
        {
            var tag = eTagValue.FirstOrDefault();
            if (string.IsNullOrEmpty(tag))
                return false;
            if (tag.Length <= 40)
                return false;

            return CurrentETag.Equals(tag, StringComparison.OrdinalIgnoreCase);
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            switch (context.HttpContext.Request.Method)
            {
            case "GET":
            case "HEAD":
                if (!context.HttpContext.Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
                    return;
                if (!EqualsCurrentETag(ifNoneMatch))
                    return;
                context.Result = new StatusCodeResult(304);
                return;

            case "POST":
            case "PUT":
            case "PATCH":
            case "DELETE":
                if (!context.HttpContext.Request.Headers.TryGetValue("If-Match", out var ifMatch))
                {
                    context.Result = new BadRequestObjectResult(new
                    {
                        IfMatch = "'If-Match' header is not present, use corresponding GET api to retrieve the ETag."
                    });
                    return;
                }
                if (!EqualsCurrentETag(ifMatch))
                {
                    context.Result = new ObjectResult(new
                    {
                        IfMatch = "The wiki has been modified, use corresponding GET api to renew the ETag."
                    })
                    { StatusCode = 412 };
                    return;
                }
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.HttpContext.Response.StatusCode < 300 && context.HttpContext.Response.StatusCode >= 200)
                context.HttpContext.Response.Headers.Add("ETag", new StringValues(CurrentETag));
        }
    }
}
