using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    [Serializable]
    public class UniqueDirectivesPerLocationError : ValidationError
    {
        internal const string NUMBER = "5.7.3";

        public UniqueDirectivesPerLocationError(ValidationContext context, Directive node, Directive altNode)
            : base(context.OriginalQuery, NUMBER, DuplicateDirectiveMessage(node.Name), node, altNode)
        {
        }

        internal static string DuplicateDirectiveMessage(string directiveName)
            => $"The directive \"{directiveName}\" can only be used once at this location.";
    }
}
