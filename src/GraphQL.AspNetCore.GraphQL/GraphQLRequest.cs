namespace GraphQL.AspNetCore.GraphQL {
    internal sealed class GraphQLRequest {
        public string OperationName { get; set; }

        public string Query { get; set; }

        public string Variables { get; set; }
    }
}
