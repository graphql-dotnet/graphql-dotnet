using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace GraphQL.GraphiQLCore
{
    public class GraphQLSettings
    {
        public PathString Path { get; set; } = "/graphql";
        public ISchema Schema { get; set; }
    }
}