using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// An enumeration representing a kind of GraphQL type.
    /// </summary>
    public class __TypeKind : EnumerationGraphType<TypeKind>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="__TypeKind"/> introspection type.
        /// </summary>
        public __TypeKind()
        {
            SetName(nameof(__TypeKind), validate: false);
            Description = "An enum describing what kind of type a given __Type is.";
        }
    }
}
