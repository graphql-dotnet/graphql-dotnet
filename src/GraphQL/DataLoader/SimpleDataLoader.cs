using System;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public class SimpleDataLoader<T> : IDataLoader, IDataLoader<T>, IDataLoaderResult<T>
    {
        private readonly Func<CancellationToken, Task<T>> _loader;
        private Task<T> _result;

        public SimpleDataLoader(Func<CancellationToken, Task<T>> loader)
        {
            _loader = loader;
        }

        public Task DispatchAsync(CancellationToken cancellationToken = default) => GetResultAsync(cancellationToken);

        public Task<T> GetResultAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_result != null)
                return _result;

            lock (this)
            {
                if (_result != null)
                    return _result;

                return (_result = _loader(cancellationToken));
            }
        }

        public IDataLoaderResult<T> LoadAsync() => this;

        async Task<object> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken) => await GetResultAsync(cancellationToken);
    }
}
