using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// Used to conditionally include fields or fragments.
    /// </summary>
    public class IncludeDirective : Directive
    {
        /// <summary>
        /// Initializes a new instance of the 'include' directive.
        /// </summary>
        public IncludeDirective()
            : base("include", DirectiveLocation.Field, DirectiveLocation.FragmentSpread, DirectiveLocation.InlineFragment)
        {
            Description = "Directs the executor to include this field or fragment only when the 'if' argument is true.";
            Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<BooleanGraphType>>
            {
                Name = "if",
                Description = "Included when true."
            });
        }
    }
}
