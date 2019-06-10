using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public abstract class GraphType : IGraphType
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string DeprecationReason { get; set; }

        public IDictionary<string, object> Metadata { get; set; } = new ConcurrentDictionary<string, object>();

        public TType GetMetadata<TType>(string key, TType defaultValue = default)
        {
            var local = Metadata;
            return local != null && local.TryGetValue(key, out var item) ? (TType)item : defaultValue;
        }

        public bool HasMetadata(string key) => Metadata?.ContainsKey(key) ?? false;

        public virtual string CollectTypes(TypeCollectionContext context)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = GetType().Name;
            }

            return Name;
        }

        public override string ToString() =>
            string.IsNullOrWhiteSpace(Name)
                ? GetType().Name
                : Name;

        protected bool Equals(IGraphType other) => string.Equals(Name, other.Name);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((IGraphType)obj);
        }

        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// This sucks, find a better way
    /// </summary>
    public class TypeCollectionContext
    {
        public TypeCollectionContext(
            Func<Type, IGraphType> resolver,
            Action<string, IGraphType, TypeCollectionContext> addType)
        {
            ResolveType = resolver;
            AddType = addType;
        }

        public Func<Type, IGraphType> ResolveType { get; private set; }
        public Action<string, IGraphType, TypeCollectionContext> AddType { get; private set; }
    }
}
