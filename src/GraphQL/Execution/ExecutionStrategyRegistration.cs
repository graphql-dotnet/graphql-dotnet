using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents a registration for a specific execution strategy to be used for a specific GraphQL operation type.
    /// </summary>
    /// <param name="Strategy">The execution strategy to be used.</param>
    /// <param name="Operation">The GraphQL operation type the execution strategy applies to.</param>
    public record ExecutionStrategyRegistration(IExecutionStrategy Strategy, OperationType Operation);
}
