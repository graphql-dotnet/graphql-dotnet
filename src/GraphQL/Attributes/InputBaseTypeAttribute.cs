using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies an input graph type mapping for the CLR class or property marked with this attribute,
/// and changes the base graph type for the generated graph type.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class InputBaseTypeAttribute : GraphQLAttribute
{
    /// <inheritdoc cref="InputBaseTypeAttribute"/>
    public InputBaseTypeAttribute(Type graphType)
    {
        InputBaseType = graphType;
    }

    /// <inheritdoc cref="InputBaseTypeAttribute"/>
    public Type InputBaseType
    {
        get;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!value.IsInputType())
                throw new ArgumentException(nameof(InputBaseType), $"'{value.GetFriendlyName()}' specified on '{GetType().GetFriendlyName()}' should be an input type");

            if (typeof(ListGraphType).IsAssignableFrom(value) || typeof(NonNullGraphType).IsAssignableFrom(value))
                throw new ArgumentException(nameof(InputBaseType), $"'{value.GetFriendlyName()}' specified on '{GetType().GetFriendlyName()}' should not be a wrapper type such as ListGraphType or NonNullGraphType");

            field = value;
        }
    } = null!;

    /// <inheritdoc/>
    public override void Modify(TypeInformation typeInformation)
    {
        if (typeInformation.IsInputType)
        {
            typeInformation.GraphType = InputBaseType;
        }
    }
}

/// <inheritdoc cref="InputBaseTypeAttribute"/>
public class InputBaseTypeAttribute<TGraphType> : InputBaseTypeAttribute
    where TGraphType : IGraphType
{
    /// <inheritdoc cref="InputBaseTypeAttribute{TGraphType}"/>
    public InputBaseTypeAttribute()
        : base(typeof(TGraphType))
    {
    }
}
