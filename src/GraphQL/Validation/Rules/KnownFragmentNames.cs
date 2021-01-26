using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

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
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<FragmentSpread>(node =>
                {
                    var fragmentName = node.Name;
                    var fragment = context.GetFragment(fragmentName);
                    if (fragment == null)
                    {
                        context.ReportError(new KnownFragmentNamesError(context, node, fragmentName));
                    }
                });
            }).ToTask();
        }
    }
}
