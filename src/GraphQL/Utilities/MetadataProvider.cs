using System;
using GraphQL.Types;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GraphQL.Utilities
{
    public class MetadataProvider : IProvideMetadata
    {
        public IDictionary<string, object> Metadata { get; set; } = new ConcurrentDictionary<string, object>();

        public TType GetMetadata<TType>(string key, TType defaultValue = default)
        {
            return GetMetadata(key, () => defaultValue);
        }

        public TType GetMetadata<TType>(string key, Func<TType> defaultValueFactory)
        {
            var local = Metadata;
            return local != null && local.TryGetValue(key, out var item) ? (TType)item : defaultValueFactory();
        }

        public bool HasMetadata(string key) => Metadata?.ContainsKey(key) ?? false;
    }
}
