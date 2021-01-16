using GraphQL.Execution;

namespace GraphQL
{
    public interface IResolveEventStreamContextProvider
    {
        IResolveEventStreamContext CreateContext(ExecutionNode node, ExecutionContext context);
    }
}
