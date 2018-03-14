using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Validation;

namespace GraphQL.DataLoader
{
    /// <summary>
    /// Used to manage the <seealso cref="DataLoaderContext"/>
    /// and automatically dispatch data loader operations at each execution step.
    /// </summary>
    public class DataLoaderDocumentListener : IDocumentExecutionListener
    {
        private readonly IDataLoaderContextAccessor _accessor;

        public DataLoaderDocumentListener(IDataLoaderContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public Task AfterValidationAsync(object userContext, IValidationResult validationResult, CancellationToken token)
        {
            return TaskExtensions.CompletedTask;
        }

        public Task BeforeExecutionAsync(object userContext, CancellationToken token)
        {
            if (_accessor.Context == null)
                _accessor.Context = new DataLoaderContext();

            return TaskExtensions.CompletedTask;
        }

        public Task BeforeExecutionAwaitedAsync(object userContext, CancellationToken token)
        {
            return TaskExtensions.CompletedTask;
        }

        public Task AfterExecutionAsync(object userContext, CancellationToken token)
        {
            _accessor.Context = null;

            return TaskExtensions.CompletedTask;
        }

        public Task BeforeExecutionStepAwaitedAsync(object userContext, CancellationToken token)
        {
            var context = _accessor.Context;
            context.DispatchAll(token);

            return TaskExtensions.CompletedTask;
        }
    }
}
