using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    /// <summary>
    /// Optional directive for some libraries such as graphql-java that don't have native
    /// support for type extensions in their printer. Apollo Federation supports using an
    /// @extends directive in place of 'extend type' to annotate type references.
    /// </summary>
    public class ExtendsDirective : Directive //TODO: remove?
    {
        /// <summary>
        /// Initializes a new instance of the 'extends' directive.
        /// </summary>
        public ExtendsDirective()
            : base("extends", DirectiveLocation.Object, DirectiveLocation.Interface)
        {
            Description = "Alternative for 'extend type'";
        }
    }
}
