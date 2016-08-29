using System;
using System.Collections.Generic;
using GraphQL.Builders;

namespace GraphQL.Types
{
    public abstract class GraphType
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public virtual string CollectTypes(TypeCollectionContext context)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = GetType().Name;
            }

            return Name;
        }

        protected bool Equals(GraphType other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((GraphType)obj);
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// This sucks, find a better way
    /// </summary>
    public class TypeCollectionContext
    {
        public TypeCollectionContext(
            Func<Type, GraphType> resolver,
            Action<string, GraphType, TypeCollectionContext> addType)
        {
            ResolveType = resolver;
            AddType = addType;
        }

        public Func<Type, GraphType> ResolveType { get; private set; }
        public Action<string, GraphType, TypeCollectionContext> AddType { get; private set; }
    }
}
