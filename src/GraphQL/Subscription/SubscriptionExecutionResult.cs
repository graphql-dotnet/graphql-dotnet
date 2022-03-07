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

        /// <inheritdoc cref="ExecutionResult()"/>
        public SubscriptionExecutionResult()
        {
        }

        /// <inheritdoc cref="ExecutionResult(ExecutionResult)"/>
        public SubscriptionExecutionResult(ExecutionResult result)
            : base(result)
        {
        }
    }
}
