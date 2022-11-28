using GraphQLParser.AST;

namespace GraphQL.Validation.Complexity
{
    /// <summary>
    /// Contains the result of a complexity analysis.
    /// </summary>
    public class ComplexityResult
    {
        /// <summary>
        /// Returns a dictionary of nodes and their complexity factors.
        /// </summary>
        public Dictionary<ASTNode, double> ComplexityMap { get; } = new Dictionary<ASTNode, double>();

        /// <summary>
        /// Returns the total calculated document complexity factor over all executed nodes.
        /// </summary>
        public double Complexity { get; set; }

        /// <summary>
        /// Returns the total query depth.
        /// </summary>
        public int TotalQueryDepth { get; set; }
    }
}
