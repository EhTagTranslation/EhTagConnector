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
using System.Text.RegularExpressions;
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
            if (!context.HttpContext.Request.Headers.TryGetValue("X-Token", out var tokenHeader))
            {
                context.Result = new UnauthorizedObjectResult("No X-Token header.");
                return;
            }
            if (tokenHeader.Count != 1)
            {
                context.Result = new UnauthorizedObjectResult("Multiple X-Token header.");
                return;
            }
            var token = tokenHeader[0].Trim();
            if(string.IsNullOrEmpty(token) || !Regex.IsMatch(token, @"^[a-fA-F0-9]{8,}$"))
            {
                context.Result = new UnauthorizedObjectResult("Invalid X-Token header.");
                return;
            }
            try
            {
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
                context.Result = new UnauthorizedObjectResult("Invalid X-Token header.");
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
