using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Complexity
{
    /// <summary>
    /// The default complexity analyzer.
    /// </summary>
    public class ComplexityAnalyzer : IComplexityAnalyzer
    {
        private sealed class FragmentComplexity
        {
            public int Depth { get; set; }
            public double Complexity { get; set; }
        }

        private sealed class AnalysisContext
        {
            public ComplexityResult Result { get; } = new ComplexityResult();
            public int LoopCounter { get; set; }
            public int MaxRecursionCount { get; set; }
            public Dictionary<string, FragmentComplexity> FragmentMap { get; } = new Dictionary<string, FragmentComplexity>();

            public void AssertRecursion()
            {
                if (LoopCounter++ > MaxRecursionCount)
                {
                    throw new InvalidOperationException("Query is too complex to validate.");
                }
            }
        }

        /// <inheritdoc/>
        public void Validate(Document document, ComplexityConfiguration complexityParameters)
        {
            if (complexityParameters == null)
                return;
            var complexityResult = Analyze(document, complexityParameters.FieldImpact ?? 2.0f, complexityParameters.MaxRecursionCount);

            Analyzed(document, complexityParameters, complexityResult);

            if (complexityResult.Complexity > complexityParameters.MaxComplexity)
                throw new InvalidOperationException(
                    $"Query is too complex to execute. The field with the highest complexity is: {complexityResult.ComplexityMap.OrderByDescending(pair => pair.Value).First().Key}");

            if (complexityResult.TotalQueryDepth > complexityParameters.MaxDepth)
                throw new InvalidOperationException(
                    $"Query is too nested to execute. Depth is {complexityResult.TotalQueryDepth} levels, maximum allowed on this endpoint is {complexityParameters.MaxDepth}.");
        }

        /// <summary>
        /// Executes after the complexity analysis has completed, before comparing results to the complexity configuration parameters.
        /// This method is made to be able to access the calculated <see cref="ComplexityResult"/> and handle it, for example, for logging.
        /// </summary>
        protected virtual void Analyzed(Document document, ComplexityConfiguration complexityParameters, ComplexityResult complexityResult)
        {
#if DEBUG
            Debug.WriteLine($"Complexity: {complexityResult.Complexity}");
            Debug.WriteLine($"Sum(Query depth across all subqueries) : {complexityResult.TotalQueryDepth}");
            foreach (var node in complexityResult.ComplexityMap)
                Debug.WriteLine($"{node.Key} : {node.Value}");
#endif
        }

        /// <summary>
        /// Analyzes the complexity of a document.
        /// </summary>
        internal ComplexityResult Analyze(Document doc, double avgImpact, int maxRecursionCount)
        {
            if (avgImpact <= 1)
                throw new ArgumentOutOfRangeException(nameof(avgImpact));

            var context = new AnalysisContext { MaxRecursionCount = maxRecursionCount };

            foreach (var node in doc.Children.OfType<FragmentDefinition>())
            {
                var fragResult = new FragmentComplexity();
                FragmentIterator(context, node, fragResult, avgImpact, avgImpact, 1d);
                context.FragmentMap[node.Name] = fragResult;
            }

            TreeIterator(context, doc, avgImpact, avgImpact, 1d);

            return context.Result;
        }

        private void FragmentIterator(AnalysisContext context, INode node, FragmentComplexity qDepthComplexity, double avgImpact, double currentSubSelectionImpact, double currentEndNodeImpact)
        {
            context.AssertRecursion();

            if (node.Children != null &&
                node.Children.Any(
                    n => n is Field || (n is SelectionSet set && set.Children.Any()) || n is Operation))
            {
                if (node is Field)
                {
                    qDepthComplexity.Depth++;
                    var impactFromArgs = GetImpactFromArgs(node);
                    qDepthComplexity.Complexity += currentEndNodeImpact = impactFromArgs / avgImpact * currentSubSelectionImpact ?? currentSubSelectionImpact;
                    foreach (var nodeChild in node.Children.Where(n => n is SelectionSet))
                        FragmentIterator(context, nodeChild, qDepthComplexity, avgImpact, currentSubSelectionImpact * (impactFromArgs ?? avgImpact), currentEndNodeImpact);
                }
                else
                    foreach (var nodeChild in node.Children)
                        FragmentIterator(context, nodeChild, qDepthComplexity, avgImpact, currentSubSelectionImpact, currentEndNodeImpact);
            }
            else if (node is Field)
                qDepthComplexity.Complexity += currentEndNodeImpact;
        }

        private void TreeIterator(AnalysisContext context, INode node, double avgImpact, double currentSubSelectionImpact, double currentEndNodeImpact)
        {
            context.AssertRecursion();

            if (node is FragmentDefinition)
                return;

            if (node.Children != null &&
                node.Children.Any(n => n is Field || n is FragmentSpread || (n is SelectionSet set && set.Children.Any()) || n is Operation))
            {
                if (node is Field)
                {
                    context.Result.TotalQueryDepth++;
                    var impactFromArgs = GetImpactFromArgs(node);
                    RecordFieldComplexity(context, node, currentEndNodeImpact = impactFromArgs / avgImpact * currentSubSelectionImpact ?? currentSubSelectionImpact);
                    foreach (var nodeChild in node.Children.Where(n => n is SelectionSet))
                        TreeIterator(context, nodeChild, avgImpact, currentSubSelectionImpact * (impactFromArgs ?? avgImpact), currentEndNodeImpact);
                }
                else
                    foreach (var nodeChild in node.Children)
                        TreeIterator(context, nodeChild, avgImpact, currentSubSelectionImpact, currentEndNodeImpact);
            }
            else if (node is Field)
                RecordFieldComplexity(context, node, currentEndNodeImpact);
            else if (node is FragmentSpread spread)
            {
                var fragmentComplexity = context.FragmentMap[spread.Name];
                RecordFieldComplexity(context, spread, currentSubSelectionImpact / avgImpact * fragmentComplexity.Complexity);
                context.Result.TotalQueryDepth += fragmentComplexity.Depth;
            }
        }

        private static double? GetImpactFromArgs(INode node)
        {
            double? newImpact = null;
            if (!(node.Children.FirstOrDefault(n => n is Arguments) is Arguments args))
                return null;

            if (args.ValueFor("id") != null)
                newImpact = 1;
            else
            {
                if (args.ValueFor("first") is IntValue firstValue)
                    newImpact = firstValue.Value;
                else
                {
                    if (args.ValueFor("last") is IntValue lastValue)
                        newImpact = lastValue.Value;
                }
            }
            return newImpact;
        }

        private static void RecordFieldComplexity(AnalysisContext context, INode node, double impact)
        {
            context.Result.Complexity += impact;
            if (context.Result.ComplexityMap.ContainsKey(node))
                context.Result.ComplexityMap[node] += impact;
            else
                context.Result.ComplexityMap.Add(node, impact);
        }
    }
}
