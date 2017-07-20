using System.Security.Claims;

namespace GraphQL.Tools
{
    public interface IProvideClaimsPrincipal
    {
        ClaimsPrincipal Principal { get; }
    }
}