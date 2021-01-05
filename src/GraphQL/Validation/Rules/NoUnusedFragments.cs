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
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var operationDefs = new List<Operation>();
            var fragmentDefs = new List<FragmentDefinition>();

            return new NodeVisitors(
                new MatchingNodeVisitor<Operation>((node, context) => operationDefs.Add(node)),
                new MatchingNodeVisitor<FragmentDefinition>((node, context) => fragmentDefs.Add(node)),
                new MatchingNodeVisitor<Document>(leave: (document, context) =>
                {
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
}
