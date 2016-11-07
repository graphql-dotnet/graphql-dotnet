using System.Threading;
using System.Threading.Tasks;
using GraphQL.Validation;

namespace GraphQL.Execution
{
    public interface IDocumentExecutionListener
    {
        Task AfterValidationAsync(object userContext, IValidationResult validationResult, CancellationToken token);
        Task BeforeExecutionAsync(object userContext, CancellationToken token);
        Task BeforeExecutionAwaitedAsync(object userContext, CancellationToken token);
        Task AfterExecutionAsync(object userContext, CancellationToken token);
    }

    public interface IDocumentExecutionListener<in T>
    {
        Task AfterValidationAsync(T userContext, IValidationResult validationResult, CancellationToken token);
        Task BeforeExecutionAsync(T userContext, CancellationToken token);
        Task BeforeExecutionAwaitedAsync(T userContext, CancellationToken token);
        Task AfterExecutionAsync(T userContext, CancellationToken token);
    }

    public abstract class DocumentExecutionListenerBase<T> : IDocumentExecutionListener<T>, IDocumentExecutionListener
    {
        public virtual Task AfterValidationAsync(T userContext, IValidationResult validationResult, CancellationToken token)
        {
            return TaskExtensions.CompletedTask;
        }

        public virtual Task BeforeExecutionAsync(T userContext, CancellationToken token)
        {
            return TaskExtensions.CompletedTask;
        }

        public virtual Task BeforeExecutionAwaitedAsync(T userContext, CancellationToken token)
        {
            return TaskExtensions.CompletedTask;
        }

        public virtual Task AfterExecutionAsync(T userContext, CancellationToken token)
        {
            return TaskExtensions.CompletedTask;
        }

        Task IDocumentExecutionListener.AfterValidationAsync(object userContext, IValidationResult validationResult, CancellationToken token)
        {
            return AfterValidationAsync((T)userContext, validationResult, token);
        }

        Task IDocumentExecutionListener.BeforeExecutionAsync(object userContext, CancellationToken token)
        {
            return BeforeExecutionAsync((T)userContext, token);
        }

        Task IDocumentExecutionListener.BeforeExecutionAwaitedAsync(object userContext, CancellationToken token)
        {
            return BeforeExecutionAwaitedAsync((T)userContext, token);
        }

        Task IDocumentExecutionListener.AfterExecutionAsync(object userContext, CancellationToken token)
        {
            return AfterExecutionAsync((T)userContext, token);
        }
    }
}
