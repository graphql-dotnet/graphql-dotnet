using System.Threading.Tasks;

namespace GraphQL.Authorization
{
    public interface IAuthorizationRequirement
    {
        Task Authorize(AuthorizationContext context);
    }
}