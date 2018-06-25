using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Complexity
{
    public class ComplexityAnalyzer : IComplexityAnalyzer
    {
        private class FragmentComplexity
        {
            public int Depth { get; set; }
            public double Complexity { get; set; }
        }

        private class AnalysisContext
        {
            public ComplexityResult Result { get; } = new ComplexityResult();
            public int LoopCounter { get; set; }
            public Dictionary<string, FragmentComplexity> FragmentMap { get; } = new Dictionary<string, FragmentComplexity>();
        }
        
        private readonly int _maxRecursionCount;
        
        /// <summary>
        /// Creates a new instance of ComplexityAnalyzer
        /// </summary>
        /// <param name="maxRecursionCount">
        /// Max. number of times to traverse tree nodes. GraphiQL queries take ~95 iterations, adjust as needed.
        /// </param>
        public ComplexityAnalyzer(int maxRecursionCount = 250)
        {
            _maxRecursionCount = maxRecursionCount;
        }

        public void Validate(Document document, ComplexityConfiguration complexityParameters)
        {
            if (complexityParameters == null) return;
            var complexityResult = Analyze(document, complexityParameters.FieldImpact ?? 2.0f);
#if DEBUG
            Debug.WriteLine($"Complexity: {complexityResult.Complexity}");
            Debug.WriteLine($"Sum(Query depth across all subqueries) : {complexityResult.TotalQueryDepth}");
            foreach (var node in complexityResult.ComplexityMap) Debug.WriteLine($"{node.Key} : {node.Value}");
#endif
            if (complexityParameters.MaxComplexity.HasValue &&
                complexityResult.Complexity > complexityParameters.MaxComplexity.Value)
                throw new InvalidOperationException(
                    $"Query is too complex to execute. The field with the highest complexity is: {complexityResult.ComplexityMap.OrderByDescending(pair => pair.Value).First().Key}");

            if (complexityParameters.MaxDepth.HasValue &&
                complexityResult.TotalQueryDepth > complexityParameters.MaxDepth)
                throw new InvalidOperationException(
                    $"Query is too nested to execute. Depth is {complexityResult.TotalQueryDepth} levels, maximum allowed on this endpoint is {complexityParameters.MaxDepth}.");
        }

        /// <summary>
        /// Analyzes the complexity of a document.
        /// </summary>  
        internal ComplexityResult Analyze(Document doc, double avgImpact = 2.0d)
        {
            if (avgImpact <= 1) throw new ArgumentOutOfRangeException(nameof(avgImpact));
            
            var context = new AnalysisContext();

            doc.Children.OfType<FragmentDefinition>().Apply(node =>
            {
                var fragResult = new FragmentComplexity();
                FragmentIterator(context, node, fragResult, avgImpact, avgImpact, 1d);                
                context.FragmentMap[node.Name] = fragResult;
            });
            
            TreeIterator(context, doc, avgImpact, avgImpact, 1d);

            return context.Result;
        }

        private void FragmentIterator(AnalysisContext context, INode node, FragmentComplexity qDepthComplexity, double avgImpact, double currentSubSelectionImpact, double currentEndNodeImpact)
        {
            if (context.LoopCounter++ > _maxRecursionCount)
            {
                throw new InvalidOperationException("Query is too complex to validate.");
            }

            if (node.Children != null &&
                node.Children.Any(
                    n => n is Field || (n is SelectionSet && ((SelectionSet)n).Children.Any()) || n is Operation))
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
            if (context.LoopCounter++ > _maxRecursionCount)
            {
                throw new InvalidOperationException("Query is too complex to validate.");
            }

            if (node is FragmentDefinition) return;

            if (node.Children != null &&
                node.Children.Any(n => n is Field || n is FragmentSpread || (n is SelectionSet && ((SelectionSet)n).Children.Any()) || n is Operation))
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
            else if (node is FragmentSpread)
            {
                var fragmentComplexity = context.FragmentMap[((FragmentSpread)node).Name];
                RecordFieldComplexity(context, node, currentSubSelectionImpact / avgImpact * fragmentComplexity.Complexity);
                context.Result.TotalQueryDepth += fragmentComplexity.Depth;
            }
        }

        private static double? GetImpactFromArgs(INode node)
        {
            double? newImpact = null;
            var args = node.Children.First(n => n is Arguments) as Arguments;
            if (args == null) return null;

            if (args.ValueFor("id") != null) newImpact = 1;
            else
            {
                var firstValue = args.ValueFor("first") as IntValue;
                if (firstValue != null) newImpact = firstValue.Value;
                else
                {
                    var lastValue = args.ValueFor("last") as IntValue;
                    if (lastValue != null) newImpact = lastValue.Value;
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
