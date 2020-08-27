using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class NoUnusedFragmentsError : ValidationError
    {
        internal const string NUMBER = "5.5.1.4";

        public NoUnusedFragmentsError(ValidationContext context, FragmentDefinition node)
            : base(context.OriginalQuery, NUMBER, UnusedFragMessage(node.Name), node)
        {
        }

        internal static string UnusedFragMessage(string fragName)
            => $"Fragment \"{fragName}\" is never used.";
    }
}
