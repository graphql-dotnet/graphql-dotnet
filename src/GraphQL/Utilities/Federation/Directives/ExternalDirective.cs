using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    /// <summary>
    /// The @external directive is used to mark a field as owned by another service.
    /// This allows service A to use fields from service B while also knowing at
    /// runtime the types of that field.
    /// <br/>
    /// <see href="https://www.apollographql.com/docs/federation/federation-spec/#external"/>
    /// </summary>
    public class ExternalDirective : Directive
    {
        /// <summary>
        /// Initializes a new instance of the 'external' directive.
        /// </summary>
        public ExternalDirective()
            : base("external", DirectiveLocation.FieldDefinition)
        {
            Description = "The @external directive is used to mark a field as owned by another service.";
        }
    }
}
