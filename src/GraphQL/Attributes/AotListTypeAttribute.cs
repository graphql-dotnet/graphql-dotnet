namespace GraphQL;

/// <summary>
/// Specifies a CLR list type that should be registered for AOT schema compilation.
/// Use this to ensure list types such as <see cref="IEnumerable{T}"/>,
/// arrays, or custom collection types are properly handled during ahead-of-time compilation.
/// This attribute can be applied multiple times to specify multiple list types.
/// </summary>
/// <typeparam name="TListType">
/// The CLR list type to register, such as <see cref="IEnumerable{T}"/>, <c>int[]</c>,
/// <see cref="List{T}"/>, or other collection types.
/// </typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AotListTypeAttribute<[NotAGraphType] TListType> : AotSchemaAttribute
{
}

/// <summary>
/// Specifies a CLR list interface and its corresponding implementation type for AOT schema compilation.
/// Use this when you need to map a collection interface to a specific concrete implementation,
/// such as mapping <see cref="ISet{T}"/> to <see cref="HashSet{T}"/>.
/// This attribute can be applied multiple times to specify multiple list type mappings.
/// </summary>
/// <typeparam name="TListInterface">
/// The CLR list interface type, such as <see cref="ISet{T}"/>, <see cref="ICollection{T}"/>,
/// or other collection interface types.
/// </typeparam>
/// <typeparam name="TListImplementation">
/// The concrete implementation type that implements <typeparamref name="TListInterface"/>,
/// such as <see cref="HashSet{T}"/>, <see cref="List{T}"/>, or other collection implementation types.
/// </typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AotListTypeAttribute<[NotAGraphType] TListInterface, [NotAGraphType] TListImplementation> : AotSchemaAttribute
    where TListImplementation : TListInterface
{
}
