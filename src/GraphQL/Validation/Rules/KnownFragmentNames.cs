using System.Threading.Tasks;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Known fragment names:
    ///
    /// A GraphQL document is only valid if all <c>...Fragment</c> fragment spreads refer
    /// to fragments defined in the same document.
    /// </summary>
    public class KnownFragmentNames : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly KnownFragmentNames Instance = new KnownFragmentNames();

        /// <inheritdoc/>
        /// <exception cref="KnownFragmentNamesError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new ValueTask<INodeVisitor?>(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLFragmentSpread>((node, context) =>
        {
            var fragment = context.GetFragment(node.Name);
            if (fragment == null)
            {
                context.ReportError(new KnownFragmentNamesError(context, node, node.Name));
            }
        });
    }
}
