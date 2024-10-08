using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation.Complexity;

[Obsolete("Please use the new complexity analyzer. This class will be removed in v9.")]
internal sealed class LegacyAnalysisContext : IASTVisitorContext
{
    public double AvgImpact { get; set; }

    public double? CurrentSubSelectionImpact { get; set; }

    public double CurrentEndNodeImpact { get; set; }

    public LegacyFragmentComplexity CurrentFragmentComplexity { get; set; } = null!;

    public LegacyComplexityResult Result { get; } = new LegacyComplexityResult();

    public int LoopCounter { get; set; }

    public int MaxRecursionCount { get; set; }

    public bool FragmentMapAlreadyBuilt { get; set; }

    public Dictionary<string, LegacyFragmentComplexity> FragmentMap { get; } = new Dictionary<string, LegacyFragmentComplexity>();

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
