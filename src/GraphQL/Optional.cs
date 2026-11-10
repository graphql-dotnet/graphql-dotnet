namespace GraphQL;

/// <summary>
/// Represents a value that may be unspecified, allowing input object members to distinguish
/// an omitted field from an explicitly supplied value, including <see langword="null"/>.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public readonly struct Optional<T>
{
    private readonly T _value;

    /// <summary>
    /// Gets whether a value was specified.
    /// </summary>
    public bool IsSpecified { get; }

    /// <summary>
    /// Gets the specified value.
    /// </summary>
    /// <exception cref="InvalidOperationException">No value was specified.</exception>
    public T Value => IsSpecified
        ? _value
        : throw new InvalidOperationException("Optional object must have a value.");

    /// <summary>
    /// Initializes a new instance with a specified value, including <see langword="null"/>.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public Optional(T value)
    {
        _value = value;
        IsSpecified = true;
    }

    /// <summary>
    /// Returns the specified value, or the default value of <typeparamref name="T"/> if unspecified.
    /// </summary>
    [return: MaybeNull]
    public T GetValueOrDefault() => _value;

    /// <summary>
    /// Returns the specified value, or <paramref name="defaultValue"/> if unspecified.
    /// </summary>
    /// <param name="defaultValue">The value to return if no value was specified.</param>
    public T GetValueOrDefault(T defaultValue) => IsSpecified ? _value : defaultValue;

    /// <summary>
    /// Converts a value to an optional with <see cref="IsSpecified"/> set to <see langword="true"/>.
    /// </summary>
    /// <param name="value">The specified value.</param>
    public static implicit operator Optional<T>(T value) => new(value);
}
