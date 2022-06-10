using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation.Complexity
{
    internal sealed class AnalysisContext : IASTVisitorContext
    {
        public double AvgImpact { get; set; }

        public double? CurrentSubSelectionImpact { get; set; }

        public double CurrentEndNodeImpact { get; set; }

        public FragmentComplexity CurrentFragmentComplexity { get; set; } = null!;

        public ComplexityResult Result { get; } = new ComplexityResult();

        public int LoopCounter { get; set; }

        public int MaxRecursionCount { get; set; }

        public bool FragmentMapAlreadyBuilt { get; set; }

        public Dictionary<string, FragmentComplexity> FragmentMap { get; } = new Dictionary<string, FragmentComplexity>();

        public CancellationToken CancellationToken => default;

        public void AssertRecursion()
        {
            if (LoopCounter++ > MaxRecursionCount)
            {
                throw new InvalidOperationException("Query is too complex to validate.");
            }
        }

        public static double? GetImpactFromArgs(GraphQLField node) //TODO: variables support
        {
            double? newImpact = null;

            if (node.Arguments != null)
            {
                if (node.Arguments.ValueFor("id") != null)
                {
                    newImpact = 1;
                }
                else
                {
                    if (node.Arguments.ValueFor("first") is GraphQLIntValue firstValue)
                    {
                        newImpact = Int.Parse(firstValue.Value);
                    }
                    else
                    {
                        if (node.Arguments.ValueFor("last") is GraphQLIntValue lastValue)
                            newImpact = Int.Parse(lastValue.Value);
                    }
                }
            }

            return newImpact;
        }

        /// <summary>
        /// Takes into account the complexity of the specified node.
        /// <br/>
        /// Available nodes:
        /// <list type="number">
        /// <item><see cref="GraphQLField"/></item>
        /// <item><see cref="GraphQLFragmentSpread"/></item>
        /// </list>
        /// </summary>
        /// <param name="node">The node for which the complexity is added.</param>
        /// <param name="impact">Added complexity.</param>
        public void RecordFieldComplexity(ASTNode node, double impact)
        {
            Result.Complexity += impact;

            if (Result.ComplexityMap.ContainsKey(node))
                Result.ComplexityMap[node] += impact;
            else
                Result.ComplexityMap.Add(node, impact);
        }
    }
}
