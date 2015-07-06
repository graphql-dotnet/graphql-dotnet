using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL
{
    public class DocumentWriter
    {
        public string Write(ExecutionResult result)
        {
            return JsonConvert.SerializeObject(
                result,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }
    }
}