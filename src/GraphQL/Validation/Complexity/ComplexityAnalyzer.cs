using System;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Complexity
{
    public class ComplexityAnalyzer : IComplexityAnalyzer
    {
        private ComplexityResult _result = new ComplexityResult();
        private readonly int _maxRecursionCount;
        private int _loopCounter;

        /// <summary>
        /// Creates a new instance of ComplexityAnalyzer
        /// </summary>
        /// <param name="maxRecursionCount">
        /// Max. number of times to traverse tree nodes. GraphiQL queries take ~95 iterations, adjust as needed.
        /// </param>
        /// 
        public ComplexityAnalyzer(int maxRecursionCount = 100)
        {
            _maxRecursionCount = maxRecursionCount;
        }


        /// <summary>
        /// Analyzes the complexity of a document.
        /// </summary>  
        public ComplexityResult Analyze(Document doc, double avgImpact = 2.0d)
        {
            if (avgImpact <= 1) throw new ArgumentOutOfRangeException(nameof(avgImpact));

            TreeIterator(doc, avgImpact, avgImpact);

            // Cleanup in case Analyze is called again
            _loopCounter = 0;
            var retVal = _result;
            _result = new ComplexityResult();

            return retVal;
        }

        private void TreeIterator(INode node, double avgImpact, double currentImpact)
        {
            if (_loopCounter++ > _maxRecursionCount)
                throw new InvalidOperationException("Query is too complex to validate.");

            if (node.Children != null && node.Children.Any(n => n is Field || (n is SelectionSet && ((SelectionSet)n).Children.Any()) || n is Operation))
            {
                if (node is Field)
                {
                    _result.TotalQueryDepth++;
                    RecordFieldComplexity(node, currentImpact);
                    foreach (var nodeChild in node.Children.Where(n => n is SelectionSet))
                        TreeIterator(nodeChild, avgImpact, currentImpact * avgImpact);
                }
                else
                    foreach (var nodeChild in node.Children)
                        TreeIterator(nodeChild, avgImpact, currentImpact);
            }
            else if (node is Field)
                RecordFieldComplexity(node, currentImpact);
        }

        private void RecordFieldComplexity(INode node, double impact)
        {
            _result.Complexity += impact;
            if (_result.ComplexityMap.ContainsKey(node))
                _result.ComplexityMap[node] += impact;
            else
                _result.ComplexityMap.Add(node, impact);
        }
    }
}