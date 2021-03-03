using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Possible fragment spread:
    ///
    /// A fragment spread is only valid if the type condition could ever possibly
    /// be <see langword="true"/>: if there is a non-empty intersection of the
    /// possible parent types, and possible types which pass the type condition.
    /// </summary>
    public class PossibleFragmentSpreads : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly PossibleFragmentSpreads Instance = new PossibleFragmentSpreads();

        /// <inheritdoc/>
        /// <exception cref="PossibleFragmentSpreadsError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<InlineFragment>((node, context) =>
            {
                var fragType = context.TypeInfo.GetLastType();
                var parentType = context.TypeInfo.GetParentType().GetNamedType();

                if (fragType != null && parentType != null && !GraphQLExtensions.DoTypesOverlap(fragType, parentType))
                {
                    context.ReportError(new PossibleFragmentSpreadsError(context, node, parentType, fragType));
                }
            }),

            new MatchingNodeVisitor<FragmentSpread>((node, context) =>
            {
                string fragName = node.Name;
                var fragType = getFragmentType(context, fragName);
                var parentType = context.TypeInfo.GetParentType().GetNamedType();

                if (fragType != null && parentType != null && !GraphQLExtensions.DoTypesOverlap(fragType, parentType))
                {
                    context.ReportError(new PossibleFragmentSpreadsError(context, node, parentType, fragType));
                }
            })
        ).ToTask();

        private static IGraphType getFragmentType(ValidationContext context, string name)
        {
            var frag = context.GetFragment(name);
            return frag?.Type?.GraphTypeFromType(context.Schema);
        }
    }
}
