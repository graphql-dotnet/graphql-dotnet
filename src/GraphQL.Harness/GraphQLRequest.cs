using System.Text.Json.Serialization;
using GraphQL;
using GraphQL.SystemTextJson;

namespace Example
{
    public class GraphQLRequest
    {
        public string OperationName { get; set; }

        public string Query { get; set; }

        [JsonConverter(typeof(InputsConverter))]
        public Inputs Variables { get; set; }
    }
}
