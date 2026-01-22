using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies an output graph type mapping for the CLR class or property marked with this attribute,
/// and changes the base graph type for the generated graph type.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method)]
public sealed class OutputBaseTypeAttribute : GraphQLAttribute
{
    /// <inheritdoc cref="OutputBaseTypeAttribute"/>
    public OutputBaseTypeAttribute(Type graphType)
    {
        OutputBaseType = graphType;
    }

    /// <inheritdoc cref="OutputBaseTypeAttribute"/>
    public Type OutputBaseType
    {
        get;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!value.IsOutputType())
                throw new ArgumentException(nameof(OutputBaseType), $"'{value.GetFriendlyName()}' specified on '{GetType().GetFriendlyName()}' should be an output type");

            field = value;
        }
    } = null!;

    /// <inheritdoc/>
    public override void Modify(TypeInformation typeInformation)
    {
        if (!typeInformation.IsInputType)
        {
            typeInformation.GraphType = OutputBaseType;
        }
    }
}

/// <inheritdoc cref="OutputBaseTypeAttribute"/>
public class OutputBaseTypeAttribute<TGraphType> : GraphQLAttribute
    where TGraphType : IGraphType
{
    /// <inheritdoc/>
    public override void Modify(TypeInformation typeInformation)
    {
        if (!typeInformation.IsInputType)
        {
            typeInformation.GraphType = typeof(TGraphType);
        }
    }
}
