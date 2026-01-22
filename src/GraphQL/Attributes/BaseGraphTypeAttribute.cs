using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies a graph type mapping for the CLR class or property marked with this attribute,
/// and changes the base graph type for the generated graph type. This attribute works for both
/// input and output types.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Method)]
public sealed class BaseGraphTypeAttribute : GraphQLAttribute
{
    /// <inheritdoc cref="BaseGraphTypeAttribute"/>
    public BaseGraphTypeAttribute(Type graphType)
    {
        BaseGraphType = graphType;
    }

    /// <inheritdoc cref="BaseGraphTypeAttribute"/>
    public Type BaseGraphType
    {
        get;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!value.IsGraphType())
                throw new ArgumentException(nameof(BaseGraphType), $"'{value.GetFriendlyName()}' specified on '{GetType().GetFriendlyName()}' should be a graph type");

            field = value;
        }
    } = null!;

    /// <inheritdoc/>
    public override void Modify(TypeInformation typeInformation)
    {
        typeInformation.GraphType = BaseGraphType;
    }
}

/// <inheritdoc cref="BaseGraphTypeAttribute"/>
public class BaseGraphTypeAttribute<TGraphType> : GraphQLAttribute
    where TGraphType : IGraphType
{
    /// <inheritdoc/>
    public override void Modify(TypeInformation typeInformation)
    {
        typeInformation.GraphType = typeof(TGraphType);
    }
}
