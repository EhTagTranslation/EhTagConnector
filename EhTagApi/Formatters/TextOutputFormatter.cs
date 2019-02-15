using EhTagClient;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Buffers;

namespace EhTagApi.Formatters
{
    public class TextOutputFormatter : JsonOutputFormatter
    {
        public TextOutputFormatter() : base(Consts.SerializerSettings, ArrayPool<char>.Shared)
        {
            SerializerSettings.Converters.Add(new MdConverter(MdConverter.ConvertType.Text));
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add("application/text+json");
        }
    }
}
