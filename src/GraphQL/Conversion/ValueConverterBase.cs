using System.Collections;
using GraphQL.Conversion;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Base class for value conversion operations used by ToObject methods.
/// </summary>
public abstract class ValueConverterBase : IValueConverter
{
    /// <inheritdoc/>
    public abstract Func<object, object>? GetConversion(Type valueType, Type targetType);

    /// <inheritdoc/>
    public abstract IListConverter GetListConverter(Type listType);

    /// <summary>
    /// Gets the list converter factory for the specified list type, if any.
    /// Array types are supported.
    /// </summary>
    public abstract IListConverterFactory GetListConverterFactory(Type listType);

    /// <summary>
    /// Allows you to register your own conversion delegate from one type to another.
    /// <br/><br/>
    /// If the conversion from valueType to targetType is already registered, then it will be overwritten.
    /// </summary>
    /// <param name="valueType">Type of original value.</param>
    /// <param name="targetType">Converted value type.</param>
    /// <param name="conversion">Conversion delegate; <see langword="null"/> for unregister already registered conversion.</param>
    public abstract void Register(Type valueType, Type targetType, Func<object, object>? conversion);

    /// <summary>
    /// Allows you to register your own conversion delegate from one type to another.
    /// <br/><br/>
    /// If the conversion from TSource to TTarget is already registered, then it will be overwritten.
    /// </summary>
    /// <typeparam name="TSource">Type of original value.</typeparam>
    /// <typeparam name="TTarget">Converted value type.</typeparam>
    /// <param name="conversion">Conversion delegate; <see langword="null"/> for unregister already registered conversion.</param>
    public abstract void Register<TSource, TTarget>(Func<TSource, TTarget>? conversion);

    /// <summary>
    /// Registers a list converter for a specified list type. Especially useful for AOT scenarios where
    /// dynamic compilation is not available. Each element type must be individually registered.
    /// To register an open generic list type, use <see cref="RegisterListConverterFactory(Type, Type)"/>.
    /// <para>
    /// Sample usage:
    /// <code>
    /// RegisterListConverter&lt;List&lt;int&gt;, int&gt;(list => list.ToList());
    /// </code>
    /// </para>
    /// </summary>
    public abstract void RegisterListConverter<TListType, TElementType>(Func<IEnumerable<TElementType>, TListType>? conversion) where TListType : IEnumerable<TElementType>;

    /// <summary>
    /// Registers or removes a list converter factory for a specified list type.
    /// The list type may be a generic type definition, such as <see cref="List{T}"/>
    /// or a non-generic collection type such as <see cref="IList"/>. Closed
    /// generic types are also supported, such as <c>List&lt;int&gt;</c>.
    /// Array types cannot be registered. If the converter is <see langword="null"/>,
    /// the factory is removed.
    /// </summary>
    public abstract void RegisterListConverterFactory(Type listType, IListConverterFactory? converter);

    /// <summary>
    /// Registers a generic list converter factory for a specified list type.
    /// For example, it can be used to register a custom list converter for <see cref="IList{T}"/>.
    /// If the implementation type is an open generic type, the generic type argument from the list
    /// type will be used to create the implementation type. The implementation type must have a
    /// public constructor and Add method that accepts a single argument of the generic type argument,
    /// or a public constructor that accepts a single argument of type <see cref="IEnumerable{T}"/>.
    /// </summary>
    [RequiresUnreferencedCode(
        "For generic list types, the constructed implementation type (e.g. List<T>) must be rooted for trimming. " +
        "If the closed generic type is only referenced via reflection, the trimmer may remove its required constructors " +
        "or other members, which can cause runtime failures.")]
    public abstract void RegisterListConverterFactory(Type listType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type implementationType);

    /// <inheritdoc/>
    public abstract object ToObject(IDictionary<string, object?> source, Type type, IInputObjectGraphType inputGraphType);
}
