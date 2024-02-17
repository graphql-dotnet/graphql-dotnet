using GraphQL.Types;

namespace GraphQL.Utilities.Federation;

/// <summary>
/// Represents a type unknown within this portion of the federated schema.
/// </summary>
public class AnyScalarGraphType : ComplexScalarGraphType
{
    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    public AnyScalarGraphType()
    {
        Name = "_Any";
    }
}
