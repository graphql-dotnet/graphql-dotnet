using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public class ComplexityReducer
    {
        public Dictionary<INode, int> ComplexityMap
        {
            get
            {
                if (!_initialized) throw new InvalidOperationException("Calculate() should be called before accessing this member.");
                return _complexityMap;
            }
        }

        public int Complexity
        {
            get
            {
                if (!_initialized) throw new InvalidOperationException("Calculate() should be called before accessing this member.");
                return _complexity;
            }
        }

        public int QueryDepth
        {
            get
            {
                if (!_initialized) throw new InvalidOperationException("Calculate() should be called before accessing this member.");
                return _queryDepth;
            }
        }

        private readonly Dictionary<INode, int> _complexityMap = new Dictionary<INode, int>();
        private bool _initialized = false;
        private int _queryDepth;
        private int _complexity;

        public void Calculate(INode node, int impact = 1)
        {
            if (node.Children != null && node.Children.Any(n => n is Field || (n is SelectionSet && ((SelectionSet)n).Children.Any()) || n is Operation))
            {
                if (node is Field)
                {
                    _queryDepth++;
                    foreach (var nodeChild in node.Children.Where(n => n is SelectionSet))
                        Calculate(nodeChild, impact * _queryDepth);
                }
                else foreach (var nodeChild in node.Children)
                        Calculate(nodeChild, impact * _queryDepth);
            }
            else if (node is Field)
            {
                if (_complexityMap.ContainsKey(node))
                    _complexityMap[node] += impact;
                else
                    _complexityMap.Add(node, impact);
            }

            _initialized = true;
        }
    }
}