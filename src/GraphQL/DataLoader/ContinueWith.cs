using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public sealed class ContinueWith<T, TResult> : IDataLoaderResult<TResult>
    {
        private readonly IDataLoaderResult<T> _parent;
        private readonly Func<T, CancellationToken, Task<TResult>> _func;

        public ContinueWith(IDataLoaderResult<T> parent, Func<T, Task<TResult>> func)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            if (func == null) throw new ArgumentNullException(nameof(func));
            _func = (value, token) => func(value);
        }

        public ContinueWith(IDataLoaderResult<T> parent, Func<T, CancellationToken, Task<TResult>> func)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public async Task<TResult> GetResultAsync(CancellationToken cancellationToken = default)
        {
            return await _func(await _parent.GetResultAsync(cancellationToken), cancellationToken);
        }

        async Task<object> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken)
        {
            return (object)(await GetResultAsync(cancellationToken));
        }
    }
}
