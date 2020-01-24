using EhTagClient;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Buffers;

namespace EhTagApi.Formatters
{
    public class RawOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        public RawOutputFormatter(Microsoft.AspNetCore.Mvc.MvcOptions options) : base(Consts.SerializerSettings, ArrayPool<char>.Shared, options)
        {
            SerializerSettings.Converters.Add(new MdConverter(MdConverter.ConvertType.Raw));
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add("application/raw+json");
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context) => base.CanWriteResult(context);
    }
}
