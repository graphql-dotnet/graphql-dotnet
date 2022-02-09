using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    /// <summary>
    /// The @key directive is used to indicate a combination of fields that
    /// can be used to uniquely identify and fetch an object or interface.
    /// <br/>
    /// <see href="https://www.apollographql.com/docs/federation/federation-spec/#key"/>
    /// </summary>
    public class KeyDirective : Directive
    {
        /// <summary>
        /// Initializes a new instance of the 'key' directive.
        /// </summary>
        public KeyDirective()
            : base("key", DirectiveLocation.Object, DirectiveLocation.Interface)
        {
            Description = "The @key directive is used to indicate a combination of fields that can be used to uniquely identify and fetch an object or interface.";
            Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<FieldSetScalarGraphType>>
            {
                Name = "fields",
                Description = "A combination of fields that can be used to uniquely identify and fetch an object or interface."
            });
            Repeatable = true;
        }
    }
}
