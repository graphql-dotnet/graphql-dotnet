using System.Collections.Generic;
using System.Text.Json.Serialization;
using GraphQL.SystemTextJson;

namespace Example
{
    public class GraphQLRequest
    {
        public string OperationName { get; set; }

        public string Query { get; set; }

        [JsonConverter(typeof(ObjectDictionaryConverter))]
        public Dictionary<string, object> Variables { get; set; }
    }
}
