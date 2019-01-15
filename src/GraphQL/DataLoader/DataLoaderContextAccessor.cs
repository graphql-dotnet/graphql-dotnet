using System.Threading;

namespace GraphQL.DataLoader
{
    public class DataLoaderContextAccessor : IDataLoaderContextAccessor
    {
        private readonly AsyncLocal<DataLoaderContext> _current = new AsyncLocal<DataLoaderContext>();

        public DataLoaderContext Context
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }
}
