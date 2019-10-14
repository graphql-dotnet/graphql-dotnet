using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    public abstract class GraphType : MetadataProvider, IGraphType
    {
        protected GraphType()
        {
            var name = GetType().Name;
            if (name.EndsWith(nameof(GraphType)))
                Name = name.Substring(0, name.Length - nameof(GraphType).Length);
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public string DeprecationReason { get; set; }

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
