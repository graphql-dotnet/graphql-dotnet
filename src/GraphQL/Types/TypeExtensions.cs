using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// Provides helper methods for locating a graph type within a schema from the AST type.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Searches a schema for a graph type specified by an AST type after unwrapping any
        /// <see cref="GraphQLNonNullType"/> or <see cref="GraphQLListType"/> layers. If the type cannot be
        /// found, returns <see langword="null"/>.
        /// </summary>
        /// <param name="type">The AST type to search for.</param>
        /// <param name="schema">The schema to search within.</param>
        public static IGraphType? NamedGraphTypeFromType(this GraphQLType type, ISchema schema) => type switch
        {
            GraphQLNonNullType nonnull => NamedGraphTypeFromType(nonnull.Type, schema),
            GraphQLListType list => NamedGraphTypeFromType(list.Type, schema),
            GraphQLNamedType named => schema.AllTypes[named.Name],
            _ => null
        };

        /// <summary>
        /// Searches a schema for a graph type specified by an AST type. If the type
        /// cannot be found, returns <see langword="null"/>.
        /// </summary>
        /// <param name="type">The AST type to search for.</param>
        /// <param name="schema">The schema to search within.</param>
        public static IGraphType? GraphTypeFromType(this GraphQLType type, ISchema schema)
        {
            if (type is GraphQLNonNullType nonnull)
            {
                var ofType = GraphTypeFromType(nonnull.Type, schema);
                return ofType == null
                    ? null
                    : new NonNullGraphType(ofType);
            }

            if (type is GraphQLListType list)
            {
                var ofType = GraphTypeFromType(list.Type, schema);
                return ofType == null
                    ? null
                    : new ListGraphType(ofType);
            }

            return type is GraphQLNamedType named
                ? schema.AllTypes[named.Name]
                : null;
        }

        /// <summary>
        /// Returns the name of an AST type after unwrapping any <see cref="GraphQLNonNullType"/> or <see cref="GraphQLListType"/> layers.
        /// </summary>
        public static string Name(this GraphQLType type) => type switch
        {
            GraphQLNonNullType nonnull => Name(nonnull.Type),
            GraphQLListType list => Name(list.Type),
            GraphQLNamedType named => named.Name.StringValue, //ISSUE:allocation but used only on error paths
            _ => throw new NotSupportedException($"Unknown type {type}")
        };

        /// <summary>
        /// Returns the formatted GraphQL type name of the AST type,
        /// using brackets and exclamation points as necessary to
        /// indicate lists or non-null types, respectively.
        /// </summary>
        public static string FullName(this GraphQLType type) => type switch
        {
            GraphQLNonNullType nonnull => $"{FullName(nonnull.Type)}!",
            GraphQLListType list => $"[{FullName(list.Type)}]",
            GraphQLNamedType named => named.Name.StringValue, //ISSUE:allocation
            _ => throw new NotSupportedException($"Unknown type {type}")
        };
    }
}
