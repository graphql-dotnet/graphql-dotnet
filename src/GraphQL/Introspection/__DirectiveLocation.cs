using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Introspection
{
    /// <summary>
    /// An enumeration representing a location that a directive may be placed.
    /// </summary>
    public class __DirectiveLocation : EnumerationGraphType<DirectiveLocation>
    {
        internal static readonly __DirectiveLocation Instance = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="__DirectiveLocation"/> graph type.
        /// </summary>
        public __DirectiveLocation()
        {
            SetName(nameof(__DirectiveLocation), validate: false);
            Description =
                "A Directive can be adjacent to many parts of the GraphQL language, a " +
                "__DirectiveLocation describes one such possible adjacencies.";
        }
    }
}
