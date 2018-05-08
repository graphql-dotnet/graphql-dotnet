using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public interface IFieldResolver
    {
        object Resolve(ResolveFieldContext context);
    }

    public interface IAsyncFieldResolver
    {
        Task<object> Resolve(ResolveFieldContext context);
    }
}
