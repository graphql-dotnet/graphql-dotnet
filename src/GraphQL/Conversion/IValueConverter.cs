using GraphQL.Conversion;

namespace GraphQL;

/// <summary>
/// Interface for value conversion operations used by ToObject methods.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// <para>
    /// If a conversion delegate was registered, converts an object to the specified type and
    /// returns <see langword="true"/>; returns <see langword="false"/> if no conversion delegate is registered.
    /// </para>
    /// <para>Conversion delegates may throw exceptions if the conversion was unsuccessful</para>
    /// </summary>
    public bool TryConvertTo(object? value, Type targetType, out object? result, Type? sourceType = null);

    /// <summary>
    /// <para>Returns an object of the specified type and whose value is equivalent to the specified object.</para>
    /// <para>Throws a <see cref="InvalidOperationException"/> if there is no conversion registered; conversion functions may throw other exceptions</para>
    /// </summary>
    public T? ConvertTo<T>(object? value);

    /// <summary>
    /// <para>Returns an object of the specified type and whose value is equivalent to the specified object.</para>
    /// <para>Throws a <see cref="InvalidOperationException"/> if there is no conversion registered; conversion functions may throw other exceptions</para>
    /// </summary>
    public object? ConvertTo(object? value, Type targetType);

    /// <summary>
    /// Returns the conversion delegate registered to convert objects of type <paramref name="valueType"/>
    /// to type <paramref name="targetType"/>, if any.
    /// </summary>
    /// <param name="valueType">Type of original values.</param>
    /// <param name="targetType">Converted value type.</param>
    /// <returns>The conversion delegate if it is present, <see langword="null"/> otherwise.</returns>
    public Func<object, object>? GetConversion(Type valueType, Type targetType);

    /// <summary>
    /// Returns a converter which will convert items from a given <c>object[]</c> list
    /// into a list instance of the specified type. The list converter is cached for the specified type.
    /// </summary>
    public IListConverter GetListConverter(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType);
}
