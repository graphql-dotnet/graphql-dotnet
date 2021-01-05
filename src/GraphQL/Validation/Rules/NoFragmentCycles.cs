using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No fragment cycles:
    ///
    /// A GraphQL document is only valid if it does not contain fragment cycles.
    /// </summary>
    public class NoFragmentCycles : IValidationRule
    {
        private sealed class NoFragmentCyclesData
        {
            // Tracks already visited fragments to maintain O(N) and to ensure that cycles are not redundantly reported.
            public LightweightCache<string, bool> VisitedFrags { get; set; } = new LightweightCache<string, bool>(key => false);

            // Array of AST nodes used to produce meaningful errors
            public Stack<FragmentSpread> SpreadPath = new Stack<FragmentSpread>();

            // Position in the spread path
            public LightweightCache<string, int> SpreadPathIndexByName = new LightweightCache<string, int>(key => -1);
        }

        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly NoFragmentCycles Instance = new NoFragmentCycles();

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<Document>((_, context) => context.Set<NoFragmentCycles>(new NoFragmentCyclesData()));
                _.Match<FragmentDefinition>((node, context) =>
                {
                    var data = context.Get<NoFragmentCycles, NoFragmentCyclesData>();
                    if (!data.VisitedFrags[node.Name])
                    {
                        detectCycleRecursive(node, data.SpreadPath, data.VisitedFrags, data.SpreadPathIndexByName, context);
                    }
                });
            },
            shouldRun: context => context.Document.Fragments.Count > 0
            ).ToTask();

        /// <inheritdoc/>
        /// <exception cref="NoFragmentCyclesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;

        private static void detectCycleRecursive(
            FragmentDefinition fragment,
            Stack<FragmentSpread> spreadPath,
            LightweightCache<string, bool> visitedFrags,
            LightweightCache<string, int> spreadPathIndexByName,
            ValidationContext context)
        {
            var fragmentName = fragment.Name;
            visitedFrags[fragmentName] = true;

            var spreadNodes = context.GetFragmentSpreads(fragment.SelectionSet);
            if (spreadNodes.Count == 0)
            {
                return;
            }

            spreadPathIndexByName[fragmentName] = spreadPath.Count;

            foreach (var spreadNode in spreadNodes)
            {
                var spreadName = spreadNode.Name;
                var cycleIndex = spreadPathIndexByName[spreadName];

                if (cycleIndex == -1)
                {
                    spreadPath.Push(spreadNode);

                    if (!visitedFrags[spreadName])
                    {
                        var spreadFragment = context.GetFragment(spreadName);
                        if (spreadFragment != null)
                        {
                            detectCycleRecursive(
                                spreadFragment,
                                spreadPath,
                                visitedFrags,
                                spreadPathIndexByName,
                                context);
                        }
                    }

                    spreadPath.Pop();
                }
                else
                {
                    var cyclePath = spreadPath.Reverse().Skip(cycleIndex).ToArray();
                    var nodes = cyclePath.OfType<INode>().Concat(new[] { spreadNode }).ToArray();

                    context.ReportError(new NoFragmentCyclesError(context, spreadName, cyclePath.Select(x => x.Name).ToArray(), nodes));
                }
            }

            spreadPathIndexByName[fragmentName] = -1;
        }
    }
}
