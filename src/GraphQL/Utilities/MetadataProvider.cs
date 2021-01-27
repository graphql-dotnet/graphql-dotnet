using System;
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
        private Dictionary<string, object> _metadata;

        /// <inheritdoc />
        public Dictionary<string, object> Metadata => _metadata ??= new Dictionary<string, object>();

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
