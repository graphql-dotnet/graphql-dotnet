namespace GraphQL;

/// <summary>
/// Registers a generic CLR type as an optional wrapper for input object members.
/// </summary>
/// <remarks>
/// Apply this attribute to an assembly or module loaded before schema construction, such as
/// the application or schema assembly. The wrapper may be defined in a different assembly.
/// Registrations apply globally to input bindings; output bindings are unchanged.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module, AllowMultiple = true)]
public sealed class OptionalTypeAttribute : Attribute
{
    /// <summary>
    /// Registers an open generic type with one type parameter, a public parameterless constructor
    /// (implicit for structs), and a public constructor accepting the type parameter.
    /// </summary>
    /// <param name="optionalType">The open generic wrapper type, such as <c>typeof(MyOptional&lt;&gt;)</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="optionalType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="optionalType"/> does not have the required shape.</exception>
    public OptionalTypeAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type optionalType)
    {
        if (optionalType == null)
            throw new ArgumentNullException(nameof(optionalType));
        if (!optionalType.IsGenericTypeDefinition || optionalType.GetGenericArguments().Length != 1 || optionalType.IsAbstract)
            throw new ArgumentException("An optional type must be a non-abstract open generic type with one type parameter.", nameof(optionalType));
        if (!optionalType.IsValueType && optionalType.GetConstructor(Type.EmptyTypes) == null)
            throw new ArgumentException("An optional type must have a public parameterless constructor.", nameof(optionalType));
        var valueType = optionalType.GetGenericArguments()[0];
        var constructor = optionalType.GetConstructor([valueType]);
        if (constructor == null || constructor.GetParameters()[0].ParameterType != valueType)
            throw new ArgumentException("An optional type must have a public constructor accepting its type parameter.", nameof(optionalType));

        OptionalType = optionalType;
    }

    /// <summary>
    /// Gets the registered open generic wrapper type. No particular properties or fields are required.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type OptionalType { get; }
}
