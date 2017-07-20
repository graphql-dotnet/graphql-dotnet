using System;
using Microsoft.AspNetCore.Http;

namespace GraphQL.GraphiQLCore
{
    public class GraphQLSettings
    {
        public PathString Path { get; set; } = "/api/graphql";
        public ISchema Schema { get; set; }
        public Func<HttpContext, object> BuildUserContext { get; set; }
    }
}