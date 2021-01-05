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
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
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
                        context.ReportError(new KnownTypeNamesError(context, node, suggestionList));
                    }
                });
            }).ToTask();
        }
    }
}
