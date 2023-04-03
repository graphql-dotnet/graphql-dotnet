using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// Used to declare element of a GraphQL schema as deprecated.
    /// </summary>
    public class DeprecatedDirective : Directive
    {
        /// <inheritdoc/>
        public override bool? Introspectable => true;

        /// <summary>
        /// Initializes a new instance of the 'deprecated' directive.
        /// </summary>
        public DeprecatedDirective()
            : base("deprecated", DirectiveLocation.FieldDefinition, DirectiveLocation.EnumValue)
        {
            Description = "Marks an element of a GraphQL schema as no longer supported.";
            Arguments = new QueryArguments(new QueryArgument<StringGraphType>
            {
                Name = "reason",
                Description =
                    "Explains why this element was deprecated, usually also including a " +
                    "suggestion for how to access supported similar data. Formatted " +
                    "in [Markdown](https://daringfireball.net/projects/markdown/).",
                DefaultValue = "No longer supported"
            });
        }
    }
}
