using EhTagClient;
using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EhTagApi.Formatters
{
    public class AstOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        public AstOutputFormatter(Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) : base(Consts.SerializerSettings, ArrayPool<char>.Shared, mvcOptions)
        {
            SerializerSettings.Converters.Add(new MdConverter(MdConverter.ConvertType.Ast));
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add("application/ast+json");
        }
    }
}
