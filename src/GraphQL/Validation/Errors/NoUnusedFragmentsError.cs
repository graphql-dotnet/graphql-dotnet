using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class NoUnusedFragmentsError : ValidationError
    {
        public const string PARAGRAPH = "5.5.1.4";

        public NoUnusedFragmentsError(ValidationContext context, FragmentDefinition node)
            : base(context.OriginalQuery, PARAGRAPH, UnusedFragMessage(node.Name), node)
        {
        }

        internal static string UnusedFragMessage(string fragName)
            => $"Fragment \"{fragName}\" is never used.";
    }
}
