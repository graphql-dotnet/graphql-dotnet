#if NET45
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;
#else
using System.Threading;
#endif

namespace GraphQL.DataLoader
{
    public class DataLoaderContextAccessor : IDataLoaderContextAccessor
    {
#if NET45

        private const string LogicalDataKey = "__DataLoaderContext_Current__";

        public DataLoaderContext Context
        {
            get
            {
                var handle = CallContext.LogicalGetData(LogicalDataKey) as ObjectHandle;
                return handle?.Unwrap() as DataLoaderContext;
            }
            set
            {
                CallContext.LogicalSetData(LogicalDataKey, new ObjectHandle(value));
            }
        }
#else

        private readonly AsyncLocal<DataLoaderContext> _current = new AsyncLocal<DataLoaderContext>();

        public DataLoaderContext Context
        {
            get => _current.Value;
            set => _current.Value = value;
        }

#endif
    }
}
