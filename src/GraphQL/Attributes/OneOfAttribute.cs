using GraphQL.Conversion;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies a GraphQL type name for a CLR class when used as an input type.
/// Note that the specified name will be translated by the schema's <see cref="INameConverter"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class OneOfAttribute : GraphQLAttribute
{
    /// <inheritdoc/>
    public override void Modify(IGraphType graphType)
    {
        if (graphType is IInputObjectGraphType inputType)
        {
            inputType.IsOneOf = true;
        }
    }
}
