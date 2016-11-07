using System.Threading;
using System.Threading.Tasks;
using GraphQL.Validation;

namespace GraphQL.Execution
{
    public interface IDocumentExecutionListener
    {
        Task AfterValidation(IValidationResult validationResult, CancellationToken token);
        Task BeforeExecution(CancellationToken token);
        Task BeforeExecutionAwaited(CancellationToken token);
        Task AfterExecution(CancellationToken token);
    }

    public abstract class DocumentExecutionListenerBase : IDocumentExecutionListener
    {
        public virtual Task AfterValidation(IValidationResult validationResult, CancellationToken token)
        {
            return CompletedTask();
        }

        public virtual Task BeforeExecution(CancellationToken token)
        {
            return CompletedTask();
        }

        public virtual Task BeforeExecutionAwaited(CancellationToken token)
        {
            return CompletedTask();
        }

        public virtual Task AfterExecution(CancellationToken token)
        {
            return CompletedTask();
        }

        private Task CompletedTask()
        {
            object result = null;
            return Task.FromResult(result);
        }
    }
}
