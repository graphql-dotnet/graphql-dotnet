using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public interface IFieldResolver
    {
        object Resolve(IResolveFieldContext context);
    }

    public interface IFieldResolver<out T> : IFieldResolver
    {
        new T Resolve(IResolveFieldContext context);
    }
}
