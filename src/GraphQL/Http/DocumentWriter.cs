using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.Http
{
    public class DocumentWriter
    {
        public string Write(object value)
        {
            return JsonConvert.SerializeObject(
                value,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }
    }
}
