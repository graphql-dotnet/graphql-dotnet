using GraphQL.Types;
using GraphQL.Utilities;
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
            : base(context.Document.Source, NUMBER,
                UnknownArgMessage(
                    node.Name.StringValue,
                    fieldDef.Name,
                    parentType.ToString()!,
                    StringUtils.SuggestionList(node.Name.StringValue, fieldDef.Arguments?.List?.Select(q => q.Name))), //ISSUE:allocation
                node)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public KnownArgumentNamesError(ValidationContext context, GraphQLArgument node, Directive directive)
            : base(context.Document.Source, NUMBER,
                UnknownDirectiveArgMessage(
                    node.Name.StringValue,
                    directive.Name,
                    StringUtils.SuggestionList(node.Name.StringValue, directive.Arguments?.Select(q => q.Name))), //ISSUE:allocation
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
