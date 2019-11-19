using EhTagClient;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Buffers;

namespace EhTagApi.Formatters
{
    public class RawOutputFormatter : JsonOutputFormatter
    {
        public RawOutputFormatter() : base(Consts.SerializerSettings, ArrayPool<char>.Shared)
        {
            SerializerSettings.Converters.Add(new MdConverter(MdConverter.ConvertType.Raw));
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add("application/raw+json");
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context) => base.CanWriteResult(context);
    }
}
