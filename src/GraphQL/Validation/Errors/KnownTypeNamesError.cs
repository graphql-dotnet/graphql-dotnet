using System;
using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.KnownTypeNames"/>
    [Serializable]
    public class KnownTypeNamesError : ValidationError
    {
        internal const string NUMBER = "5.5.1.2";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public KnownTypeNamesError(ValidationContext context, NamedType node, string[] suggestedTypes)
            : base(context.Document.OriginalQuery!, NUMBER, UnknownTypeMessage(node.Name, suggestedTypes), node)
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
