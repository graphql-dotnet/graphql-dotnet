using System.Threading;
using System.Threading.Tasks;
using GraphQL.Validation;
using GraphQL.Resolvers;

namespace GraphQL.Execution
{
    /// <summary>
    /// Provides the ability to log query validation failures and monitor progress of a GraphQL request's execution.
    /// </summary>
    public interface IDocumentExecutionListener
    {
        /// <summary>Executes after document validation is complete. Can be used to log validation failures.</summary>
        Task AfterValidationAsync(object userContext, IValidationResult validationResult, CancellationToken token);
        /// <summary>Executes after document validation passes, before calling <see cref="IExecutionStrategy.ExecuteAsync(ExecutionContext)"/>.</summary>
        Task BeforeExecutionAsync(object userContext, CancellationToken token);

        /// <summary>Executes before the <see cref="IDocumentExecuter"/> awaits the <see cref="Task"/> returned by <see cref="IExecutionStrategy.ExecuteAsync(ExecutionContext)"/></summary>
        Task BeforeExecutionAwaitedAsync(object userContext, CancellationToken token);

        /// <summary>Executes after the <see cref="IExecutionStrategy"/> has completed executing the request</summary>
        Task AfterExecutionAsync(object userContext, CancellationToken token);

        /// <summary>Executes before each time the <see cref="IExecutionStrategy"/> awaits the <see cref="Task{TResult}"/> returned by <see cref="IFieldResolver.Resolve"/></summary>
        Task BeforeExecutionStepAwaitedAsync(object userContext, CancellationToken token);
    }

    /// <inheritdoc cref="IDocumentExecutionListener"/>
    public interface IDocumentExecutionListener<in T>
    {
        /// <inheritdoc cref="IDocumentExecutionListener.AfterValidationAsync(object, IValidationResult, CancellationToken)"/>
        Task AfterValidationAsync(T userContext, IValidationResult validationResult, CancellationToken token);

        /// <inheritdoc cref="IDocumentExecutionListener.BeforeExecutionAsync(object, CancellationToken)"/>
        Task BeforeExecutionAsync(T userContext, CancellationToken token);

        /// <inheritdoc cref="IDocumentExecutionListener.BeforeExecutionAwaitedAsync(object, CancellationToken)"/>
        Task BeforeExecutionAwaitedAsync(T userContext, CancellationToken token);

        /// <inheritdoc cref="IDocumentExecutionListener.AfterExecutionAsync(object, CancellationToken)"/>
        Task AfterExecutionAsync(T userContext, CancellationToken token);

        /// <inheritdoc cref="IDocumentExecutionListener.BeforeExecutionStepAwaitedAsync(object, CancellationToken)"/>
        Task BeforeExecutionStepAwaitedAsync(T userContext, CancellationToken token);
    }

    /// <inheritdoc cref="IDocumentExecutionListener"/>
    public abstract class DocumentExecutionListenerBase<T> : IDocumentExecutionListener<T>, IDocumentExecutionListener
    {
        public virtual Task AfterValidationAsync(T userContext, IValidationResult validationResult, CancellationToken token) => Task.CompletedTask;

        public virtual Task BeforeExecutionAsync(T userContext, CancellationToken token) => Task.CompletedTask;

        public virtual Task BeforeExecutionAwaitedAsync(T userContext, CancellationToken token) => Task.CompletedTask;

        public virtual Task AfterExecutionAsync(T userContext, CancellationToken token) => Task.CompletedTask;

        public virtual Task BeforeExecutionStepAwaitedAsync(T userContext, CancellationToken token) => Task.CompletedTask;

        Task IDocumentExecutionListener.AfterValidationAsync(object userContext, IValidationResult validationResult, CancellationToken token) => AfterValidationAsync((T)userContext, validationResult, token);

        Task IDocumentExecutionListener.BeforeExecutionAsync(object userContext, CancellationToken token) => BeforeExecutionAsync((T)userContext, token);

        Task IDocumentExecutionListener.BeforeExecutionAwaitedAsync(object userContext, CancellationToken token) => BeforeExecutionAwaitedAsync((T)userContext, token);

        Task IDocumentExecutionListener.AfterExecutionAsync(object userContext, CancellationToken token) => AfterExecutionAsync((T)userContext, token);

        Task IDocumentExecutionListener.BeforeExecutionStepAwaitedAsync(object userContext, CancellationToken token) => BeforeExecutionStepAwaitedAsync((T)userContext, token);
    }
}
