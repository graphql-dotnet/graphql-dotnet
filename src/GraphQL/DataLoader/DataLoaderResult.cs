using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public class DataLoaderResult<T> : IDataLoaderResult<T>
    {
        private readonly Task<T> _result;

        public DataLoaderResult(Task<T> result)
        {
            _result = result ?? throw new ArgumentNullException(nameof(result));
        }

        public DataLoaderResult(T result)
        {
            _result = Task.FromResult(result);
        }

        public Task<T> GetResultAsync(CancellationToken cancellationToken = default) => _result;

        async Task<object> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken) => await GetResultAsync(cancellationToken);
    }
}
