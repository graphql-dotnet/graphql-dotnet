using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Represents a property value with its source location.
/// Used for properties that may need location-specific diagnostics.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public sealed class GraphQLObjectProperty<T>
{
    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the source location of this property in the code.
    /// </summary>
    public Location Location { get; }

    internal GraphQLObjectProperty(T? value, Location location)
    {
        Value = value;
        Location = location;
    }

    internal GraphQLObjectProperty(object? value, Location location)
    {
        Value = value == null ? default : (T)value;
        Location = location;
    }

    /// <summary>
    /// Implicitly converts the property to its underlying value.
    /// </summary>
    public static implicit operator T?(GraphQLObjectProperty<T> property) => property.Value;

    public override string? ToString() => Value?.ToString();
}
