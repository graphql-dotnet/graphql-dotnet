namespace GraphQL.Types;

/// <summary>
/// Maps unmapped complex types to <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
/// and <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/>.
/// </summary>
public class AutoRegisteringGraphTypeMappingProvider : IGraphTypeMappingProvider
{
    private readonly bool _mapInputTypes;
    private readonly bool _mapOutputTypes;

    /// <summary>
    /// Creates an instance that maps both input and output types.
    /// </summary>
    public AutoRegisteringGraphTypeMappingProvider()
        : this(true, true)
    {
    }

    /// <summary>
    /// Creates an instance that maps input and/or output types, as specified.
    /// </summary>
    public AutoRegisteringGraphTypeMappingProvider(bool mapInputTypes, bool mapOutputTypes)
    {
        _mapInputTypes = mapInputTypes;
        _mapOutputTypes = mapOutputTypes;
    }

    /// <inheritdoc/>
    public Type? GetGraphTypeFromClrType(Type clrType, bool isInputType, Type? preferredType)
    {
        if (preferredType != null)
            return preferredType;

        if (isInputType && !_mapInputTypes ||
            !isInputType && !_mapOutputTypes ||
            clrType.IsEnum ||
            SchemaTypes.BuiltInScalarMappings.ContainsKey(clrType))
            return null;

        return (isInputType ? typeof(AutoRegisteringInputObjectGraphType<>) : typeof(AutoRegisteringObjectGraphType<>))
            .MakeGenericType(clrType);
    }
}
