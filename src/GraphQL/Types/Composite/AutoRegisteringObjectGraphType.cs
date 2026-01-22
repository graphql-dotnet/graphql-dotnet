using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types;

internal static class AutoRegisteringObjectGraphType
{
    public static ConcurrentDictionary<Type, IObjectGraphType> ReflectionCache { get; } = new();
}

/// <summary>
/// Allows you to automatically register the necessary fields for the specified type.
/// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
/// Also it can get descriptions for fields from the XML comments.
/// </summary>
public class AutoRegisteringObjectGraphType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)][NotAGraphType] TSourceType> : ObjectGraphType<TSourceType>
{
    private readonly Expression<Func<TSourceType, object?>>[]? _excludedProperties;

    /// <summary>
    /// Creates a GraphQL type from <typeparamref name="TSourceType"/>.
    /// <br/><br/>
    /// When <see cref="GlobalSwitches.EnableReflectionCaching"/> is enabled (typically for scoped schemas),
    /// be sure to place any custom initialization code within <see cref="ConfigureGraph"/> or <see cref="ProvideFields"/>
    /// so that the instance will be cached with the customizations. Also note that <see cref="ObjectGraphType{TSourceType}.Interfaces"/>
    /// will reference a shared instance of <see cref="Interfaces"/> when restored from the cache and must not be modified further.
    /// </summary>
    public AutoRegisteringObjectGraphType() : this(null) { }

    /// <summary>
    /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
    /// </summary>
    /// <param name="excludedProperties">Expressions for excluding fields, for example 'o => o.Age'.</param>
    public AutoRegisteringObjectGraphType(params Expression<Func<TSourceType, object?>>[]? excludedProperties)
        : this(
            GlobalSwitches.EnableReflectionCaching && excludedProperties == null && AutoRegisteringObjectGraphType.ReflectionCache.TryGetValue(typeof(TSourceType), out var cacheEntry)
                ? (AutoRegisteringObjectGraphType<TSourceType>?)cacheEntry
                : null,
            excludedProperties,
            GlobalSwitches.EnableReflectionCaching)
    {
    }

    internal AutoRegisteringObjectGraphType(AutoRegisteringObjectGraphType<TSourceType>? cloneFrom, Expression<Func<TSourceType, object?>>[]? excludedProperties, bool cache)
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
            ResolvedInterfaces.Count == 0)
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
                AutoRegisteringObjectGraphType.ReflectionCache[typeof(TSourceType)] = new AutoRegisteringObjectGraphType<TSourceType>(this, null, false);
        }
    }

    /// <summary>
    /// Applies default configuration settings to this graph type along with any <see cref="GraphQLAttribute"/> attributes marked on <typeparamref name="TSourceType"/>.
    /// Allows the ability to override the default naming convention used by this class without affecting attributes applied directly to <typeparamref name="TSourceType"/>.
    /// </summary>
    protected virtual void ConfigureGraph()
    {
        AutoRegisteringHelper.ApplyGraphQLAttributes<TSourceType>(this);
    }

    /// <inheritdoc cref="AutoRegisteringHelper.ProvideFields(IEnumerable{MemberInfo}, Func{MemberInfo, FieldType?}, bool)"/>
    protected virtual IEnumerable<FieldType> ProvideFields()
        => AutoRegisteringHelper.ProvideFields(GetRegisteredMembers(), CreateField, false);

    /// <summary>
    /// Processes the specified member and returns a <see cref="FieldType"/>.
    /// May return <see langword="null"/> to skip a member.
    /// </summary>
    protected virtual FieldType? CreateField(MemberInfo memberInfo)
        => AutoRegisteringHelper.CreateField(this, memberInfo, GetTypeInformation, BuildFieldType, false);

    /// <inheritdoc cref="AutoRegisteringOutputHelper.BuildFieldType(MemberInfo, FieldType, Func{MemberInfo, LambdaExpression}?, Func{Type, Func{FieldType, ParameterInfo, ArgumentInformation}}, Action{ParameterInfo, QueryArgument}, Func{Type, Func{ArgumentInformation, LambdaExpression?}})"/>
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
            BuildMemberInstanceExpression,
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

    private static readonly MethodInfo _getParameterResolverInternalMethodInfo = typeof(AutoRegisteringObjectGraphType<TSourceType>).GetMethod(nameof(GetParameterResolverInternal), BindingFlags.NonPublic | BindingFlags.Instance)!;
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

    /// <summary>
    /// Returns a lambda expression that will be used by the field resolver to access the member.
    /// <br/><br/>
    /// Typically this is a lambda expression of type <see cref="Func{T, TResult}">Func</see>&lt;<see cref="IResolveFieldContext"/>, <typeparamref name="TSourceType"/>&gt;.
    /// <br/><br/>
    /// By default this returns the <see cref="IResolveFieldContext.Source"/> property,
    /// unless <see cref="InstanceSourceAttribute"/> is applied to <typeparamref name="TSourceType"/> to specify
    /// a different instance source.
    /// </summary>
    /// <param name="memberInfo">The member being called or accessed.</param>
    protected virtual LambdaExpression BuildMemberInstanceExpression(MemberInfo memberInfo)
        => _sourceExpression;

    private static readonly Expression<Func<IResolveFieldContext, TSourceType>> _sourceExpression = AutoRegisteringOutputHelper.BuildSourceExpression<TSourceType>();

    private static readonly MethodInfo _getArgumentInformationInternalMethodInfo = typeof(AutoRegisteringObjectGraphType<TSourceType>).GetMethod(nameof(GetArgumentInformationInternal), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private ArgumentInformation GetArgumentInformationInternal<TParameterType>(FieldType fieldType, ParameterInfo parameterInfo)
        => GetArgumentInformation<TParameterType>(fieldType, parameterInfo);

    /// <inheritdoc cref="AutoRegisteringOutputHelper.ApplyArgumentAttributes(ParameterInfo, QueryArgument)"/>
    protected virtual void ApplyArgumentAttributes(ParameterInfo parameterInfo, QueryArgument queryArgument)
        => AutoRegisteringOutputHelper.ApplyArgumentAttributes(parameterInfo, queryArgument);

    /// <inheritdoc cref="AutoRegisteringOutputHelper.GetArgumentInformation{TSourceType}(TypeInformation, FieldType, ParameterInfo)"/>
    protected virtual ArgumentInformation GetArgumentInformation<TParameterType>(FieldType fieldType, ParameterInfo parameterInfo)
        => AutoRegisteringOutputHelper.GetArgumentInformation<TSourceType>(GetTypeInformation(parameterInfo), fieldType, parameterInfo);

    /// <inheritdoc cref="AutoRegisteringOutputHelper.GetRegisteredMembers{TSourceType}(Expression{Func{TSourceType, object?}}[])"/>
    protected virtual IEnumerable<MemberInfo> GetRegisteredMembers()
        => AutoRegisteringOutputHelper.GetRegisteredMembers(_excludedProperties);

    /// <inheritdoc cref="AutoRegisteringHelper.GetTypeInformation(MemberInfo, bool)"/>
    protected virtual TypeInformation GetTypeInformation(MemberInfo memberInfo)
        => AutoRegisteringHelper.GetTypeInformation(memberInfo, false);

    /// <inheritdoc cref="AutoRegisteringHelper.GetTypeInformation(ParameterInfo)"/>
    protected virtual TypeInformation GetTypeInformation(ParameterInfo parameterInfo)
        => AutoRegisteringHelper.GetTypeInformation(parameterInfo);
}
