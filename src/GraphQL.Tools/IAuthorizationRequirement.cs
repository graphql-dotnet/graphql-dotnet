using System.Threading.Tasks;

namespace GraphQL.Tools
{
    public interface IAuthorizationRequirement
    {
        Task Authorize(AuthorizationContext context);
    }
}