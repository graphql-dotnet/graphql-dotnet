using System;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.KnownArgumentNames"/>
    [Serializable]
    public class KnownArgumentNamesError : ValidationError
    {
        internal const string NUMBER = "5.4.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public KnownArgumentNamesError(ValidationContext context, Argument node, FieldType fieldDef, IGraphType parentType)
            : base(context.Document.OriginalQuery!, NUMBER,
                UnknownArgMessage(
                    node.Name,
                    fieldDef.Name,
                    parentType.ToString(),
                    StringUtils.SuggestionList(node.Name, fieldDef.Arguments?.List?.Select(q => q.Name))),
                node)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public KnownArgumentNamesError(ValidationContext context, Argument node, DirectiveGraphType directive)
            : base(context.Document.OriginalQuery!, NUMBER,
                UnknownDirectiveArgMessage(
                    node.Name,
                    directive.Name,
                    StringUtils.SuggestionList(node.Name, directive.Arguments?.Select(q => q.Name))),
                node)
        {
        }

        internal static string UnknownArgMessage(string argName, string fieldName, string type, string[] suggestedArgs)
        {
            var message = $"Unknown argument '{argName}' on field '{fieldName}' of type '{type}'.";
            if (suggestedArgs != null && suggestedArgs.Length > 0)
            {
                message += $" Did you mean {StringUtils.QuotedOrList(suggestedArgs)}";
            }
            return message;
        }

        internal static string UnknownDirectiveArgMessage(string argName, string directiveName, string[] suggestedArgs)
        {
            var message = $"Unknown argument '{argName}' on directive '{directiveName}'.";
            if (suggestedArgs != null && suggestedArgs.Length > 0)
            {
                message += $" Did you mean {StringUtils.QuotedOrList(suggestedArgs)}";
            }
            return message;
        }
    }
}
