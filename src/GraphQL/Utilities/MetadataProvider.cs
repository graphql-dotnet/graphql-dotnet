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
        /// <inheritdoc />
        public AppliedDirectives AppliedDirectives { get; set; } = new AppliedDirectives();

        /// <inheritdoc />
        public IDictionary<string, object> Metadata { get; set; } = new ConcurrentDictionary<string, object>();

        /// <inheritdoc />
        public TType GetMetadata<TType>(string key, TType defaultValue = default)
        {
            var local = Metadata;
            return local != null && local.TryGetValue(key, out object item) ? (TType)item : defaultValue;
        }

        /// <inheritdoc />
        public TType GetMetadata<TType>(string key, Func<TType> defaultValueFactory)
        {
            var local = Metadata;
            return local != null && local.TryGetValue(key, out object item) ? (TType)item : defaultValueFactory();
        }

        /// <inheritdoc />
        public bool HasMetadata(string key) => Metadata?.ContainsKey(key) ?? false;
    }
}
