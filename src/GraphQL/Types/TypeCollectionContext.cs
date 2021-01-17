using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    /// <summary>
    /// Provides a mechanism to resolve graph type instances from their .NET types,
    /// and also to register new graph type instances with their name in the graph type lookup table.
    /// (See <see cref="SchemaTypes"/>.)
    /// </summary>
    internal sealed class TypeCollectionContext
    {
        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="resolver">A delegate which returns an instance of a graph type from its .NET type.</param>
        /// <param name="addType">A delegate which adds a graph type instance to the list of named graph types for the schema.</param>
        internal TypeCollectionContext(Func<Type, IGraphType> resolver, Action<string, IGraphType, TypeCollectionContext> addType)
        {
            ResolveType = resolver;
            AddType = addType;
        }

        /// <summary>
        /// Returns a delegate which returns an instance of a graph type from its .NET type.
        /// </summary>
        internal Func<Type, IGraphType> ResolveType { get; private set; }

        /// <summary>
        /// Returns a delegate which adds a graph type instance to the list of named graph types for the schema.
        /// </summary>
        internal Action<string, IGraphType, TypeCollectionContext> AddType { get; private set; }

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
