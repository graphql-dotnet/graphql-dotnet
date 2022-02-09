using GraphQL.Utilities.Federation;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The @provides directive is used to annotate the expected returned fieldset from
    /// a field on a base type that is guaranteed to be selectable by the gateway.
    /// <br/>
    /// <see href="https://www.apollographql.com/docs/federation/federation-spec/#provides"/>
    /// </summary>
    public class ProvidesDirective : Directive
    {
        /// <summary>
        /// Initializes a new instance of the 'provides' directive.
        /// </summary>
        public ProvidesDirective()
            : base("provides", DirectiveLocation.FieldDefinition)
        {
            Description = "The @provides directive is used to annotate the expected returned fieldset from a field on a base type that is guaranteed to be selectable by the gateway.";
            Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<FieldSetScalarGraphType>>
            {
                Name = "fields",
                Description = "Expected returned fieldset from a field on a base type that is guaranteed to be selectable by the gateway."
            });
        }
    }
}
