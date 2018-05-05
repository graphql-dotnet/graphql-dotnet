using System.Security.Claims;

namespace Example
{
    public class GraphQLUserContext
    {
        public ClaimsPrincipal User { get; set; }
    }
}
