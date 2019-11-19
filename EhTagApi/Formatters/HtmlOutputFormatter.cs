using EhTagClient;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Buffers;

namespace EhTagApi.Formatters
{
    public class HtmlOutputFormatter : JsonOutputFormatter
    {
        public HtmlOutputFormatter() : base(Consts.SerializerSettings, ArrayPool<char>.Shared)
        {
            SerializerSettings.Converters.Add(new MdConverter(MdConverter.ConvertType.Html));
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add("application/html+json");
        }
    }
}
