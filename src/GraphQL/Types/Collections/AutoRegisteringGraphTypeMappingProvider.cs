using System.Reflection;
using GraphQL.Reflection;

namespace GraphQL.Types;

/// <summary>
/// Maps unmapped complex types to <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
/// and <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/>.
/// <br/><br/>
/// A C# <c>union</c> output type is mapped to <see cref="AutoRegisteringUnionGraphType{TSourceType}"/>, and
/// a type declared with the <c>closed</c> modifier is mapped to
/// <see cref="AutoRegisteringInterfaceGraphType{TSourceType}"/>, since both describe a fixed set of
/// alternatives that GraphQL can express directly.
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
    public AutoRegisteringGraphTypeMappingProvider()
        : this(true, true)
    {
    }

    /// <summary>
    /// Creates an instance that maps input and/or output types, as specified.
    /// When output types are enabled, <paramref name="mapInterfaceTypes"/> indicates whether CLR
    /// interface output types are mapped as GraphQL interfaces or GraphQL object types.
    /// </summary>
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(AutoRegisteringUnionGraphType<>))]
    public virtual Type? GetGraphTypeFromClrType(Type clrType, bool isInputType, Type? preferredType)
    {
        if (preferredType != null)
            return preferredType;

        if (isInputType && !_mapInputTypes && !IsForcedType(clrType) ||
            !isInputType && !_mapOutputTypes && !IsForcedType(clrType) ||
            clrType.IsEnum ||
            SchemaTypes.BuiltInScalarMappings.ContainsKey(clrType))
            return null;

        if (isInputType)
        {
            // GraphQL has no input union, so a union reaching an input position cannot be represented at all
            if (ClrUnionInfo.IsUnion(clrType))
                throw new InvalidOperationException($"The union type '{clrType.GetFriendlyName()}' cannot be used as an input type, because GraphQL has no union input type. Use a separate input type that carries the case explicitly.");

            return typeof(AutoRegisteringInputObjectGraphType<>).MakeGenericType(clrType);
        }
        else if (ClrUnionInfo.IsUnion(clrType))
        {
            return typeof(AutoRegisteringUnionGraphType<>).MakeGenericType(clrType);
        }
        else if (ClosedTypeInfo.IsClosedType(clrType) && _mapInterfaceTypes)
        {
            return typeof(AutoRegisteringInterfaceGraphType<>).MakeGenericType(clrType);
        }
        else if (clrType.IsInterface && _mapInterfaceTypes)
        {
            return typeof(AutoRegisteringInterfaceGraphType<>).MakeGenericType(clrType);
        }
        else
        {
            return typeof(AutoRegisteringObjectGraphType<>).MakeGenericType(clrType);
        }

        static bool IsForcedType(Type type) => type.GetCustomAttribute<MapAutoClrTypeAttribute>() != null;
    }
}
