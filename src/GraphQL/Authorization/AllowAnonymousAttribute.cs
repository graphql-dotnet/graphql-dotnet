using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL;

/// <summary>
/// Attribute to typically indicate that anonymous access should be allowed to a field of a graph type
/// requiring authorization, providing that no other fields were selected.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public class AllowAnonymousAttribute : GraphQLAttribute
{
    /// <inheritdoc />
    public override void Modify(FieldConfig field)
    {
        field.AllowAnonymous();
    }

    /// <inheritdoc />
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        fieldType.AllowAnonymous();
    }
}
