using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public class ComplexityAnalyzer
    {
        public class ComplexityResult
        {
            public Dictionary<INode, float> ComplexityMap { get; set; } = new Dictionary<INode, float>();
            public int Complexity { get; set; }
            public int TotalQueryDepth { get; set; }
        }

        private ComplexityResult _result = new ComplexityResult();
        private readonly float _avgImpact;
        private readonly int _maxRecursionCount;
        private int _loopCounter;

        public ComplexityAnalyzer(float avgImpact = 1.5f, int maxRecursionCount = 100)
        {
            if (avgImpact <= 1) throw new ArgumentOutOfRangeException(nameof(avgImpact));

            _avgImpact = avgImpact;
            _maxRecursionCount = maxRecursionCount;
        }
        public ComplexityResult Analyze(Document doc)
        {
            TreeIterator(doc, _avgImpact);
            _loopCounter = 0;
            var retVal = _result;
            _result = new ComplexityResult();
            return retVal;
        }

        private void TreeIterator(INode node, float impact)
        {
            if (_loopCounter++ > _maxRecursionCount)
                throw new InvalidOperationException("Query is too complex to validate.");

            // if (impact <= 1) impact = _avgImpact;

            if (node.Children != null && node.Children.Any(n => n is Field || (n is SelectionSet && ((SelectionSet)n).Children.Any()) || n is Operation))
            {
                var nextimpact = _avgImpact * impact;
                if (node is Field)
                {
                    _result.TotalQueryDepth++;
                    foreach (var nodeChild in node.Children.Where(n => n is SelectionSet))
                        TreeIterator(nodeChild, nextimpact);
                }
                else foreach (var nodeChild in node.Children)
                        TreeIterator(nodeChild, nextimpact);
            }
            else if (node is Field)
                if (_result.ComplexityMap.ContainsKey(node))
                    _result.ComplexityMap[node] += impact;
                else
                    _result.ComplexityMap.Add(node, impact);
        }
    }
}