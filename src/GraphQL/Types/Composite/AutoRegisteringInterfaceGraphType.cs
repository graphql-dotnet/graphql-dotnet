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
public class AutoRegisteringInterfaceGraphType<[DynamicallyAccessedMembers(
#if NET6_0_OR_GREATER
    DynamicallyAccessedMemberTypes.Interfaces |
#endif
    DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)][NotAGraphType] TSourceType> : InterfaceGraphType<TSourceType>
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
    [RequiresUnreferencedCode("Scans the specified type for public methods and properties, which may not be statically referenced.")]
    [RequiresDynamicCode("Builds resolvers at runtime, requiring dynamic code generation.")]
    public AutoRegisteringInterfaceGraphType() : this(null) { }

    /// <summary>
    /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
    /// </summary>
    /// <param name="excludedProperties">Expressions for excluding fields, for example 'o => o.Age'.</param>
    [RequiresUnreferencedCode("Scans the specified type for public methods and properties, which may not be statically referenced.")]
    [RequiresDynamicCode("Builds resolvers at runtime, requiring dynamic code generation.")]
    public AutoRegisteringInterfaceGraphType(params Expression<Func<TSourceType, object?>>[]? excludedProperties)
        : this(
            GlobalSwitches.EnableReflectionCaching && excludedProperties == null && AutoRegisteringInterfaceGraphType.ReflectionCache.TryGetValue(typeof(TSourceType), out var cacheEntry)
                ? (AutoRegisteringInterfaceGraphType<TSourceType>?)cacheEntry
                : null,
            excludedProperties,
            GlobalSwitches.EnableReflectionCaching)
    {
    }

    [RequiresUnreferencedCode("Scans the specified type for public methods and properties, which may not be statically referenced.")]
    [RequiresDynamicCode("Builds resolvers at runtime, requiring dynamic code generation.")]
    private AutoRegisteringInterfaceGraphType(AutoRegisteringInterfaceGraphType<TSourceType>? cloneFrom, Expression<Func<TSourceType, object?>>[]? excludedProperties, bool cache)
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
    {
        var field = AutoRegisteringHelper.CreateField(this, memberInfo, GetTypeInformation, BuildFieldType, false);
        // clear the field resolver after the attributes are applied, which may set IsPrivate
        // private fields are ignored, as [FederationResolver] uses a private field and requires the resolver to execute
        if (field != null && !field.IsPrivate)
        {
            field.Resolver = null;
            field.StreamResolver = null;
        }
        return field;
    }

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.BuildFieldType(FieldType, MemberInfo)"/>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "The constructor is marked with RequiresDynamicCodeAttribute.")]
    protected void BuildFieldType(FieldType fieldType, MemberInfo memberInfo)
    {
        Func<Type, Func<FieldType, ParameterInfo, ArgumentInformation>> getTypedArgumentInfoMethod =
            parameterType =>
            {
                var getArgumentInfoMethodInfo = _getArgumentInformationInternalMethodInfo.MakeGenericMethod(parameterType);
                return (Func<FieldType, ParameterInfo, ArgumentInformation>)getArgumentInfoMethodInfo.CreateDelegate(typeof(Func<FieldType, ParameterInfo, ArgumentInformation>), this);
            };

        Func<Type, Func<ArgumentInformation, LambdaExpression?>> getTypedParameterResolverMethod =
            parameterType =>
            {
                var getParameterResolverMethodInfo = _getParameterResolverInternalMethodInfo.MakeGenericMethod(parameterType);
                return (Func<ArgumentInformation, LambdaExpression?>)getParameterResolverMethodInfo.CreateDelegate(typeof(Func<ArgumentInformation, LambdaExpression?>), this);
            };

        AutoRegisteringOutputHelper.BuildFieldType(
            memberInfo,
            fieldType,
            null,
            getTypedArgumentInfoMethod,
            ApplyArgumentAttributes,
            getTypedParameterResolverMethod);
    }

    /// <summary>
    /// Returns a resolver function for the specified parameter using <see cref="ParameterAttribute"/> if present,
    /// or returns a built-in resolver for <see cref="IResolveFieldContext"/> or <see cref="CancellationToken"/>.
    /// Returns <see langword="null"/> if no parameter attribute is found and the parameter type is not a built-in type.
    /// </summary>
    /// <typeparam name="TParameterType">The CLR type of the method parameter.</typeparam>
    /// <param name="argumentInformation">The argument information for the parameter.</param>
    /// <returns>A function that resolves the parameter value, or <see langword="null"/> if no resolver is available.</returns>
    protected virtual Func<IResolveFieldContext, TParameterType>? GetParameterResolver<TParameterType>(ArgumentInformation argumentInformation)
        => AutoRegisteringHelper.GetParameterResolver<TParameterType>(argumentInformation);

    private static readonly MethodInfo _getParameterResolverInternalMethodInfo = typeof(AutoRegisteringInterfaceGraphType<TSourceType>).GetMethod(nameof(GetParameterResolverInternal), BindingFlags.NonPublic | BindingFlags.Instance)!;
    [RequiresDynamicCode("Uses Expression.Lambda which requires dynamic code generation.")]
    private LambdaExpression? GetParameterResolverInternal<TParameterType>(ArgumentInformation argumentInformation)
    {
        var func = GetParameterResolver<TParameterType>(argumentInformation);
        if (func == null)
            return null;

        // Convert the Func<IResolveFieldContext, TParameterType> to Expression<Func<IResolveFieldContext, TParameterType>>
        Expression<Func<IResolveFieldContext, TParameterType>> expression = context => func(context);
        return expression;
    }

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
    [UnconditionalSuppressMessage("AOT", "IL2026:Calling members annotated with 'RequiresUnreferencedCodeAttribute' may break functionality when trimming application code.",
        Justification = "The constructor is marked with RequiresUnreferencedCodeAttribute.")]
    protected virtual IEnumerable<MemberInfo> GetRegisteredMembers()
        => AutoRegisteringOutputHelper.GetRegisteredMembers(_excludedProperties);

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.GetTypeInformation(MemberInfo)"/>
    protected virtual TypeInformation GetTypeInformation(MemberInfo memberInfo)
        => AutoRegisteringHelper.GetTypeInformation(memberInfo, false);

    /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.GetTypeInformation(ParameterInfo)"/>
    protected virtual TypeInformation GetTypeInformation(ParameterInfo parameterInfo)
        => AutoRegisteringHelper.GetTypeInformation(parameterInfo);
}
