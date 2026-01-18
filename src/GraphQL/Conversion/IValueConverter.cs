using GraphQL.Conversion;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Interface for value conversion operations used by ToObject methods.
/// </summary>
public interface IValueConverter
{
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
    public IListConverter GetListConverter(Type listType);

    /// <summary>
    /// Creates a new instance of the indicated type, populating it with the dictionary.
    /// </summary>
    /// <param name="source">The source of values.</param>
    /// <param name="type">The type to create.</param>
    /// <param name="inputGraphType">
    /// GraphType for matching dictionary keys with <paramref name="type"/> property names.
    /// GraphType contains information about this matching in Metadata property.
    /// In case of configuring field as Field("FirstName", x => x.FName) source dictionary
    /// will have 'FirstName' key but its value should be set to 'FName' property of created object.
    /// </param>
    public object ToObject(IDictionary<string, object?> source, Type type, IInputObjectGraphType inputGraphType);
}
