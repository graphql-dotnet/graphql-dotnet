using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <inheritdoc cref="IExecutionStrategySelector"/>
public class DefaultExecutionStrategySelector : IExecutionStrategySelector
{
    /// <inheritdoc/>
    public virtual IExecutionStrategy Select(ExecutionContext context)
    {
        return context.Operation.Operation switch
        {
            OperationType.Query => ParallelExecutionStrategy.Instance,
            OperationType.Mutation => SerialExecutionStrategy.Instance,
            OperationType.Subscription => throw new NotSupportedException(),
            _ => throw new InvalidOperationException()
        };
    }
}
}
