using System.Reflection;

namespace GraphQL.Types;

/// <summary>
/// Maps unmapped complex types to <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
/// and <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/>.
/// </summary>
public class AutoRegisteringGraphTypeMappingProvider : IGraphTypeMappingProvider
{
    private readonly bool _mapInputTypes;
    private readonly bool _mapOutputTypes;
    private readonly bool _mapInterfaceTypes;

    /// <summary>
    /// Creates an instance that maps both input and output types.
    /// CLR interface output types will be mapped as GraphQL interfaces.
    /// </summary>
    [RequiresDynamicCode("Creating generic types requires dynamic code.")]
    public AutoRegisteringGraphTypeMappingProvider()
        : this(true, true)
    {
    }

    /// <summary>
    /// Creates an instance that maps input and/or output types, as specified.
    /// When output types are enabled, <paramref name="mapInterfaceTypes"/> indicates whether CLR
    /// interface output types are mapped as GraphQL interfaces or GraphQL object types.
    /// </summary>
    [RequiresDynamicCode("Creating generic types requires dynamic code.")]
    public AutoRegisteringGraphTypeMappingProvider(bool mapInputTypes, bool mapOutputTypes, bool mapInterfaceTypes = true)
    {
        _mapInputTypes = mapInputTypes;
        _mapOutputTypes = mapOutputTypes;
        _mapInterfaceTypes = mapInterfaceTypes;
    }

    /// <inheritdoc/>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(AutoRegisteringObjectGraphType<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(AutoRegisteringInterfaceGraphType<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(AutoRegisteringInputObjectGraphType<>))]
    [UnconditionalSuppressMessage("Trimming", "IL3050: Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT")]
    public virtual Type? GetGraphTypeFromClrType(Type clrType, bool isInputType, Type? preferredType)
    {
        if (preferredType != null)
            return preferredType;

        // Check if this type has MapAutoClrTypeAttribute and get the CLR type from it if specified
        var attribute = clrType.GetCustomAttribute<MapAutoClrTypeAttribute>();
        var mappingClrType = attribute?.ClrType ?? clrType;

        if (isInputType && !_mapInputTypes && attribute == null ||
            !isInputType && !_mapOutputTypes && attribute == null)
            return null;

        if (isInputType)
        {
            return typeof(AutoRegisteringInputObjectGraphType<>).MakeGenericType(mappingClrType);
        }
        else if (mappingClrType.IsInterface && _mapInterfaceTypes)
        {
            return typeof(AutoRegisteringInterfaceGraphType<>).MakeGenericType(mappingClrType);
        }
        else
        {
            return typeof(AutoRegisteringObjectGraphType<>).MakeGenericType(mappingClrType);
        }
    }
}
