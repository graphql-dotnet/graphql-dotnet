using Newtonsoft.Json;

namespace GraphQL
{
    public class ExecutionResult
    {
        public object Data { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ExecutionErrors Errors { get; set; }
    }
}
