using EhTagClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EhTagApi.Filters
{
    public static class IdentityExtension
    {
        public static LibGit2Sharp.Identity ToGitIdentity(this User user)
            => new LibGit2Sharp.Identity(user.Login, $"{user.Id}+{user.Login}@users.noreply.github.com");
    }

    public class GitHubIdentityFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authstr))
            {
                context.Result = new UnauthorizedObjectResult("No authorization header.");
                return;
            }
            if (!System.Net.Http.Headers.AuthenticationHeaderValue.TryParse(authstr.FirstOrDefault(), out var auth))
            {
                context.Result = new UnauthorizedObjectResult("Invalid authorization header.");
                return;
            }
            if (!"token".Equals(auth.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new UnauthorizedObjectResult("Unsupported authorization scheme, only 'token' supported.");
                return;
            }
            try
            {
                var token = auth.Parameter.Trim();
                var client = new GitHubClient(new ProductHeaderValue(Consts.Username, "1.0"))
                {
                    Credentials = new Credentials(token)
                };
                var user = client.User.Current().Result;
                foreach (var item in context.ActionArguments.Keys.ToArray())
                {
                    if (context.ActionArguments[item] is User)
                        context.ActionArguments[item] = user;
                }
            }
            catch
            {
                context.Result = new UnauthorizedObjectResult("Invalid token.");
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
