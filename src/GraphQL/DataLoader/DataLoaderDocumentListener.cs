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

        public Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult)
            => Task.CompletedTask;

        public Task BeforeExecutionAsync(IExecutionContext context)
        {
            if (_accessor.Context == null)
                _accessor.Context = new DataLoaderContext();

            return Task.CompletedTask;
        }

        public Task BeforeExecutionAwaitedAsync(IExecutionContext context)
            => Task.CompletedTask;

        public Task AfterExecutionAsync(IExecutionContext context)
        {
            _accessor.Context = null;

            return Task.CompletedTask;
        }

        public Task BeforeExecutionStepAwaitedAsync(IExecutionContext context)
            => Task.CompletedTask;
    }
}
