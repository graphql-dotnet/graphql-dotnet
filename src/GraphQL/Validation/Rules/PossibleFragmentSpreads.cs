using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

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
        public static readonly PossibleFragmentSpreads Instance = new();

        /// <inheritdoc/>
        /// <exception cref="PossibleFragmentSpreadsError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<GraphQLInlineFragment>((node, context) =>
            {
                var fragType = context.TypeInfo.GetLastType();
                var parentType = context.TypeInfo.GetParentType()?.GetNamedType();

                if (fragType != null && parentType != null && !GraphQLExtensions.DoTypesOverlap(fragType, parentType))
                {
                    context.ReportError(new PossibleFragmentSpreadsError(context, node, parentType, fragType));
                }
            }),

            new MatchingNodeVisitor<GraphQLFragmentSpread>((node, context) =>
            {
                var fragName = node.FragmentName.Name;
                var fragType = getFragmentType(context, fragName);
                var parentType = context.TypeInfo.GetParentType()?.GetNamedType();

                if (fragType != null && parentType != null && !GraphQLExtensions.DoTypesOverlap(fragType, parentType))
                {
                    context.ReportError(new PossibleFragmentSpreadsError(context, node, parentType, fragType));
                }
            })
        );

        private static IGraphType? getFragmentType(ValidationContext context, ROM name)
        {
            var frag = context.Document.FindFragmentDefinition(name);
            return frag?.TypeCondition.Type.GraphTypeFromType(context.Schema);
        }
    }
}
