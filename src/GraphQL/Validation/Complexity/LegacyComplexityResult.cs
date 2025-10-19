using GraphQLParser.AST;

namespace GraphQL.Validation.Complexity;

/// <summary>
/// Contains the result of a complexity analysis.
/// </summary>
[Obsolete("Please use the new complexity analyzer. This class will be removed in v9.")]
public class LegacyComplexityResult
{
    /// <summary>
    /// Returns a dictionary of nodes and their complexity factors.
    /// </summary>
    public Dictionary<ASTNode, double> ComplexityMap { get; } = [];

    /// <summary>
    /// Returns the total calculated document complexity factor over all executed nodes.
    /// </summary>
    public double Complexity { get; set; }

    /// <summary>
    /// Returns the total query depth.
    /// </summary>
    public int TotalQueryDepth { get; set; }
}
