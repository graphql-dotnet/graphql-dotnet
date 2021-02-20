using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

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
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => context.Document.Fragments.Count > 0 ? _nodeVisitor : null;

        private static readonly Task<INodeVisitor> _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<Operation>((node, context) => (context.TypeInfo.NoUnusedFragments_OperationDefs ??= new List<Operation>(1)).Add(node)),
            new MatchingNodeVisitor<FragmentDefinition>((node, context) => (context.TypeInfo.NoUnusedFragments_FragmentDefs ??= new List<FragmentDefinition>()).Add(node)),
            new MatchingNodeVisitor<Document>(leave: (document, context) =>
            {
                var fragmentDefs = context.TypeInfo.NoUnusedFragments_FragmentDefs;
                if (fragmentDefs == null)
                    return;
                var operationDefs = context.TypeInfo.NoUnusedFragments_OperationDefs;

                var fragmentNamesUsed = operationDefs
                    .SelectMany(context.GetRecursivelyReferencedFragments)
                    .Select(fragment => fragment.Name)
                    .ToList();

                foreach (var fragmentDef in fragmentDefs)
                {
                    var fragName = fragmentDef.Name;

                    if (!fragmentNamesUsed.Contains(fragName))
                    {
                        context.ReportError(new NoUnusedFragmentsError(context, fragmentDef));
                    }
                }
            })
        ).ToTask();
    }
}
