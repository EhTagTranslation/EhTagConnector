using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EhTagApi.Formatters
{
    public class TextFormatter : InputFormatter
    {
        public TextFormatter()
        {
            SupportedMediaTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("text/*"));
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            var content = Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(request.ContentType);
            using (var reader = context.ReaderFactory(request.Body, content.Encoding ?? Encoding.UTF8))
            {
                return await InputFormatterResult.SuccessAsync(await reader.ReadToEndAsync());
            }
        }
    }
}
