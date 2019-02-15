using System;
using Newtonsoft.Json;

namespace EhTagClient
{
    public class MdConverter : JsonConverter
    {
        public ConvertType Type { get; set; }

        public enum ConvertType
        {
            Raw,
            Text,
            Html,
            Ast,
        }

        public MdConverter() { }

        public MdConverter(ConvertType type) => Type = type;

        public override bool CanConvert(Type objectType) => objectType == typeof(MarkdownText);
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (Type)
            {
            case ConvertType.Raw:
                serializer.Serialize(writer, ((MarkdownText)value).Raw);
                break;
            case ConvertType.Text:
                serializer.Serialize(writer, ((MarkdownText)value).Text);
                break;
            case ConvertType.Html:
                serializer.Serialize(writer, ((MarkdownText)value).Html);
                break;
            case ConvertType.Ast:
                serializer.Serialize(writer, ((MarkdownText)value).Ast);
                break;
            default:
                throw new NotImplementedException();
            }
        }
    }
}
