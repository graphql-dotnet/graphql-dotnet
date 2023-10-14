using GraphQL.Validation;

namespace GraphQL.Execution
{
    /// <summary>
    /// Provides the ability to log query validation failures and monitor progress of a GraphQL request's execution.
    /// </summary>
    public interface IDocumentExecutionListener
    {
        /// <summary>Executes after document validation is complete. Can be used to log validation failures.</summary>
        Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult);

        /// <summary>Executes after document validation passes, before calling <see cref="IExecutionStrategy.ExecuteAsync(ExecutionContext)"/>.</summary>
        Task BeforeExecutionAsync(IExecutionContext context);

        /// <summary>Executes after the <see cref="IExecutionStrategy"/> has completed executing the request</summary>
        Task AfterExecutionAsync(IExecutionContext context);
    }

    /// <inheritdoc cref="IDocumentExecutionListener"/>
    public abstract class DocumentExecutionListenerBase : IDocumentExecutionListener
    {
        /// <inheritdoc/>
        public virtual Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult) => Task.CompletedTask;

        /// <inheritdoc/>
        public virtual Task BeforeExecutionAsync(IExecutionContext context) => Task.CompletedTask;

        /// <inheritdoc/>
        public virtual Task AfterExecutionAsync(IExecutionContext context) => Task.CompletedTask;
    }
}
