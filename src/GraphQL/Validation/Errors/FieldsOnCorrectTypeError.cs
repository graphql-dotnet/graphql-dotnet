using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.FieldsOnCorrectType"/>
    [Serializable]
    public class FieldsOnCorrectTypeError : ValidationError
    {
        internal const string NUMBER = "5.3.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public FieldsOnCorrectTypeError(ValidationContext context, GraphQLField node, IGraphType type, IEnumerable<string> suggestedTypeNames, IEnumerable<string> suggestedFieldNames)
            : base(context.Document.Source, NUMBER, UndefinedFieldMessage(node.Name.StringValue, type.Name, suggestedTypeNames, suggestedFieldNames), node)
        {
        }

        internal static string UndefinedFieldMessage(
            string fieldName,
            string type,
            IEnumerable<string> suggestedTypeNames,
            IEnumerable<string> suggestedFieldNames)
        {
            var message = $"Cannot query field '{fieldName}' on type '{type}'.";

            if (suggestedTypeNames != null)
            {
                var suggestedTypeNamesList = suggestedTypeNames.ToList();
                if (suggestedTypeNamesList.Count > 0)
                {
                    var suggestions = StringUtils.QuotedOrList(suggestedTypeNamesList);
                    message += $" Did you mean to use an inline fragment on {suggestions}?";
                    return message;
                }
            }

            if (suggestedFieldNames != null)
            {
                var suggestedFieldNamesList = suggestedFieldNames.ToList();
                if (suggestedFieldNamesList.Count > 0)
                {
                    message += $" Did you mean {StringUtils.QuotedOrList(suggestedFieldNamesList)}?";
                }
            }

            return message;
        }
    }
}
