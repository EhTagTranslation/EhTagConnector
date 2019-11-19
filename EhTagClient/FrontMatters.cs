using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace EhTagClient
{
    public class FrontMatters : Dictionary<string, object>
    {
        private readonly static IDeserializer _Deserializer = new DeserializerBuilder()
            //.WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();

        public static FrontMatters Parse(string value)
        {
            return _Deserializer.Deserialize<FrontMatters>(value);
        }
    }
}
