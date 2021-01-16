using GraphQL.Execution;

namespace GraphQL
{
    public interface IResolveFieldContextProvider
    {
        IResolveFieldContext CreateContext(ExecutionNode node, ExecutionContext context);
    }
}
