using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Validation.Errors
{
    public class KnownTypeNamesError : ValidationError
    {
        public const string PARAGRAPH = "5.5.1.2";

        public KnownTypeNamesError(ValidationContext context, NamedType node, string[] suggestedTypes)
            : base(context.OriginalQuery, PARAGRAPH, UnknownTypeMessage(node.Name, suggestedTypes), node)
        {
        }

        internal static string UnknownTypeMessage(string type, string[] suggestedTypes)
        {
            var message = $"Unknown type {type}.";
            if (suggestedTypes != null && suggestedTypes.Length > 0)
            {
                message += $" Did you mean {StringUtils.QuotedOrList(suggestedTypes)}?";
            }
            return message;
        }
    }
}
