using System;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Known type names
    ///
    /// A GraphQL document is only valid if referenced types (specifically
    /// variable definitions and fragment conditions) are defined by the type schema.
    /// </summary>
    public class KnownTypeNames : IValidationRule
    {
        public Func<string, string[], string> UnknownTypeMessage = (type, suggestedTypes) =>
        {
            var message = $"Unknown type {type}.";
            if (suggestedTypes != null && suggestedTypes.Length > 0)
            {
                message += $" Did you mean {StringUtils.QuotedOrList(suggestedTypes)}?";
            }
            return message;
        };

        public INodeVisitor Validate(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<NamedType>(leave: node =>
                {
                    var type = context.Schema.FindType(node.Name);
                    if (type == null)
                    {
                        var typeNames = context.Schema.AllTypes.Select(x => x.Name).ToArray();
                        var suggestionList = StringUtils.SuggestionList(node.Name, typeNames);
                        context.ReportError(new ValidationError(context.OriginalQuery, "5.4.1.2", UnknownTypeMessage(node.Name, suggestionList), node));
                    }
                });
            });
        }
    }
}
