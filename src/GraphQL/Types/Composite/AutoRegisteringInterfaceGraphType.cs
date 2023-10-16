using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types;

internal static class AutoRegisteringInterfaceGraphType
{
    public static ConcurrentDictionary<Type, IInterfaceGraphType> ReflectionCache { get; } = new();
}

/// <summary>
/// Allows you to automatically register the necessary fields for the specified type.
/// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
/// Also it can get descriptions for fields from the XML comments.
/// </summary>
public class AutoRegisteringInterfaceGraphType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] TSourceType> : InterfaceGraphType<TSourceType>
{
    private readonly Expression<Func<TSourceType, object?>>[]? _excludedProperties;

    /// <summary>
    /// Creates a GraphQL type from <typeparamref name="TSourceType"/>.
    /// <br/><br/>
    /// When <see cref="GlobalSwitches.EnableReflectionCaching"/> is enabled (typically for scoped schemas),
    /// be sure to place any custom initialization code within <see cref="ConfigureGraph"/> or <see cref="ProvideFields"/>
    /// so that the instance will be cached with the customizations, except when using <see cref="InterfaceGraphType{TSource}.AddPossibleType(IObjectGraphType)">AddPossibleType</see>,
    /// <see cref="InterfaceGraphType{TSource}.PossibleTypes">PossibleTypes</see> or <see cref="InterfaceGraphType{TSource}.ResolveType">ResolveType</see> as these values cannot be cached.
    /// </summary>
    public AutoRegisteringInterfaceGraphType() : this(null) { }

    /// <summary>
    /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
    /// </summary>
    /// <param name="excludedProperties">Expressions for excluding fields, for example 'o => o.Age'.</param>
    public AutoRegisteringInterfaceGraphType(params Expression<Func<TSourceType, object?>>[]? excludedProperties)
        : this(
            GlobalSwitches.EnableReflectionCaching && excludedProperties == null && AutoRegisteringInterfaceGraphType.ReflectionCache.TryGetValue(typeof(TSourceType), out var cacheEntry)
                ? (AutoRegisteringInterfaceGraphType<TSourceType>?)cacheEntry
                : null,
            excludedProperties,
            GlobalSwitches.EnableReflectionCaching)
    {
    }

    internal AutoRegisteringInterfaceGraphType(AutoRegisteringInterfaceGraphType<TSourceType>? cloneFrom, Expression<Func<TSourceType, object?>>[]? excludedProperties, bool cache)
        : base(cloneFrom)
    {
        // if copying a cached instance, just return the instance
        if (cloneFrom != null)
            return;

        _excludedProperties = excludedProperties;
        Name = typeof(TSourceType).GraphQLName();
        ConfigureGraph();
        foreach (var fieldType in ProvideFields())
        {
            _ = AddField(fieldType);
        }

        // cache the instance if reflection caching is enabled
        if (cache &&
            excludedProperties == null &&
            PossibleTypes.Count == 0 &&
            ResolveType == null)
        {
            foreach (var f in Fields.List)
            {
                if (f.ResolvedType != null)
                    cache = false;

                if (f.Arguments?.List != null)
                {
                    foreach (var a in f.Arguments.List)
                    {
                        if (a.ResolvedType != null || a.Type == null)
                            cache = false;
                    }
                }
            }

            // cache the constructed object
            if (cache)
                AutoRegisteringInterfaceGraphType.ReflectionCache[typeof(TSourceType)] = new AutoRegisteringInterfaceGraphType<TSourceType>(this, null, false);
        }
    }

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.ConfigureGraph"/>
    protected virtual void ConfigureGraph()
    {
        AutoRegisteringHelper.ApplyGraphQLAttributes<TSourceType>(this);
    }

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.ProvideFields"/>
    protected virtual IEnumerable<FieldType> ProvideFields()
        => AutoRegisteringHelper.ProvideFields(GetRegisteredMembers(), CreateField, false);

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.CreateField(MemberInfo)"/>
    protected virtual FieldType? CreateField(MemberInfo memberInfo)
        => AutoRegisteringHelper.CreateField(memberInfo, GetTypeInformation, BuildFieldType, false);

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.BuildFieldType(FieldType, MemberInfo)"/>
    protected void BuildFieldType(FieldType fieldType, MemberInfo memberInfo)
    {
        Func<Type, Func<FieldType, ParameterInfo, ArgumentInformation>> getTypedArgumentInfoMethod =
            parameterType =>
            {
                var getArgumentInfoMethodInfo = _getArgumentInformationInternalMethodInfo.MakeGenericMethod(parameterType);
                return (Func<FieldType, ParameterInfo, ArgumentInformation>)getArgumentInfoMethodInfo.CreateDelegate(typeof(Func<FieldType, ParameterInfo, ArgumentInformation>), this);
            };

        AutoRegisteringOutputHelper.BuildFieldType(
            memberInfo,
            fieldType,
            null,
            getTypedArgumentInfoMethod,
            ApplyArgumentAttributes);
    }

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.BuildMemberInstanceExpression(MemberInfo)"/>
    [Obsolete("Interface graph types do not support field resolvers; use of this method is unnecessary.")]
    protected virtual LambdaExpression BuildMemberInstanceExpression(MemberInfo memberInfo)
        => throw new NotSupportedException("Interface graph types do not support field resolvers");

    private static readonly MethodInfo _getArgumentInformationInternalMethodInfo = typeof(AutoRegisteringInterfaceGraphType<TSourceType>).GetMethod(nameof(GetArgumentInformationInternal), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private ArgumentInformation GetArgumentInformationInternal<TParameterType>(FieldType fieldType, ParameterInfo parameterInfo)
        => GetArgumentInformation<TParameterType>(fieldType, parameterInfo);

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.ApplyArgumentAttributes(ParameterInfo, QueryArgument)"/>
    protected virtual void ApplyArgumentAttributes(ParameterInfo parameterInfo, QueryArgument queryArgument)
        => AutoRegisteringOutputHelper.ApplyArgumentAttributes(parameterInfo, queryArgument);

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.GetArgumentInformation{TParameterType}(FieldType, ParameterInfo)"/>
    protected virtual ArgumentInformation GetArgumentInformation<TParameterType>(FieldType fieldType, ParameterInfo parameterInfo)
        => AutoRegisteringOutputHelper.GetArgumentInformation<TSourceType>(GetTypeInformation(parameterInfo), fieldType, parameterInfo);

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.GetRegisteredMembers"/>
    protected virtual IEnumerable<MemberInfo> GetRegisteredMembers()
        => AutoRegisteringOutputHelper.GetRegisteredMembers(_excludedProperties);

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.GetTypeInformation(MemberInfo)"/>
    protected virtual TypeInformation GetTypeInformation(MemberInfo memberInfo)
        => AutoRegisteringHelper.GetTypeInformation(memberInfo, false);

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.GetTypeInformation(ParameterInfo)"/>
    protected virtual TypeInformation GetTypeInformation(ParameterInfo parameterInfo)
        => AutoRegisteringHelper.GetTypeInformation(parameterInfo);
}
