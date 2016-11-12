using System;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Complexity
{
    public class ComplexityResult
    {
        public Dictionary<INode, double> ComplexityMap { get; } = new Dictionary<INode, double>();
        public double Complexity { get; set; }
        public int TotalQueryDepth { get; set; }
    }
}