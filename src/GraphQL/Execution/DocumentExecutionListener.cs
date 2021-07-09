#nullable enable

using System;
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
        Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult);

        /// <summary>Executes after document validation passes, before calling <see cref="IExecutionStrategy.ExecuteAsync(ExecutionContext)"/>.</summary>
        Task BeforeExecutionAsync(IExecutionContext context);

        /// <summary>Executes before the <see cref="IDocumentExecuter"/> awaits the <see cref="Task"/> returned by <see cref="IExecutionStrategy.ExecuteAsync(ExecutionContext)"/></summary>
        [Obsolete]
        Task BeforeExecutionAwaitedAsync(IExecutionContext context);

        /// <summary>Executes after the <see cref="IExecutionStrategy"/> has completed executing the request</summary>
        Task AfterExecutionAsync(IExecutionContext context);

        /// <summary>Executes before each time the <see cref="IExecutionStrategy"/> awaits the <see cref="Task{TResult}"/> returned by <see cref="IFieldResolver.Resolve"/>. For parallel resolvers, this may execute a single time prior to awaiting multiple tasks.</summary>
        [Obsolete]
        Task BeforeExecutionStepAwaitedAsync(IExecutionContext context);
    }

    /// <inheritdoc cref="IDocumentExecutionListener"/>
    public abstract class DocumentExecutionListenerBase : IDocumentExecutionListener
    {
        /// <inheritdoc/>
        public virtual Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult) => Task.CompletedTask;

        /// <inheritdoc/>
        public virtual Task BeforeExecutionAsync(IExecutionContext context) => Task.CompletedTask;

        /// <inheritdoc/>
        [Obsolete]
        public virtual Task BeforeExecutionAwaitedAsync(IExecutionContext context) => Task.CompletedTask;

        /// <inheritdoc/>
        public virtual Task AfterExecutionAsync(IExecutionContext context) => Task.CompletedTask;

        /// <inheritdoc/>
        [Obsolete]
        public virtual Task BeforeExecutionStepAwaitedAsync(IExecutionContext context) => Task.CompletedTask;
    }
}
