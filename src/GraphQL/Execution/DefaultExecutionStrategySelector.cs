using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <inheritdoc cref="IExecutionStrategySelector"/>
    public class DefaultExecutionStrategySelector : IExecutionStrategySelector
    {
        private readonly ExecutionStrategyRegistration[] _registrations;

        /// <summary>
        /// Initializes an instance that only returns the default registrations;
        /// <see cref="ParallelExecutionStrategy"/> for <see cref="OperationType.Query"/> and
        /// <see cref="SerialExecutionStrategy"/> for <see cref="OperationType.Mutation"/>.
        /// </summary>
        public DefaultExecutionStrategySelector()
            : this(Array.Empty<ExecutionStrategyRegistration>())
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified registrations.
        /// If no registration is specified for <see cref="OperationType.Query"/>, returns <see cref="ParallelExecutionStrategy"/>.
        /// If no registration is specified for <see cref="OperationType.Mutation"/>, returns <see cref="SerialExecutionStrategy"/>.
        /// </summary>
        public DefaultExecutionStrategySelector(IEnumerable<ExecutionStrategyRegistration> registrations)
        {
            _registrations = (registrations ?? throw new ArgumentNullException(nameof(registrations))).ToArray();
        }

        /// <inheritdoc/>
        public virtual IExecutionStrategy Select(ExecutionContext context)
        {
            var operationType = context.Operation.Operation;

            foreach (var registration in _registrations)
            {
                if (registration.Operation == operationType)
                    return registration.Strategy;
            }
            // no matching registration, so return default implementation
            return operationType switch
            {
                OperationType.Query => ParallelExecutionStrategy.Instance,
                OperationType.Mutation => SerialExecutionStrategy.Instance,
                OperationType.Subscription => SubscriptionExecutionStrategy.Instance,
                _ => throw new InvalidOperationException($"Unexpected OperationType '{operationType}'.")
            };
        }
    }
}
