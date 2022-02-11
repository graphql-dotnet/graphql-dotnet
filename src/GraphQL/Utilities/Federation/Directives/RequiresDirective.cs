using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    /// <summary>
    /// The @requires directive is used to annotate the required input fieldset from a base type for a resolver.
    /// <br/>
    /// <see href="https://www.apollographql.com/docs/federation/federation-spec/#requires"/>
    /// </summary>
    public class RequiresDirective : Directive
    {
        /// <summary>
        /// Initializes a new instance of the 'requires' directive.
        /// </summary>
        public RequiresDirective()
            : base("requires", DirectiveLocation.FieldDefinition)
        {
            Description = "The @requires directive is used to annotate the required input fieldset from a base type for a resolver.";
            Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<FieldSetScalarGraphType>>
            {
                Name = "fields",
                Description = "Required input fieldset from a base type."
            });
        }
    }
}
