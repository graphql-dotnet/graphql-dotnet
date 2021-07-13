#nullable enable

using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Utilities;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Known type names:
    ///
    /// A GraphQL document is only valid if referenced types (specifically
    /// variable definitions and fragment conditions) are defined by the type schema.
    /// </summary>
    public class KnownTypeNames : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly KnownTypeNames Instance = new KnownTypeNames();

        /// <inheritdoc/>
        /// <exception cref="KnownTypeNamesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new MatchingNodeVisitor<NamedType>(leave: (node, context) =>
        {
            var type = context.Schema.AllTypes[node.Name];
            if (type == null)
            {
                var typeNames = context.Schema.AllTypes.Dictionary.Values.Select(x => x.Name).ToArray();
                var suggestionList = StringUtils.SuggestionList(node.Name, typeNames);
                context.ReportError(new KnownTypeNamesError(context, node, suggestionList));
            }
        }).ToTask();
    }
}
