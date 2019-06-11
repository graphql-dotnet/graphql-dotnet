using GraphQL.Resolvers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace GraphQL.Types
{
    public interface IFieldType : IHaveDefaultValue, IProvideMetadata
    {
        string Name { get; set; }
        string Description { get; set; }
        string DeprecationReason { get; set; }
        QueryArguments Arguments { get; set; }
    }

    [DebuggerDisplay("{Name,nq}: {ResolvedType,nq}")]
    public class FieldType : IFieldType
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public object DefaultValue { get; set; }
        public Type Type { get; set; }
        public IGraphType ResolvedType { get; set; }
        public QueryArguments Arguments { get; set; }
        public IFieldResolver Resolver { get; set; }
        public IDictionary<string, object> Metadata { get; set; } = new ConcurrentDictionary<string, object>();

        public TType GetMetadata<TType>(string key, TType defaultValue = default)
        {
            var local = Metadata;
            return local != null && local.TryGetValue(key, out var item) ? (TType)item : defaultValue;
        }

        public bool HasMetadata(string key) => Metadata?.ContainsKey(key) ?? false;
    }
}
