using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation;

/// <summary>
/// Represents a variable reference node and the graph type it is referenced to be used for.
/// </summary>
public class VariableUsage
{
    /// <summary>
    /// Returns a variable reference node.
    /// </summary>
    public GraphQLVariable Node { get; }

    /// <summary>
    /// Returns a graph type.
    /// </summary>
    public IGraphType Type { get; }

    /// <summary>
    /// Indicates if the variable usage has a default field value.
    /// </summary>
    public bool HasDefault { get; }

    /// <summary>
    /// Indicates if the variable usage is a non-null field.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Initializes a new instance with the specified parameters.
    /// </summary>
    /// <param name="node">A variable reference node.</param>
    /// <param name="type">A graph type.</param>
    /// <param name="hasDefault">Indicates if the field has a default value.</param>
    /// <param name="isRequired">Indicates if the field is a non-null field.</param>
    public VariableUsage(GraphQLVariable node, IGraphType type, bool hasDefault, bool isRequired)
    {
        Node = node;
        Type = type;
        HasDefault = hasDefault;
        IsRequired = isRequired;
    }
}
