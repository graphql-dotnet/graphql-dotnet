using System.Threading;
using System.Threading.Tasks;
using DataLoader;
using GraphQL.Execution;
using GraphQL.Validation;

namespace GraphQL.DataLoader
{
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
            return TaskExtensions.CompletedTask;
        }

        public Task BeforeResolveLevelAwaitedAsync(object userContext, CancellationToken token)
        {
            var context = _accessor.Context;
            context.DispatchAll(token);

            return TaskExtensions.CompletedTask;
        }
    }
}
