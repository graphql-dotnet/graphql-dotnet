using GraphQL.Reflection;

namespace GraphQL.Types;

/// <summary>
/// Implemented by a graph type that represents a C# union, whose values are unwrapped to the value held by
/// the active case before the object graph type for them is selected.
/// </summary>
internal interface IClrUnionGraphType
{
    /// <summary>
    /// Returns the value held by the active case of the specified union instance, or <see langword="null"/>
    /// when the union holds no value.
    /// </summary>
    object? GetCaseValue(object union);
}

/// <summary>
/// Allows you to automatically register a GraphQL union graph type for a C# <c>union</c> type, where each
/// case of the union becomes a possible type of the GraphQL union.
/// <br/><br/>
/// Since a GraphQL union may only contain object types, every case of the union must map to an object graph
/// type. A case that maps to a scalar, enumeration or list &#8212; for instance the <c>int</c> case of
/// <c>union IntOrString(int, string)</c> &#8212; has no representation in GraphQL and is reported when the
/// schema is initialized.
/// <br/><br/>
/// A field returning a union resolves to the value held by its active case, so the object graph type
/// selected during execution is the graph type of that value rather than of the union itself.
/// </summary>
public class AutoRegisteringUnionGraphType<[NotAGraphType] TSourceType> : UnionGraphType, IClrUnionGraphType
{
    private readonly ClrUnionInfo _unionInfo;

    /// <summary>
    /// Creates a GraphQL union graph type from <typeparamref name="TSourceType"/>.
    /// </summary>
    public AutoRegisteringUnionGraphType()
    {
        _unionInfo = ClrUnionInfo.Find(typeof(TSourceType))
            ?? throw new ArgumentOutOfRangeException(nameof(TSourceType), $"The type '{typeof(TSourceType).GetFriendlyName()}' is not a C# union type. Only a type declared with the 'union' keyword can be registered as an {nameof(AutoRegisteringUnionGraphType<TSourceType>)}.");

        Name = typeof(TSourceType).GraphQLName();
        // a union graph type is not a ComplexGraphType, which is where these are normally picked up
        Description ??= typeof(TSourceType).Description();
        DeprecationReason ??= typeof(TSourceType).ObsoleteMessage();
        ConfigureGraph();

        foreach (var caseType in _unionInfo.CaseTypes)
        {
            Type(typeof(GraphQLClrOutputTypeReference<>).MakeGenericType(caseType));
        }
    }

    object? IClrUnionGraphType.GetCaseValue(object union) => _unionInfo.GetValue(union);

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.ConfigureGraph"/>
    protected virtual void ConfigureGraph()
    {
        AutoRegisteringHelper.ApplyGraphQLAttributes<TSourceType>(this);
    }
}
