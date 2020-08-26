using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class UniqueArgumentNamesError : ValidationError
    {
        public const string PARAGRAPH = "5.4.2";

        public UniqueArgumentNamesError(ValidationContext context, Argument node, Argument otherNode)
            : base(context.OriginalQuery, PARAGRAPH, DuplicateArgMessage(node.Name), node, otherNode)
        {
        }

        internal static string DuplicateArgMessage(string argName)
            => $"There can be only one argument named \"{argName}\".";
    }
}
