using System;
using System.Linq;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser;
using GraphQLParser.AST;

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
        public KnownArgumentNamesError(ValidationContext context, GraphQLArgument node, FieldType fieldDef, IGraphType parentType)
            : base(context.OriginalQuery!, NUMBER,
                UnknownArgMessage(
                    node.Name,
                    fieldDef.Name,
                    parentType.ToString(),
                    StringUtils.SuggestionList((string)node.Name, fieldDef.Arguments?.List?.Select(q => q.Name))), //TODO:!!!!alloc
                node)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public KnownArgumentNamesError(ValidationContext context, GraphQLArgument node, DirectiveGraphType directive)
            : base(context.OriginalQuery!, NUMBER,
                UnknownDirectiveArgMessage(
                    node.Name,
                    directive.Name,
                    StringUtils.SuggestionList((string)node.Name, directive.Arguments?.Select(q => q.Name))), //TODO:!!!alloc
                node)
        {
        }

        internal static string UnknownArgMessage(ROM argName, string fieldName, string type, string[] suggestedArgs)
        {
            var message = $"Unknown argument '{argName}' on field '{fieldName}' of type '{type}'.";
            if (suggestedArgs != null && suggestedArgs.Length > 0)
            {
                message += $" Did you mean {StringUtils.QuotedOrList(suggestedArgs)}";
            }
            return message;
        }

        internal static string UnknownDirectiveArgMessage(ROM argName, string directiveName, string[] suggestedArgs)
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
