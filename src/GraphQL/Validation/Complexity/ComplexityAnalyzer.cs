using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public class ComplexityAnalyzer : IComplexityAnalyzer
    {
        public class ComplexityResult
        {
            public Dictionary<INode, double> ComplexityMap { get; } = new Dictionary<INode, double>();
            public double Complexity { get; set; }
            public int TotalQueryDepth { get; set; }
        }

        private ComplexityResult _result = new ComplexityResult();
        private readonly double _avgImpact;
        private readonly int _maxRecursionCount;
        private int _loopCounter;

        /// <summary>
        /// Creates a new instance of ComplexityAnalyzer
        /// </summary>
        /// <param name="avgImpact">
        /// Average number of entries per GraphType.
        /// If a hard limit is imposed by your application data access layer on number of entities returned then use that number.
        /// </param>
        /// <param name="maxRecursionCount">
        /// Max. number of times to traverse tree nodes. GraphiQL queries take ~95 iterations, adjust as needed.
        /// </param>
        public ComplexityAnalyzer(double avgImpact = 2.0d, int maxRecursionCount = 100)
        {
            if (avgImpact <= 1) throw new ArgumentOutOfRangeException(nameof(avgImpact));

            _avgImpact = avgImpact;
            _maxRecursionCount = maxRecursionCount;
        }


        /// <summary>
        /// Analyzes the complexity of a document.
        /// </summary>  
        public ComplexityResult Analyze(Document doc)
        {
            TreeIterator(doc, _avgImpact);

            // Cleanup in case Analyze is called again
            _loopCounter = 0;
            var retVal = _result;
            _result = new ComplexityResult();

            return retVal;
        }

        private void TreeIterator(INode node, double impact)
        {
            if (_loopCounter++ > _maxRecursionCount)
                throw new InvalidOperationException("Query is too complex to validate.");

            if (node.Children != null && node.Children.Any(n => n is Field || (n is SelectionSet && ((SelectionSet)n).Children.Any()) || n is Operation))
            {
                if (node is Field)
                {
                    _result.TotalQueryDepth++;
                    RecordFieldComplexity(node, impact);
                    foreach (var nodeChild in node.Children.Where(n => n is SelectionSet))
                        TreeIterator(nodeChild, _avgImpact * impact);
                }
                else
                    foreach (var nodeChild in node.Children)
                        TreeIterator(nodeChild, impact);
            }
            else if (node is Field)
                RecordFieldComplexity(node, impact);
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