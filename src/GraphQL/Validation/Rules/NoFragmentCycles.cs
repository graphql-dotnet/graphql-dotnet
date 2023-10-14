using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No fragment cycles:
    ///
    /// A GraphQL document is only valid if it does not contain fragment cycles.
    /// </summary>
    public class NoFragmentCycles : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly NoFragmentCycles Instance = new();

        /// <inheritdoc/>
        /// <exception cref="NoFragmentCyclesError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(context.Document.FragmentsCount() > 0 ? _nodeVisitor : null);

        private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLFragmentDefinition>((node, context) =>
        {
            var visitedFrags = context.TypeInfo.NoFragmentCycles_VisitedFrags ??= new();
            var spreadPath = context.TypeInfo.NoFragmentCycles_SpreadPath ??= new();
            var spreadPathIndexByName = context.TypeInfo.NoFragmentCycles_SpreadPathIndexByName ??= new();
            if (!visitedFrags.Contains(node.FragmentName.Name))
            {
                DetectCycleRecursive(node, spreadPath, visitedFrags, spreadPathIndexByName, context);
            }
        });

        private static void DetectCycleRecursive(
            GraphQLFragmentDefinition fragment,
            Stack<GraphQLFragmentSpread> spreadPath,
            HashSet<ROM> visitedFrags,
            Dictionary<ROM, int> spreadPathIndexByName,
            ValidationContext context)
        {
            var fragmentName = fragment.FragmentName.Name;
            visitedFrags.Add(fragmentName);

            var spreadNodes = context.GetFragmentSpreads(fragment.SelectionSet);
            if (spreadNodes.Count == 0)
            {
                return;
            }

            spreadPathIndexByName[fragmentName] = spreadPath.Count;

            foreach (var spreadNode in spreadNodes)
            {
                var spreadName = spreadNode.FragmentName.Name;
                if (!spreadPathIndexByName.TryGetValue(spreadName, out var cycleIndex))
                {
                    spreadPath.Push(spreadNode);

                    if (!visitedFrags.Contains(spreadName))
                    {
                        var spreadFragment = context.Document.FindFragmentDefinition(spreadName);
                        if (spreadFragment != null)
                        {
                            DetectCycleRecursive(
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
                    var nodes = cyclePath.OfType<ASTNode>().Concat(new[] { spreadNode }).ToArray();

                    context.ReportError(new NoFragmentCyclesError(context, spreadName.StringValue, cyclePath.Select(x => x.FragmentName.Name.StringValue).ToArray(), nodes)); //ISSUE:allocation
                }
            }

            spreadPathIndexByName.Remove(fragmentName);
        }
    }
}
