using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Marks a class as a OneOf Input Object.
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
