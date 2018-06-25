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

        private class ComplexityAnalysisContext
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
            
            var context = new ComplexityAnalysisContext();

            doc.Children.Where(node => node is FragmentDefinition).Apply(node =>
            {
                var fragResult = new FragmentComplexity();
                FragmentIterator(node, fragResult, avgImpact, avgImpact, 1d, context);
                var fragmentName = ((FragmentDefinition) node).Name;
                context.FragmentMap[fragmentName] = fragResult;
            });
            
            TreeIterator(doc, context, avgImpact, avgImpact, 1d);

            return context.Result;
        }

        private void FragmentIterator(INode node, FragmentComplexity qDepthComplexity, double avgImpact, double currentSubSelectionImpact, double currentEndNodeImpact, ComplexityAnalysisContext context)
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
                        FragmentIterator(nodeChild, qDepthComplexity, avgImpact, currentSubSelectionImpact * (impactFromArgs ?? avgImpact), currentEndNodeImpact, context);
                }
                else
                    foreach (var nodeChild in node.Children)
                        FragmentIterator(nodeChild, qDepthComplexity, avgImpact, currentSubSelectionImpact, currentEndNodeImpact, context);
            }
            else if (node is Field)
                qDepthComplexity.Complexity += currentEndNodeImpact;
        }

        private void TreeIterator(INode node, ComplexityAnalysisContext context, double avgImpact, double currentSubSelectionImpact, double currentEndNodeImpact)
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
                    RecordFieldComplexity(node, context, currentEndNodeImpact = impactFromArgs / avgImpact * currentSubSelectionImpact ?? currentSubSelectionImpact);
                    foreach (var nodeChild in node.Children.Where(n => n is SelectionSet))
                        TreeIterator(nodeChild, context, avgImpact, currentSubSelectionImpact * (impactFromArgs ?? avgImpact), currentEndNodeImpact);
                }
                else
                    foreach (var nodeChild in node.Children)
                        TreeIterator(nodeChild, context, avgImpact, currentSubSelectionImpact, currentEndNodeImpact);
            }
            else if (node is Field)
                RecordFieldComplexity(node, context, currentEndNodeImpact);
            else if (node is FragmentSpread)
            {
                var fragmentComplexity = context.FragmentMap[((FragmentSpread)node).Name];
                RecordFieldComplexity(node, context, currentSubSelectionImpact / avgImpact * fragmentComplexity.Complexity);
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

        private static void RecordFieldComplexity(INode node, ComplexityAnalysisContext context, double impact)
        {
            context.Result.Complexity += impact;
            if (context.Result.ComplexityMap.ContainsKey(node))
                context.Result.ComplexityMap[node] += impact;
            else
                context.Result.ComplexityMap.Add(node, impact);
        }
    }
}
