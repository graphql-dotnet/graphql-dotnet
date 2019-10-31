using GraphQL.Types;
using System.Threading.Tasks;

namespace GraphQL.Resolvers
{
    public interface IFieldResolver
    {
        Task<object> ResolveAsync(ResolveFieldContext context);
    }

    public interface IFieldResolver<T> : IFieldResolver
    {
        new Task<T> ResolveAsync(ResolveFieldContext context);
    }
}
