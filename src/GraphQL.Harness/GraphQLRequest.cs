using System.Collections.Generic;

namespace Example
{
    public class GraphQLRequest
    {
        public string OperationName { get; set; }

        public string Query { get; set; }

        public Dictionary<string, object> Variables { get; set; }
    }
}
