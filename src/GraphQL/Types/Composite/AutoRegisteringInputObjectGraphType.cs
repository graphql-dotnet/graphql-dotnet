using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types;

internal static class AutoRegisteringInputObjectGraphType
{
    public static ConcurrentDictionary<Type, IInputObjectGraphType> ReflectionCache { get; } = new();
}

/// <summary>
/// Allows you to automatically register the necessary fields for the specified input type.
/// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
/// Also it can get descriptions for fields from the XML comments.
/// Note that now __InputValue has no isDeprecated and deprecationReason fields but in the future they may appear - https://github.com/graphql/graphql-spec/pull/525
/// </summary>
public class AutoRegisteringInputObjectGraphType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields)][NotAGraphType] TSourceType> : InputObjectGraphType<TSourceType>
{
    private readonly Expression<Func<TSourceType, object?>>[]? _excludedProperties;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/> class without adding any fields.
    /// </summary>
    /// <param name="configureGraph">When true, sets the name and processes all attributes defined for the source type.</param>
    internal AutoRegisteringInputObjectGraphType(bool configureGraph)
        : base(configureGraph)
    {
        if (configureGraph)
        {
            Name = typeof(TSourceType).GraphQLName();
            ConfigureGraph();
        }
    }

    /// <summary>
    /// Creates a GraphQL type from <typeparamref name="TSourceType"/>.
    /// <br/><br/>
    /// When <see cref="GlobalSwitches.EnableReflectionCaching"/> is enabled (typically for scoped schemas),
    /// be sure to place any custom initialization code within <see cref="ConfigureGraph"/> or <see cref="ProvideFields"/>
    /// so that the instance will be cached with the customizations.
    /// </summary>
    [RequiresDynamicCode("Builds input resolvers at runtime, requiring dynamic code generation.")]
    public AutoRegisteringInputObjectGraphType() : this(null) { }

    /// <summary>
    /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
    /// </summary>
    /// <param name="excludedProperties">Expressions for excluding fields, for example 'o => o.Age'.</param>
    [RequiresDynamicCode("Builds input resolvers at runtime, requiring dynamic code generation.")]
    public AutoRegisteringInputObjectGraphType(params Expression<Func<TSourceType, object?>>[]? excludedProperties)
        : this(
            GlobalSwitches.EnableReflectionCaching && excludedProperties == null && AutoRegisteringInputObjectGraphType.ReflectionCache.TryGetValue(typeof(TSourceType), out var cacheEntry)
                ? (AutoRegisteringInputObjectGraphType<TSourceType>?)cacheEntry
                : null,
            excludedProperties,
            GlobalSwitches.EnableReflectionCaching)
    {
    }

    [RequiresDynamicCode("Builds input resolvers at runtime, requiring dynamic code generation.")]
    private AutoRegisteringInputObjectGraphType(AutoRegisteringInputObjectGraphType<TSourceType>? cloneFrom, Expression<Func<TSourceType, object?>>[]? excludedProperties, bool cache)
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
        if (cache && excludedProperties == null)
        {
            foreach (var f in Fields.List)
            {
                if (f.ResolvedType != null)
                    cache = false;
            }

            // cache the constructed object
            if (cache)
                AutoRegisteringInputObjectGraphType.ReflectionCache[typeof(TSourceType)] = new AutoRegisteringInputObjectGraphType<TSourceType>(this, null, false);
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
        => AutoRegisteringHelper.ProvideFields(GetRegisteredMembers(), CreateField, true);

    /// <summary>
    /// Processes the specified property or field and returns a <see cref="FieldType"/>.
    /// May return <see langword="null"/> to skip a property.
    /// </summary>
    protected virtual FieldType? CreateField(MemberInfo memberInfo)
        => AutoRegisteringHelper.CreateField(this, memberInfo, GetTypeInformation, null, true);

    /// <summary>
    /// Returns a list of properties or fields that should have fields created for them.
    /// Unless overridden, returns a list of public instance writable properties,
    /// including properties on inherited classes.
    /// <br/><br/>
    /// The <see cref="MemberScanAttribute"/> can be applied to <typeparamref name="TSourceType"/> to control
    /// which member types are scanned (properties and/or fields). Methods are not supported for input types.
    /// </summary>
    protected virtual IEnumerable<MemberInfo> GetRegisteredMembers()
    {
        // Check for MemberScanAttribute to determine which member types to scan
        // Default for input types is Properties only (not methods)
        var memberScanAttr = typeof(TSourceType).GetCustomAttribute<MemberScanAttribute>();
        var memberTypes = memberScanAttr?.MemberTypes ?? ScanMemberTypes.Properties;

        // Note: for input types, methods are skipped regardless of the attribute setting, as the same attribute may be used for output types
        var scanProperties = memberTypes.HasFlag(ScanMemberTypes.Properties);
        var scanFields = memberTypes.HasFlag(ScanMemberTypes.Fields);

        List<MemberInfo> members = [];

        if (scanProperties)
        {
            // determine which constructor will be used to create the object, or null if unknown (perhaps due to overriding ParseDictionary)
            var constructor = AutoRegisteringHelper.GetConstructorOrDefault<TSourceType>();
            // get constructor's parameters
            var constructorParameters = constructor?.GetParameters();
            // get constructor's parameter names
            var parameters = constructorParameters == null || constructorParameters.Length == 0 ? null : constructorParameters.Select(x => x.Name).ToArray();
            // define PropertyInfo predicate based on whether the constructor has any parameters
            Func<PropertyInfo, bool> predicate = parameters == null
                // any writable property
                ? static x => x.SetMethod?.IsPublic ?? false
                // any writable property, or any read-only property that has a constructor parameter
                : x => (x.SetMethod?.IsPublic ?? false) || parameters.Contains(x.Name, StringComparer.InvariantCultureIgnoreCase);
            // get the list of properties, excluding ones specifically specified in the AutoRegisteringInputGraphType constructor
            var properties = AutoRegisteringHelper.ExcludeProperties(
                typeof(TSourceType).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(predicate),
                _excludedProperties);
            members.AddRange(properties);
        }

        if (scanFields)
        {
            // get public instance fields which are not readonly
            var fields = typeof(TSourceType).GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => !x.IsInitOnly);
            members.AddRange(fields);
        }

        return members;
    }

    /// <inheritdoc cref="AutoRegisteringHelper.GetTypeInformation(MemberInfo, bool)"/>
    /// <remarks>
    /// Only properties and fields are supported.
    /// </remarks>
    protected virtual TypeInformation GetTypeInformation(MemberInfo memberInfo)
        => AutoRegisteringHelper.GetTypeInformation(memberInfo, true);
}
