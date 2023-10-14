using System.Security.Claims;

namespace Example;

public class GraphQLUserContext : Dictionary<string, object>
{
    public ClaimsPrincipal User { get; set; }
}
