using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No unused fragments:
    ///
    /// A GraphQL document is only valid if all fragment definitions are spread
    /// within operations, or spread within other fragment spreads within operations.
    /// </summary>
    public class NoUnusedFragments : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly NoUnusedFragments Instance = new();

        /// <inheritdoc/>
        /// <exception cref="NoUnusedFragmentsError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(context.Document.FragmentsCount() > 0 ? _nodeVisitor : null);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<GraphQLOperationDefinition>((node, context) => (context.TypeInfo.NoUnusedFragments_OperationDefs ??= new(1)).Add(node)),
            new MatchingNodeVisitor<GraphQLFragmentDefinition>((node, context) => (context.TypeInfo.NoUnusedFragments_FragmentDefs ??= new()).Add(node)),
            new MatchingNodeVisitor<GraphQLDocument>(leave: (document, context) =>
            {
                var fragmentDefs = context.TypeInfo.NoUnusedFragments_FragmentDefs;
                if (fragmentDefs == null)
                    return;

                var operationDefs = context.TypeInfo.NoUnusedFragments_OperationDefs;
                if (operationDefs == null)
                    return;

                var fragmentsUsed = context.GetRecursivelyReferencedFragments(operationDefs);

                foreach (var fragmentDef in fragmentDefs)
                {
                    if (fragmentsUsed == null || !fragmentsUsed.Contains(fragmentDef))
                    {
                        context.ReportError(new NoUnusedFragmentsError(context, fragmentDef));
                    }
                }
            })
        );
    }
}
