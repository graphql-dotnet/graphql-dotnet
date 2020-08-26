using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class UniqueDirectivesPerLocationError : ValidationError
    {
        public const string PARAGRAPH = "5.7.3";

        public UniqueDirectivesPerLocationError(ValidationContext context, Directive node, Directive altNode)
            : base(context.OriginalQuery, PARAGRAPH, DuplicateDirectiveMessage(node.Name), node, altNode)
        {
        }

        internal static string DuplicateDirectiveMessage(string directiveName)
            => $"The directive \"{directiveName}\" can only be used once at this location.";
    }
}
