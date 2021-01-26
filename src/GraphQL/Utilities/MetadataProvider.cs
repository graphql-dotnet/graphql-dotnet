using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Default implementation of <see cref="IProvideMetadata"/>. This is the base class for numerous
    /// descendants like <see cref="GraphType"/>, <see cref="FieldType"/>, <see cref="Schema"/> and others.
    /// </summary>
    public class MetadataProvider : IProvideMetadata
    {
        private readonly object _metadataLock = new object();
        private IDictionary<string, object> _metadata;

        /// <inheritdoc />
        public IDictionary<string, object> Metadata
        {
            get
            {
                if (_metadata == null)
                {
                    lock (_metadataLock)
                    {
                        if (_metadata == null)
                        {
                            _metadata = new ConcurrentDictionary<string, object>();
                        }
                    }
                }

                return _metadata;
            }
        }

        /// <inheritdoc />
        public TType GetMetadata<TType>(string key, TType defaultValue = default)
        {
            var local = _metadata;
            return local != null && local.TryGetValue(key, out object item) ? (TType)item : defaultValue;
        }

        /// <inheritdoc />
        public TType GetMetadata<TType>(string key, Func<TType> defaultValueFactory)
        {
            var local = _metadata;
            return local != null && local.TryGetValue(key, out object item) ? (TType)item : defaultValueFactory();
        }

        /// <inheritdoc />
        public bool HasMetadata(string key) => _metadata?.ContainsKey(key) ?? false;
    }
}
