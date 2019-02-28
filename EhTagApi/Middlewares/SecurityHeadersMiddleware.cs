using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EhTagApi.Middlewares
{
    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }

    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _Next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _Next = next;
        }

        private readonly Dictionary<string, StringValues> _SetHeaders = new Dictionary<string, StringValues>
        {
            ["Content-Security-Policy"] = "default-src 'none'",
            ["X-Frame-Options"] = "DENY",
            ["X-XSS-Protection"] = "1; mode=block",
            ["X-Content-Type-Options"] = "nosniff",
            ["Referrer-Policy"] = "no-referrer",
            ["Feature-Policy"] = "",
            ["Expect-CT"] = "enforce, max-age=86400",
        };

        private readonly HashSet<string> _RemoveHeaders = new HashSet<string>
        {
            "X-Powered-By",
            "Server"
        };

        public async Task Invoke(HttpContext context)
        {
            var headers = context.Response.Headers;

            foreach (var headerValuePair in _SetHeaders)
            {
                headers[headerValuePair.Key] = headerValuePair.Value;
            }

            foreach (var header in _RemoveHeaders)
            {
                headers.Remove(header);
            }

            await _Next(context);
        }
    }
}
