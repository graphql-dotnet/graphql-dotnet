using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DI.DelayLoader
{
    public interface IDelayLoadedResult
    {
        Task<object> GetResultAsync();
        Task<object> GetResultAsync(CancellationToken cancellationToken);
    }

    public interface IDelayLoadedResult<TOut> : IDelayLoadedResult
    {
        new Task<TOut> GetResultAsync();
        new Task<TOut> GetResultAsync(CancellationToken cancellationToken);
    }
}
