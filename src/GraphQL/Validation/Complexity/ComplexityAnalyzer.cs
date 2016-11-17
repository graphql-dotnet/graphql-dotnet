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

        private Dictionary<string, FragmentComplexity> _fragmentMap { get; } = new Dictionary<string, FragmentComplexity>();
        private ComplexityResult _result = new ComplexityResult();
        private readonly int _maxRecursionCount;
        private int _loopCounter;

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

            doc.Children.Where(node => node is FragmentDefinition).Apply(node =>
            {
                var fragResult = new FragmentComplexity();
                FragmentIterator(node, fragResult, avgImpact, avgImpact, 1d);
                _fragmentMap[(node as FragmentDefinition)?.Name] = fragResult;
            });

            TreeIterator(doc, _result, avgImpact, avgImpact, 1d);

            // Cleanup in case Analyze is called again
            _loopCounter = 0;
            var retVal = _result;
            _result = new ComplexityResult();

            return retVal;
        }

        private void FragmentIterator(INode node, FragmentComplexity qDepthComplexity, double avgImpact, double currentSubSelectionImpact, double currentEndNodeImpact)
        {
            if (_loopCounter++ > _maxRecursionCount)
                throw new InvalidOperationException("Query is too complex to validate.");

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
                        FragmentIterator(nodeChild, qDepthComplexity, avgImpact, currentSubSelectionImpact * (impactFromArgs ?? avgImpact), currentEndNodeImpact);
                }
                else
                    foreach (var nodeChild in node.Children)
                        FragmentIterator(nodeChild, qDepthComplexity, avgImpact, currentSubSelectionImpact, currentEndNodeImpact);
            }
            else if (node is Field)
                qDepthComplexity.Complexity += currentEndNodeImpact;
        }

        private void TreeIterator(INode node, ComplexityResult result, double avgImpact, double currentSubSelectionImpact, double currentEndNodeImpact)
        {
            if (_loopCounter++ > _maxRecursionCount)
                throw new InvalidOperationException("Query is too complex to validate.");
            if (node is FragmentDefinition) return;

            if (node.Children != null &&
                node.Children.Any(n => n is Field || n is FragmentSpread || (n is SelectionSet && ((SelectionSet)n).Children.Any()) || n is Operation))
            {
                if (node is Field)
                {
                    result.TotalQueryDepth++;
                    var impactFromArgs = GetImpactFromArgs(node);
                    RecordFieldComplexity(node, result, currentEndNodeImpact = impactFromArgs / avgImpact * currentSubSelectionImpact ?? currentSubSelectionImpact);
                    foreach (var nodeChild in node.Children.Where(n => n is SelectionSet))
                        TreeIterator(nodeChild, result, avgImpact, currentSubSelectionImpact * (impactFromArgs ?? avgImpact), currentEndNodeImpact);
                }
                else
                    foreach (var nodeChild in node.Children)
                        TreeIterator(nodeChild, result, avgImpact, currentSubSelectionImpact, currentEndNodeImpact);
            }
            else if (node is Field)
                RecordFieldComplexity(node, result, currentEndNodeImpact);
            else if (node is FragmentSpread)
            {
                var fragmentComplexity = _fragmentMap[((FragmentSpread)node).Name];
                RecordFieldComplexity(node, result, currentSubSelectionImpact / avgImpact * fragmentComplexity.Complexity);
                result.TotalQueryDepth += fragmentComplexity.Depth;
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

        private static void RecordFieldComplexity(INode node, ComplexityResult result, double impact)
        {
            result.Complexity += impact;
            if (result.ComplexityMap.ContainsKey(node))
                result.ComplexityMap[node] += impact;
            else
                result.ComplexityMap.Add(node, impact);
        }
    }
}