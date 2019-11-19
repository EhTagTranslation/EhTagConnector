using EhTagClient;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Buffers;

namespace EhTagApi.Formatters
{
    public class HtmlOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        public HtmlOutputFormatter(Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) : base(Consts.SerializerSettings, ArrayPool<char>.Shared, mvcOptions)
        {
            SerializerSettings.Converters.Add(new MdConverter(MdConverter.ConvertType.Html));
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add("application/html+json");
        }
    }
}
