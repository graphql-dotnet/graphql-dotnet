using System.Threading.Tasks;

namespace GraphQL.DI.DelayLoader
{
    public interface IDelayLoadedResult
    {
        Task<object> GetResultAsync();
    }

    public interface IDelayLoadedResult<TOut> : IDelayLoadedResult
    {
        new Task<TOut> GetResultAsync();
    }
}
