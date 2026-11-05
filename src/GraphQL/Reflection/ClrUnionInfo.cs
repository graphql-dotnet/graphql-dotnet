using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GraphQL.Reflection;

/// <summary>
/// Reflected metadata for a C# union type, i.e. the type the compiler emits for a <c>union</c> declaration.
/// </summary>
/// <remarks>
/// The compiler marks the emitted type with <c>System.Runtime.CompilerServices.UnionAttribute</c>, but the
/// definition of that attribute comes from either the target framework or from a polyfill compiled into the
/// declaring assembly, so every assembly may hold its own distinct copy of it. The marker is therefore
/// matched by full name rather than through a compile time type reference. That is also the only option
/// available here, since this library targets frameworks that predate the attribute entirely.
/// <br/><br/>
/// For the same reason the active case is read through the conventional public <c>object Value</c> property
/// rather than through <c>System.Runtime.CompilerServices.IUnion</c>, which the compiler documents as
/// optional to implement.
/// </remarks>
internal sealed class ClrUnionInfo
{
    private const string UNION_ATTRIBUTE_NAME = "UnionAttribute";
    private const string UNION_ATTRIBUTE_FULL_NAME = "System.Runtime.CompilerServices.UnionAttribute";
    private const string VALUE_PROPERTY_NAME = "Value";

    private static readonly ConcurrentDictionary<Type, ClrUnionInfo?> _cache = new();

    private readonly Func<object, object?>? _valueAccessor;
    private readonly Type _unionType;

    private ClrUnionInfo(Type unionType, IReadOnlyList<Type> caseTypes, Func<object, object?>? valueAccessor)
    {
        _unionType = unionType;
        _valueAccessor = valueAccessor;
        CaseTypes = caseTypes;
    }

    /// <summary>
    /// The CLR type of each case of the union, in declaration order.
    /// </summary>
    public IReadOnlyList<Type> CaseTypes { get; }

    /// <summary>
    /// Returns metadata for the specified type, or <see langword="null"/> when it is not a C# union.
    /// The result is cached, so types that are not unions cost a single dictionary lookup thereafter.
    /// </summary>
    public static ClrUnionInfo? Find(Type type) => _cache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), Create);

    /// <inheritdoc cref="Find(Type)"/>
    public static bool IsUnion(Type type) => Find(type) != null;

    /// <summary>
    /// Returns the value held by the active case of the specified union instance, or <see langword="null"/>
    /// when the union holds no value.
    /// </summary>
    public object? GetValue(object union)
    {
        if (_valueAccessor == null)
        {
            throw new InvalidOperationException($"The union type '{_unionType.GetFriendlyName()}' does not expose a public 'object Value' property, so the value of its active case cannot be read.");
        }

        return _valueAccessor(union);
    }

    private static ClrUnionInfo? Create(Type type)
    {
        if (!IsMarkedAsUnion(type))
            return null;

        return new ClrUnionInfo(type, GetCaseTypes(type), CreateValueAccessor(type));
    }

    private static bool IsMarkedAsUnion(Type type)
    {
        foreach (var data in type.GetCustomAttributesData())
        {
            var attributeType = data.AttributeType;
            // Name is compared before FullName because FullName allocates for constructed and nested types
            if (attributeType.Name == UNION_ATTRIBUTE_NAME && attributeType.FullName == UNION_ATTRIBUTE_FULL_NAME)
                return true;
        }

        return false;
    }

    /// <summary>
    /// The compiler emits one public single-parameter constructor per case, so the cases are read back from
    /// those constructors. Parameters of a compiler generated type are skipped, as are duplicates, which a
    /// union declaring both <c>T</c> and <c>T?</c> for a reference type produces.
    /// </summary>
    private static IReadOnlyList<Type> GetCaseTypes(Type type)
    {
        var caseTypes = new List<Type>();

        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length != 1)
                continue;

            var caseType = parameters[0].ParameterType;
            if (caseType.IsByRef ||
                caseType.GetCustomAttribute<CompilerGeneratedAttribute>() != null ||
                caseTypes.Contains(caseType))
                continue;

            caseTypes.Add(caseType);
        }

        return caseTypes;
    }

    private static Func<object, object?>? CreateValueAccessor(Type type)
    {
        var property = type.GetProperty(VALUE_PROPERTY_NAME, BindingFlags.Public | BindingFlags.Instance);
        if (property == null ||
            property.PropertyType != typeof(object) ||
            !(property.GetMethod?.IsPublic ?? false) ||
            property.GetIndexParameters().Length > 0)
            return null;

        var parameter = Expression.Parameter(typeof(object), "union");
        var body = Expression.Property(Expression.Convert(parameter, type), property);
        return Expression.Lambda<Func<object, object?>>(body, parameter).Compile();
    }
}
