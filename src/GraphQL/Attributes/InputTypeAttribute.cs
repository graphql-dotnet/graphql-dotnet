using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies an input graph type mapping for the CLR class or property marked with this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class InputTypeAttribute : GraphQLAttribute
{
    /// <inheritdoc cref="InputTypeAttribute"/>
    public InputTypeAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type graphType)
    {
        InputType = graphType;
    }

    /// <inheritdoc cref="InputTypeAttribute"/>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type InputType
    {
        get;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!value.IsInputType())
                throw new ArgumentException(nameof(InputType), $"'{value}' should be an input type");

            field = value;
        }
    } = null!;

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
        {
            fieldType.Type = InputType;
        }
    }

    /// <inheritdoc/>
    public override void Modify(QueryArgument queryArgument)
    {
        queryArgument.Type = InputType;
    }
}

/// <inheritdoc cref="InputTypeAttribute"/>
public class InputTypeAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType> : InputTypeAttribute
    where TGraphType : IGraphType
{
    /// <inheritdoc cref="InputTypeAttribute"/>
    public InputTypeAttribute()
        : base(typeof(TGraphType))
    {
    }
}
