using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    /// <summary>
    /// TODO: This sucks, find a better way
    /// </summary>
    public class TypeCollectionContext
    {
        public TypeCollectionContext(Func<Type, IGraphType> resolver, Action<string, IGraphType, TypeCollectionContext> addType)
        {
            ResolveType = resolver;
            AddType = addType;
        }

        public Func<Type, IGraphType> ResolveType { get; private set; }

        public Action<string, IGraphType, TypeCollectionContext> AddType { get; private set; }

        internal Stack<Type> InFlightRegisteredTypes { get; } = new Stack<Type>();

        internal string CollectTypes(IGraphType type)
        {
            if (type is NonNullGraphType nonNull)
            {
                var innerType = ResolveType(nonNull.Type);
                nonNull.ResolvedType = innerType;
                string name = CollectTypes(innerType);
                AddType(name, innerType, this);
                return name;
            }

            if (type is ListGraphType list)
            {
                var innerType = ResolveType(list.Type);
                list.ResolvedType = innerType;
                string name = CollectTypes(innerType);
                AddType(name, innerType, this);
                return name;
            }

            return type.Name;
        }
    }
}
