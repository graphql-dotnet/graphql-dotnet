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
        /// Initializes an instance that returns the specified registrations.
        /// </summary>
        public DefaultExecutionStrategySelector(IExecutionStrategy queryExecutionStrategy, IExecutionStrategy mutationExecutionStrategy, IExecutionStrategy? subscriptionExecutionStrategy = null)
            : this(GetRegistrations(queryExecutionStrategy, mutationExecutionStrategy, subscriptionExecutionStrategy))
        {
        }

        private static ExecutionStrategyRegistration[] GetRegistrations(IExecutionStrategy queryExecutionStrategy, IExecutionStrategy mutationExecutionStrategy, IExecutionStrategy? subscriptionExecutionStrategy)
        {
            if (queryExecutionStrategy == null)
                throw new ArgumentNullException(nameof(queryExecutionStrategy));
            if (mutationExecutionStrategy == null)
                throw new ArgumentNullException(nameof(mutationExecutionStrategy));

            if (subscriptionExecutionStrategy == null)
                return new ExecutionStrategyRegistration[] {
                    new ExecutionStrategyRegistration(queryExecutionStrategy, OperationType.Query),
                    new ExecutionStrategyRegistration(mutationExecutionStrategy, OperationType.Mutation),
                };
            else
            {
                return new ExecutionStrategyRegistration[] {
                    new ExecutionStrategyRegistration(queryExecutionStrategy, OperationType.Query),
                    new ExecutionStrategyRegistration(mutationExecutionStrategy, OperationType.Mutation),
                    new ExecutionStrategyRegistration(subscriptionExecutionStrategy, OperationType.Subscription),
                };
            }
        }

        /// <summary>
        /// Initializes a new instance with the specified registrations.
        /// If no registration is specified for <see cref="OperationType.Query"/>, returns <see cref="ParallelExecutionStrategy"/>.
        /// If no registration is specified for <see cref="OperationType.Mutation"/>, returns <see cref="SerialExecutionStrategy"/>.
        /// </summary>
        public DefaultExecutionStrategySelector(IEnumerable<ExecutionStrategyRegistration> registrations)
        {
            _registrations = registrations is ExecutionStrategyRegistration[] array
                ? array
                : (registrations ?? throw new ArgumentNullException(nameof(registrations))).ToArray();
        }

        /// <inheritdoc/>
        public virtual IExecutionStrategy Select(OperationType operationType)
        {
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
                OperationType.Subscription => throw new NotSupportedException($"No execution strategy for executing subscriptions has been configured. You can use the GraphQL.SystemReactive package to handle subscriptions."),
                _ => throw new InvalidOperationException($"Unexpected OperationType '{operationType}'.")
            };
        }
    }
}
