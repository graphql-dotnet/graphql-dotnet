using System.Threading;

namespace GraphQL.DataLoader
{
    /// <inheritdoc cref="IDataLoaderContextAccessor"/>
    public class DataLoaderContextAccessor : IDataLoaderContextAccessor
    {
        private static readonly AsyncLocal<DataLoaderContext> _current = new AsyncLocal<DataLoaderContext>();

        /// <inheritdoc/>
        public DataLoaderContext Context
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }
}
