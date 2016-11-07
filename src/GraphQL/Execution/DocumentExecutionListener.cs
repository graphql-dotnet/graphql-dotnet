using System.Threading;
using System.Threading.Tasks;
using GraphQL.Validation;

namespace GraphQL.Execution
{
    public interface IDocumentExecutionListener
    {
        Task AfterValidation(object userContext, IValidationResult validationResult, CancellationToken token);
        Task BeforeExecution(object userContext, CancellationToken token);
        Task BeforeExecutionAwaited(object userContext, CancellationToken token);
        Task AfterExecution(object userContext, CancellationToken token);
    }

    public interface IDocumentExecutionListener<T>
    {
        Task AfterValidation(T userContext, IValidationResult validationResult, CancellationToken token);
        Task BeforeExecution(T userContext, CancellationToken token);
        Task BeforeExecutionAwaited(T userContext, CancellationToken token);
        Task AfterExecution(T userContext, CancellationToken token);
    }

    public abstract class DocumentExecutionListenerBase<T> : IDocumentExecutionListener<T>, IDocumentExecutionListener
    {
        public virtual Task AfterValidation(T userContext, IValidationResult validationResult, CancellationToken token)
        {
            return CompletedTask();
        }

        public virtual Task BeforeExecution(T userContext, CancellationToken token)
        {
            return CompletedTask();
        }

        public virtual Task BeforeExecutionAwaited(T userContext, CancellationToken token)
        {
            return CompletedTask();
        }

        public virtual Task AfterExecution(T userContext, CancellationToken token)
        {
            return CompletedTask();
        }

        private Task CompletedTask()
        {
            object result = null;
            return Task.FromResult(result);
        }

        Task IDocumentExecutionListener.AfterValidation(object userContext, IValidationResult validationResult, CancellationToken token)
        {
            return AfterValidation((T)userContext, validationResult, token);
        }

        Task IDocumentExecutionListener.BeforeExecution(object userContext, CancellationToken token)
        {
            return BeforeExecution((T)userContext, token);
        }

        Task IDocumentExecutionListener.BeforeExecutionAwaited(object userContext, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        Task IDocumentExecutionListener.AfterExecution(object userContext, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}
