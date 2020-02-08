using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public interface IFieldResolverInternal
    {
        Task SetResultAsync(IResolveFieldContext context);
    }
}
