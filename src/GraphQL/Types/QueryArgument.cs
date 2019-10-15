using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace GraphQL.Types
{
    public class QueryArgument<TType> : QueryArgument
        where TType : IGraphType
    {
        public QueryArgument()
            : base(typeof(TType))
        {
        }
    }

    [DebuggerDisplay("{Name,nq}: {ResolvedType,nq}")]
    public class QueryArgument : IHaveDefaultValue, IProvideMetadata
    {
        public QueryArgument(IGraphType type)
        {
            ResolvedType = type ?? throw new ArgumentOutOfRangeException(nameof(type), "QueryArgument type is required");
        }

        public QueryArgument(Type type)
        {
            if (type == null || !typeof(IGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "QueryArgument type is required and must derive from IGraphType.");
            }

            Type = type;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public object DefaultValue { get; set; }

        public IGraphType ResolvedType { get; set; }

        public Type Type { get; private set; }

        public IDictionary<string, object> Metadata { get; set; } = new ConcurrentDictionary<string, object>();

        public TType GetMetadata<TType>(string key, TType defaultValue = default)
        {
            var local = Metadata;
            return local != null && local.TryGetValue(key, out var item) ? (TType)item : defaultValue;
        }

        public bool HasMetadata(string key) => Metadata?.ContainsKey(key) ?? false;
    }
}
