using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;

namespace GraphQL.Reflection;

/// <summary>
/// Reflected metadata for a C# closed type hierarchy, i.e. a type declared with the <c>closed</c> modifier.
/// </summary>
/// <remarks>
/// The compiler marks a closed type with <c>System.Runtime.CompilerServices.IsClosedTypeAttribute</c> and
/// records every directly derived type in its <c>DerivedTypes</c> property. As with
/// <see cref="ClrUnionInfo"/>, the definition of that attribute comes from either the target framework or
/// from a polyfill compiled into the declaring assembly, so the marker is matched by full name rather than
/// through a compile time type reference, and is read through <see cref="CustomAttributeData"/> so that the
/// attribute never has to be instantiated.
/// </remarks>
internal sealed class ClosedTypeInfo
{
    private const string ATTRIBUTE_NAME = "IsClosedTypeAttribute";
    private const string ATTRIBUTE_FULL_NAME = "System.Runtime.CompilerServices.IsClosedTypeAttribute";
    private const string DERIVED_TYPES_MEMBER_NAME = "DerivedTypes";

    /// <summary>
    /// Guards against a hand written attribute describing a cycle. A hierarchy this deep is not something
    /// the compiler can produce, since a closed type may only be derived from within its own file.
    /// </summary>
    private const int MAXIMUM_DEPTH = 32;

    private static readonly ConcurrentDictionary<Type, ClosedTypeInfo?> _cache = new();
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<Type>> _baseTypeCache = new();

    private ClosedTypeInfo(IReadOnlyList<Type> terminalDerivedTypes)
    {
        TerminalDerivedTypes = terminalDerivedTypes;
    }

    /// <summary>
    /// Every terminal type of the hierarchy, that is, every derived type that is not itself closed.
    /// A closed type nested within the hierarchy is expanded through rather than listed, so these are
    /// exactly the types that an instance of the hierarchy can have at runtime.
    /// </summary>
    public IReadOnlyList<Type> TerminalDerivedTypes { get; }

    /// <summary>
    /// Returns metadata for the specified type, or <see langword="null"/> when it is not a closed type.
    /// The result is cached, so types that are not closed cost a single dictionary lookup thereafter.
    /// </summary>
    public static ClosedTypeInfo? Find(Type type) => _cache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), Create);

    /// <inheritdoc cref="Find(Type)"/>
    public static bool IsClosedType(Type type) => Find(type) != null;

    /// <summary>
    /// Returns every closed type that the specified type derives from, nearest ancestor first. A type
    /// below a nested closed hierarchy has more than one, and each of them describes the type as one of
    /// its terminal types, so all of them have to be represented.
    /// </summary>
    public static IReadOnlyList<Type> GetClosedBaseTypes(Type type) =>
        _baseTypeCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), CreateClosedBaseTypes);

    private static ClosedTypeInfo? Create(Type type)
    {
        if (GetDirectlyDerivedTypes(type) is not { } derivedTypes)
            return null;

        var terminalTypes = new List<Type>();
        Expand(type, derivedTypes, terminalTypes, 0);
        return new ClosedTypeInfo(terminalTypes);
    }

    private static void Expand(Type declaringType, IReadOnlyList<Type> derivedTypes, List<Type> terminalTypes, int depth)
    {
        if (depth > MAXIMUM_DEPTH)
            return;

        foreach (var derivedType in derivedTypes)
        {
            if (Close(declaringType, derivedType) is not { } closedType)
                continue;

            // a type the attribute names but which does not actually derive from the declaring type cannot
            // be part of the hierarchy; this can only happen for a hand written attribute
            if (!declaringType.IsAssignableFrom(closedType))
                continue;

            if (GetDirectlyDerivedTypes(closedType) is { } nested)
                Expand(closedType, nested, terminalTypes, depth + 1);
            else if (!terminalTypes.Contains(closedType))
                terminalTypes.Add(closedType);
        }
    }

    /// <summary>
    /// An attribute argument cannot reference the type parameters of the type it is applied to, so the
    /// compiler records an open generic type for a derived type of a generic closed hierarchy. Such a type
    /// is closed over the same arguments as the hierarchy it belongs to.
    /// </summary>
    private static Type? Close(Type declaringType, Type derivedType)
    {
        if (!derivedType.IsGenericTypeDefinition)
            return derivedType;

        if (!declaringType.IsGenericType)
            return null;

        var arguments = declaringType.GetGenericArguments();
        if (derivedType.GetGenericArguments().Length != arguments.Length)
            return null;

        try
        {
            return derivedType.MakeGenericType(arguments);
        }
        catch (ArgumentException)
        {
            // the derived type's constraints are not satisfied by the hierarchy's arguments
            return null;
        }
    }

    private static IReadOnlyList<Type>? GetDirectlyDerivedTypes(Type type)
    {
        foreach (var data in type.GetCustomAttributesData())
        {
            var attributeType = data.AttributeType;
            // Name is compared before FullName because FullName allocates for constructed and nested types
            if (attributeType.Name != ATTRIBUTE_NAME || attributeType.FullName != ATTRIBUTE_FULL_NAME)
                continue;

            return ReadDerivedTypes(data);
        }

        return null;
    }

    private static IReadOnlyList<Type> ReadDerivedTypes(CustomAttributeData data)
    {
        foreach (var argument in data.NamedArguments)
        {
            if (argument.MemberName != DERIVED_TYPES_MEMBER_NAME)
                continue;

            if (argument.TypedValue.Value is not ReadOnlyCollection<CustomAttributeTypedArgument> values)
                break;

            var derivedTypes = new List<Type>(values.Count);
            foreach (var value in values)
            {
                if (value.Value is Type derivedType)
                    derivedTypes.Add(derivedType);
            }

            return derivedTypes;
        }

        return [];
    }

    private static IReadOnlyList<Type> CreateClosedBaseTypes(Type type)
    {
        List<Type>? baseTypes = null;

        for (var baseType = type.BaseType; baseType != null && baseType != typeof(object); baseType = baseType.BaseType)
        {
            if (IsClosedType(baseType))
                (baseTypes ??= []).Add(baseType);
        }

        return (IReadOnlyList<Type>?)baseTypes ?? [];
    }
}
