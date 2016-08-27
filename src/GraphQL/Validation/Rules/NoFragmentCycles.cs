using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No fragment cycles
    /// </summary>
    public class NoFragmentCycles : IValidationRule
    {
        public string CycleErrorMessage(string fragName, string[] spreadNames)
        {
            var via = spreadNames.Any() ? " via " + string.Join(", ", spreadNames) : "";
            return $"Cannot spread fragment \"{fragName}\" within itself{via}.";
        }

        public INodeVisitor Validate(ValidationContext context)
        {
            // Tracks already visited fragments to maintain O(N) and to ensure that cycles
            // are not redundantly reported.
            var visitedFrags = new LightweightCache<string, bool>(key => false);

            // Array of AST nodes used to produce meaningful errors
            var spreadPath = new Stack<FragmentSpread>();

            // Position in the spread path
            var spreadPathIndexByName = new LightweightCache<string, int>(key => -1);

            return new EnterLeaveListener(_ =>
            {
                _.Match<FragmentDefinition>(node =>
                {
                    if (!visitedFrags[node.Name])
                    {
                        detectCycleRecursive(node, spreadPath, visitedFrags, spreadPathIndexByName, context);
                    }
                });
            });
        }

        private void detectCycleRecursive(
            FragmentDefinition fragment,
            Stack<FragmentSpread> spreadPath,
            LightweightCache<string, bool> visitedFrags,
            LightweightCache<string, int> spreadPathIndexByName,
            ValidationContext context)
        {
            var fragmentName = fragment.Name;
            visitedFrags[fragmentName] = true;

            var spreadNodes = context.GetFragmentSpreads(fragment.SelectionSet).ToArray();
            if (!spreadNodes.Any())
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
                    var nodes = cyclePath.OfType<INode>().Concat(new[] {spreadNode}).ToArray();

                    context.ReportError(new ValidationError(
                        context.OriginalQuery,
                        "5.4",
                        CycleErrorMessage(spreadName, cyclePath.Select(x => x.Name).ToArray()),
                        nodes));
                }
            }

            spreadPathIndexByName[fragmentName] = -1;
        }
    }
}
