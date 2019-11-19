using EhTagClient;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Buffers;

namespace EhTagApi.Formatters
{
    public class FullOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        public FullOutputFormatter(Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) : base(Consts.SerializerSettings, ArrayPool<char>.Shared, mvcOptions)
        {
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add("application/json");
            SupportedMediaTypes.Add("application/problem+json");
        }
    }
}
