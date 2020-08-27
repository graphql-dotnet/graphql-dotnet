using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    public class KnownDirectivesError : ValidationError
    {
        internal const string NUMBER = "5.7.1";

        public KnownDirectivesError(ValidationContext context, Directive node)
            : base(context.OriginalQuery, NUMBER, UnknownDirectiveMessage(node.Name), node)
        {
        }

        public KnownDirectivesError(ValidationContext context, Directive node, DirectiveLocation candidateLocation)
            : base(context.OriginalQuery, NUMBER, MisplacedDirectiveMessage(node.Name, candidateLocation.ToString()), node)
        {
        }

        internal static string UnknownDirectiveMessage(string directiveName)
            => $"Unknown directive \"{directiveName}\".";

        internal static string MisplacedDirectiveMessage(string directiveName, string location)
            => $"Directive \"{directiveName}\" may not be used on {location}.";
    }
}
