using ExecutionContext = GraphQL.Execution.ExecutionContext;

namespace GraphQL.Subscription
{
    /// <inheritdoc/>
    public class SubscriptionExecutionResult : ExecutionResult
    {
        /// <summary>
        /// Gets or sets a dictionary of returned subscription fields along with their
        /// event streams as <see cref="IObservable{T}"/> implementations.
        /// </summary>
        public IDictionary<string, IObservable<ExecutionResult>>? Streams { get; set; }

        /// <inheritdoc/>
        public SubscriptionExecutionResult()
        {
        }

        /// <inheritdoc/>
        internal SubscriptionExecutionResult(ExecutionContext context)
            : base(context)
        {
        }

        /// <inheritdoc/>
        public SubscriptionExecutionResult(ExecutionResult result)
            : base(result)
        {
        }
    }
}
