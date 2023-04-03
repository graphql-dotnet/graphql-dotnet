using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// Used to declare element of a GraphQL schema as deprecated.
    /// </summary>
    public class DeprecatedDirective : Directive
    {
        private static readonly DirectiveLocation[] _officialLocations = new[] { DirectiveLocation.FieldDefinition, DirectiveLocation.EnumValue };
        private static readonly DirectiveLocation[] _draftLocations = new[] { DirectiveLocation.FieldDefinition, DirectiveLocation.EnumValue, DirectiveLocation.ArgumentDefinition, DirectiveLocation.InputFieldDefinition };

        /// <inheritdoc/>
        public override bool? Introspectable => true;

        /// <summary>
        /// Initializes a new instance of the 'deprecated' directive.
        /// </summary>
        public DeprecatedDirective()
            : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the 'deprecated' directive.
        /// </summary>
        /// <param name="deprecationOfInputValues">
        /// Allows deprecation of input values - arguments on a field or input fields on an input type.
        /// This feature is from a working draft of the specification.
        /// </param>
        public DeprecatedDirective(bool deprecationOfInputValues)
            : base("deprecated", deprecationOfInputValues ? _draftLocations : _officialLocations)
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
