using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No unused fragments:
    ///
    /// A GraphQL document is only valid if all fragment definitions are spread
    /// within operations, or spread within other fragments spread within operations.
    /// </summary>
    public class NoUnusedFragments : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly NoUnusedFragments Instance = new NoUnusedFragments();

        /// <inheritdoc/>
        /// <exception cref="NoUnusedFragmentsError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new ValueTask<INodeVisitor?>(context.Document.FragmentsCount() > 0 ? _nodeVisitor : null);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<GraphQLOperationDefinition>((node, context) => (context.TypeInfo.NoUnusedFragments_OperationDefs ??= new List<GraphQLOperationDefinition>(1)).Add(node)),
            new MatchingNodeVisitor<GraphQLFragmentDefinition>((node, context) => (context.TypeInfo.NoUnusedFragments_FragmentDefs ??= new List<GraphQLFragmentDefinition>()).Add(node)),
            new MatchingNodeVisitor<GraphQLDocument>(leave: (document, context) =>
            {
                var fragmentDefs = context.TypeInfo.NoUnusedFragments_FragmentDefs;
                if (fragmentDefs == null)
                    return;

                var operationDefs = context.TypeInfo.NoUnusedFragments_OperationDefs;
                if (operationDefs == null)
                    return;

                var fragmentNamesUsed = operationDefs
                    .SelectMany(context.GetRecursivelyReferencedFragments)
                    .Select(fragment => fragment.FragmentName.Name)
                    .ToList();

                foreach (var fragmentDef in fragmentDefs)
                {
                    var fragName = fragmentDef.FragmentName.Name;

                    if (!fragmentNamesUsed.Contains(fragName))
                    {
                        context.ReportError(new NoUnusedFragmentsError(context, fragmentDef));
                    }
                }
            })
        );
    }
}
