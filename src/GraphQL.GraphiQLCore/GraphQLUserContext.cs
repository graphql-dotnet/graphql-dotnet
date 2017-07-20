using System.Security.Claims;

namespace GraphQL.GraphiQLCore
{
    public class GraphQLUserContext
    {
        public ClaimsPrincipal User { get; set; }
    }
}
