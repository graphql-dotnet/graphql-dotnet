using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// Provides helper methods for locating a graph type within a schema from the AST type.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Searches a schema for a graph type specified by an AST type. If the type
        /// cannot be found, returns <see langword="null"/>.
        /// </summary>
        /// <param name="type">The AST type to search for.</param>
        /// <param name="schema">The schema to search within.</param>
        public static IGraphType GraphTypeFromType(this IType type, ISchema schema)
        {
            if (type == null)
                return null;

            if (type is NonNullType nonnull)
            {
                var ofType = GraphTypeFromType(nonnull.Type, schema);
                if (ofType == null)
                {
                    return null;
                }
                var nonnullGraphType = typeof(NonNullGraphType<>).MakeGenericType(ofType.GetType());
                var instance = (NonNullGraphType)Activator.CreateInstance(nonnullGraphType);
                instance.ResolvedType = ofType;
                return instance;
            }

            if (type is ListType list)
            {
                var ofType = GraphTypeFromType(list.Type, schema);
                if (ofType == null)
                {
                    return null;
                }
                var listGraphType = typeof(ListGraphType<>).MakeGenericType(ofType.GetType());
                var instance = (ListGraphType)Activator.CreateInstance(listGraphType);
                instance.ResolvedType = ofType;
                return instance;
            }

            if (type is NamedType named)
            {
                return schema.FindType(named.Name);
            }

            return null;
        }

        /// <summary>
        /// Returns the name of an AST type after unwrapping any <see cref="NonNullType"/> or <see cref="ListType"/> layers.
        /// </summary>
        public static string Name(this IType type) => type switch
        {
            NonNullType nonnull => Name(nonnull.Type),
            ListType list => Name(list.Type),
            _ => ((NamedType)type).Name,
        };

        /// <summary>
        /// Returns the formatted GraphQL type name of the AST type, using brackets and exclamation points as necessary to
        /// indicate lists or non-null types, respectively.
        /// </summary>
        public static string FullName(this IType type) => type switch
        {
            NonNullType nonnull => $"{FullName(nonnull.Type)}!",
            ListType list => $"[{FullName(list.Type)}]",
            _ => ((NamedType)type).Name
        };
    }
}
