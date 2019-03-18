using System.Collections.Concurrent;
using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public class MetadataProvider : IProvideMetadata
    {
        public IDictionary<string, object> Metadata { get; set; } = new ConcurrentDictionary<string, object>();

        public TType GetMetadata<TType>(string key, TType defaultValue = default)
        {
            if (!HasMetadata(key))
            {
                return defaultValue;
            }

            if (Metadata.TryGetValue(key, out var item))
            {
                return (TType) item;
            }

            return defaultValue;
        }

        public bool HasMetadata(string key)
        {
            return Metadata.ContainsKey(key);
        }
    }
}
