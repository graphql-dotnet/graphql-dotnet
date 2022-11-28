using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// Used to conditionally skip (exclude) fields or fragments.
    /// </summary>
    public class SkipDirective : Directive
    {
        /// <summary>
        /// Initializes a new instance of the 'skip' directive.
        /// </summary>
        public SkipDirective()
            : base("skip", DirectiveLocation.Field, DirectiveLocation.FragmentSpread, DirectiveLocation.InlineFragment)
        {
            Description = "Directs the executor to skip this field or fragment when the 'if' argument is true.";
            Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<BooleanGraphType>>
            {
                Name = "if",
                Description = "Skipped when true."
            });
        }
    }
}
