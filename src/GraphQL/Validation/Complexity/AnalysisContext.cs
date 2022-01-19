using System;
using System.Collections.Generic;
using System.Threading;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation.Complexity
{
    internal sealed class AnalysisContext : INodeVisitorContext
    {
        public double AvgImpact { get; set; }

        public double CurrentSubSelectionImpact { get; set; }

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

        public static double? GetImpactFromArgs(GraphQLField node)
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
