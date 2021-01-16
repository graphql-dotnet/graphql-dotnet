using GraphQL.Execution;

namespace GraphQL.Resolvers
{
    internal interface IOptimizedFieldResolver
    {
        object Resolve(ExecutionNode node, ExecutionContext context);
    }
}
