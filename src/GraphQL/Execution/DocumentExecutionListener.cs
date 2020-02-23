using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Validation;

namespace GraphQL.Execution
{
    /// <summary>
    /// Provides the ability to log query validation failures and monitor progress of a GraphQL request's execution.
    /// </summary>
    public interface IDocumentExecutionListener
    {
        /// <summary>Executes after document validation is complete. Can be used to log validation failures.</summary>
        Task AfterValidationAsync(IDictionary<string, object> userContext, IValidationResult validationResult, CancellationToken token);

        /// <summary>Executes after document validation passes, before calling <see cref="IExecutionStrategy.ExecuteAsync(ExecutionContext)"/>.</summary>
        Task BeforeExecutionAsync(IDictionary<string, object> userContext, CancellationToken token);

        /// <summary>Executes before the <see cref="IDocumentExecuter"/> awaits the <see cref="Task"/> returned by <see cref="IExecutionStrategy.ExecuteAsync(ExecutionContext)"/></summary>
        Task BeforeExecutionAwaitedAsync(IDictionary<string, object> userContext, CancellationToken token);

        /// <summary>Executes after the <see cref="IExecutionStrategy"/> has completed executing the request</summary>
        Task AfterExecutionAsync(IDictionary<string, object> userContext, CancellationToken token);

        /// <summary>Executes before each time the <see cref="IExecutionStrategy"/> awaits the <see cref="Task{TResult}"/> returned by <see cref="IFieldResolver.Resolve"/>. For parallel resolvers, this may execute a single time prior to awaiting multiple tasks.</summary>
        Task BeforeExecutionStepAwaitedAsync(IDictionary<string, object> userContext, CancellationToken token);
    }
}
