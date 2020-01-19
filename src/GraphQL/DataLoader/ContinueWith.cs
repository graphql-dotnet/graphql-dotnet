using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public sealed class ContinueWith<TOut, TResult> : IDataLoaderResult<TResult>
    {
        private readonly IDataLoaderResult<TOut> _parent;
        private readonly Func<TOut, Task<TResult>> _func;

        public ContinueWith(IDataLoaderResult<TOut> parent, Func<TOut, Task<TResult>> func)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public async Task<TResult> GetResultAsync(CancellationToken cancellationToken = default)
        {
            return await _func(await _parent.GetResultAsync(cancellationToken));
        }

        async Task<object> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken)
        {
            return (object)(await GetResultAsync(cancellationToken));
        }
    }
}
