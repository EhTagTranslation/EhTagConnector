using EhTagClient;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Buffers;

namespace EhTagApi.Formatters
{
    public class RawOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        public RawOutputFormatter(Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) : base(Consts.SerializerSettings, ArrayPool<char>.Shared, mvcOptions)
        {
            SerializerSettings.Converters.Add(new MdConverter(MdConverter.ConvertType.Raw));
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add("application/raw+json");
        }
    }
}
