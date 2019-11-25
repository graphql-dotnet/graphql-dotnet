using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DI.DelayLoader
{
    public sealed class ContinueWith<TOut, TResult> : IDelayLoadedResult<TResult>
    {
        private readonly IDelayLoadedResult<TOut> _parent;
        private readonly Func<TOut, Task<TResult>> _func;

        public ContinueWith(IDelayLoadedResult<TOut> parent, Func<TOut, Task<TResult>> func)
        {
            _parent = parent;
            _func = func;
        }

        public async Task<TResult> GetResultAsync()
        {
            return await _func(await _parent.GetResultAsync());
        }

        public async Task<TResult> GetResultAsync(CancellationToken cancellationToken)
        {
            return await _func(await _parent.GetResultAsync(cancellationToken));
        }

        async Task<object> IDelayLoadedResult.GetResultAsync()
        {
            return (object)(await GetResultAsync());
        }

        async Task<object> IDelayLoadedResult.GetResultAsync(CancellationToken cancellationToken)
        {
            return (object)(await GetResultAsync(cancellationToken));
        }
    }
}
