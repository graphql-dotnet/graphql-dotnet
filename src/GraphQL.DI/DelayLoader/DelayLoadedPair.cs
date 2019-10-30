using System;
using System.Threading.Tasks;

namespace GraphQL.DI.DelayLoader
{
    //this class could always be unsealed, but it seems pointless, as
    //  the DataLoader class always creates the DelayLoadedPair instances
    public sealed class DelayLoadedPair<TIn, TOut> : IDelayLoadedResult<TOut>
    {
        public DelayLoadedPair(IDelayLoader loader, TIn inputValue)
        {
            Loader = loader ?? throw new ArgumentNullException(nameof(loader));
            InputValue = inputValue;
        }

        public TIn InputValue { get; }
        public IDelayLoader Loader { get; }
        public TOut Result { get; private set; }
        public bool IsResultSet { get; private set; }

        public void SetResult(TOut value)
        {
            if (IsResultSet) throw new InvalidOperationException("Result has already been set");
            Result = value;
            IsResultSet = true;
        }

        public async Task<TOut> GetResultAsync()
        {
            await this.Loader.LoadAsync().ConfigureAwait(false);
            if (!IsResultSet) throw new Exception("Result has not been set");
            return this.Result;
        }

        async Task<object> IDelayLoadedResult.GetResultAsync()
        {
            await this.Loader.LoadAsync().ConfigureAwait(false);
            if (!IsResultSet) throw new Exception("Result has not been set");
            return (object)this.Result;
        }
    }
}
