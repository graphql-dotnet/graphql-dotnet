using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies an output graph type mapping for the CLR class or property marked with this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public class OutputTypeAttribute : GraphQLAttribute
{
    /// <inheritdoc cref="OutputTypeAttribute"/>
    public OutputTypeAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type graphType)
    {
        OutputType = graphType;
    }

    /// <inheritdoc cref="OutputTypeAttribute"/>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type OutputType
    {
        get;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!value.IsOutputType())
                throw new ArgumentException(nameof(OutputType), $"'{value}' should be an output type");

            field = value;
        }
    } = null!;

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (!isInputType)
        {
            fieldType.Type = OutputType;
        }
    }
}

/// <inheritdoc cref="OutputTypeAttribute"/>
public class OutputTypeAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType> : OutputTypeAttribute
    where TGraphType : IGraphType
{
    /// <inheritdoc cref="OutputTypeAttribute"/>
    public OutputTypeAttribute()
        : base(typeof(TGraphType))
    {
    }
}
