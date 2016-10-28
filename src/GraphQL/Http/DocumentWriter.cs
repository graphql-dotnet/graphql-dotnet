using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.Http
{
    public interface IDocumentWriter
    {
        string Write(object value);
    }

    public class DocumentWriter : IDocumentWriter
    {
        private readonly Formatting _formatting;
        private readonly JsonSerializerSettings _settings;

        public DocumentWriter()
            : this(indent: false)
        {
        }

        public DocumentWriter(bool indent)
            : this(
                indent ? Formatting.Indented : Formatting.None,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF'Z'",
                })
        {
        }

        public DocumentWriter(Formatting formatting, JsonSerializerSettings settings)
        {
            _formatting = formatting;
            _settings = settings;
        }

        public string Write(object value)
        {
            return JsonConvert.SerializeObject(value, _formatting, _settings);
        }
    }
}
