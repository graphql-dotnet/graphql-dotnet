using System;
using System.Collections.Generic;
using GraphQL.Builders;

namespace GraphQL.Types
{
    public abstract class GraphType : IGraphType
    {
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

        protected bool Equals(IGraphType other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((IGraphType)obj);
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }

        public virtual void CollectType(Action<IGraphType> addType)
        {
            addType(this);
        }
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
