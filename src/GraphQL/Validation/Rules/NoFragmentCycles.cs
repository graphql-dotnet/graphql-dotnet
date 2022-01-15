using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public static readonly NoFragmentCycles Instance = new NoFragmentCycles();

        /// <inheritdoc/>
        /// <exception cref="NoFragmentCyclesError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new ValueTask<INodeVisitor?>(context.Document.Definitions.OfType<GraphQLFragmentDefinition>().Count() > 0 ? _nodeVisitor : null); //TODO:LINQ

        private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLFragmentDefinition>((node, context) =>
        {
            var visitedFrags = context.TypeInfo.NoFragmentCycles_VisitedFrags ??= new HashSet<ROM>();
            var spreadPath = context.TypeInfo.NoFragmentCycles_SpreadPath ??= new Stack<GraphQLFragmentSpread>();
            var spreadPathIndexByName = context.TypeInfo.NoFragmentCycles_SpreadPathIndexByName ??= new Dictionary<ROM, int>();
            if (!visitedFrags.Contains(node.Name))
            {
                detectCycleRecursive(node, spreadPath, visitedFrags, spreadPathIndexByName, context);
            }
        });

        private static void detectCycleRecursive(
            GraphQLFragmentDefinition fragment,
            Stack<GraphQLFragmentSpread> spreadPath,
            HashSet<ROM> visitedFrags,
            Dictionary<ROM, int> spreadPathIndexByName,
            ValidationContext context)
        {
            var fragmentName = fragment.Name;
            visitedFrags.Add(fragmentName);

            var spreadNodes = context.GetFragmentSpreads(fragment.SelectionSet);
            if (spreadNodes.Count == 0)
            {
                return;
            }

            spreadPathIndexByName[fragmentName] = spreadPath.Count;

            foreach (var spreadNode in spreadNodes)
            {
                var spreadName = spreadNode.Name;
                if (!spreadPathIndexByName.TryGetValue(spreadName, out var cycleIndex))
                {
                    spreadPath.Push(spreadNode);

                    if (!visitedFrags.Contains(spreadName))
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
                    var nodes = cyclePath.OfType<ASTNode>().Concat(new[] { spreadNode }).ToArray();

                    context.ReportError(new NoFragmentCyclesError(context, spreadName, cyclePath.Select(x => x.Name.StringValue).ToArray(), nodes)); //TODO:!!!alloc
                }
            }

            spreadPathIndexByName.Remove(fragmentName);
        }
    }
}
