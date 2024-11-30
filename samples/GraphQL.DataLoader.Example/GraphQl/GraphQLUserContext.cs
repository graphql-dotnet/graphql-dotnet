using System.Security.Claims;

namespace Example;

public class GraphQlUserContext : Dictionary<string, object>
{
    public ClaimsPrincipal User { get; set; } = null!;
}
