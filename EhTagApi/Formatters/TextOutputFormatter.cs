using EhTagClient;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Buffers;

namespace EhTagApi.Formatters
{
    public class TextOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        public TextOutputFormatter(Microsoft.AspNetCore.Mvc.MvcOptions options) : base(Consts.SerializerSettings, ArrayPool<char>.Shared, options)
        {
            SerializerSettings.Converters.Add(new MdConverter(MdConverter.ConvertType.Text));
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add("application/text+json");
        }
    }
}
