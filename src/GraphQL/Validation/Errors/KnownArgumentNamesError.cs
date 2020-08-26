using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Validation.Errors
{
    public class KnownArgumentNamesError : ValidationError
    {
        public const string PARAGRAPH = "5.4.1";

        public KnownArgumentNamesError(ValidationContext context, Argument node, FieldType fieldDef, IGraphType parentType)
            : base(context.OriginalQuery, PARAGRAPH,
                UnknownArgMessage(
                    node.Name,
                    fieldDef.Name,
                    context.Print(parentType),
                    StringUtils.SuggestionList(node.Name, fieldDef.Arguments?.Select(q => q.Name))),
                node)
        {
        }

        public KnownArgumentNamesError(ValidationContext context, Argument node, DirectiveGraphType directive)
            : base(context.OriginalQuery, PARAGRAPH,
                UnknownDirectiveArgMessage(
                    node.Name,
                    directive.Name,
                    StringUtils.SuggestionList(node.Name, directive.Arguments?.Select(q => q.Name))),
                node)
        {
        }

        internal static string UnknownArgMessage(string argName, string fieldName, string type, string[] suggestedArgs)
        {
            var message = $"Unknown argument \"{argName}\" on field \"{fieldName}\" of type \"{type}\".";
            if (suggestedArgs != null && suggestedArgs.Length > 0)
            {
                message += $" Did you mean {StringUtils.QuotedOrList(suggestedArgs)}";
            }
            return message;
        }

        internal static string UnknownDirectiveArgMessage(string argName, string directiveName, string[] suggestedArgs)
        {
            var message = $"Unknown argument \"{argName}\" on directive \"{directiveName}\".";
            if (suggestedArgs != null && suggestedArgs.Length > 0)
            {
                message += $" Did you mean {StringUtils.QuotedOrList(suggestedArgs)}";
            }
            return message;
        }

    }
}
