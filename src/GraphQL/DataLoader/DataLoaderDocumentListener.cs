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
            return Task.CompletedTask;
        }

        public Task BeforeExecutionAsync(object userContext, CancellationToken token)
        {
            if (_accessor.Context == null)
                _accessor.Context = new DataLoaderContext();

            return Task.CompletedTask;
        }

        public Task BeforeExecutionAwaitedAsync(object userContext, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public Task AfterExecutionAsync(object userContext, CancellationToken token)
        {
            _accessor.Context = null;

            return Task.CompletedTask;
        }

        public Task BeforeExecutionStepAwaitedAsync(object userContext, CancellationToken token)
        {
            var context = _accessor.Context;
            return context.DispatchAllAsync(token);
        }
    }
}
