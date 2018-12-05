using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public interface IFieldResolver
    {
        object Resolve(ResolveFieldContext context);
        object Resolve(ExecutionContext context, ExecutionNode node);
    }

    public interface IFieldResolver<out T> : IFieldResolver
    {
        new T Resolve(ResolveFieldContext context);
        new T Resolve(ExecutionContext context, ExecutionNode node);
    }
}
