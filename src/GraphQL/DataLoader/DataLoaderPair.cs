using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    //this class could always be unsealed, but it seems pointless, as
    //  the DataLoaderBase class always creates the DataLoaderPair instances
    public sealed class DataLoaderPair<TKey, T> : IDataLoaderResult<T>
    {
        public DataLoaderPair(IDataLoader loader, TKey inputValue)
        {
            Loader = loader ?? throw new ArgumentNullException(nameof(loader));
            Key = inputValue;
        }

        public TKey Key { get; }
        public IDataLoader Loader { get; }
        public T Result { get; private set; }
        public bool IsResultSet { get; private set; }

        public void SetResult(T value)
        {
            if (IsResultSet)
                throw new InvalidOperationException("Result has already been set");
            Result = value;
            IsResultSet = true;
        }

        public async Task<T> GetResultAsync(CancellationToken cancellationToken = default)
        {
            await Loader.DispatchAsync(cancellationToken).ConfigureAwait(false);
            if (!IsResultSet)
                throw new Exception("Result has not been set");
            return Result;
        }

        async Task<object> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken)
        {
            await Loader.DispatchAsync(cancellationToken).ConfigureAwait(false);
            if (!IsResultSet)
                throw new Exception("Result has not been set");
            return Result;
        }
    }
}
