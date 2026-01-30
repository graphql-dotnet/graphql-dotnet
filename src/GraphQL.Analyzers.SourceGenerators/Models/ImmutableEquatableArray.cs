using System.Collections;

namespace GraphQL.Analyzers.SourceGenerators.Models;

/// <summary>
/// Represents an immutable, array-backed value that supports structural equality.
/// </summary>
public sealed class ImmutableEquatableArray<T> :
    IEquatable<ImmutableEquatableArray<T>>,
    IReadOnlyList<T>
    where T : IEquatable<T>
{
    /// <summary>
    /// Gets an empty immutable equatable array.
    /// </summary>
    public static ImmutableEquatableArray<T> Empty { get; }
        = new ImmutableEquatableArray<T>(Array.Empty<T>());

    private readonly T[] _values;

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    public T this[int index] => _values[index];

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Count => _values.Length;

    /// <summary>
    /// Initializes a new instance from the provided values.
    /// </summary>
    public ImmutableEquatableArray(IEnumerable<T> values)
    {
        _values = values.ToArray();
    }

    /// <summary>
    /// Determines whether this instance is equal to another instance.
    /// </summary>
    public bool Equals(ImmutableEquatableArray<T>? other)
        => other != null && ((ReadOnlySpan<T>)_values).SequenceEqual(other._values);

    /// <summary>
    /// Determines whether this instance is equal to another object.
    /// </summary>
    public override bool Equals(object? obj)
        => obj is ImmutableEquatableArray<T> other && Equals(other);

    /// <summary>
    /// Returns a hash code based on the contained values.
    /// </summary>
    public override int GetHashCode()
    {
        int hash = 0;
        foreach (T value in _values)
        {
            hash = Combine(hash, value is null ? 0 : value.GetHashCode());
        }
        return hash;

        static int Combine(int h1, int h2)
        {
            unchecked
            {
                uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
                return ((int)rol5 + h1) ^ h2;
            }
        }
    }

    /// <summary>
    /// Returns a struct-based enumerator for the array.
    /// </summary>
    public Enumerator GetEnumerator() => new Enumerator(_values);

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => ((IEnumerable<T>)_values).GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
        => _values.GetEnumerator();

    /// <summary>
    /// Enumerates the elements of an <see cref="ImmutableEquatableArray{T}"/>.
    /// </summary>
    public struct Enumerator
    {
        private readonly T[] _values;
        private int _index;

        internal Enumerator(T[] values)
        {
            _values = values;
            _index = -1;
        }

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        public bool MoveNext()
        {
            int newIndex = _index + 1;

            if ((uint)newIndex < (uint)_values.Length)
            {
                _index = newIndex;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public readonly T Current => _values[_index];
    }
}

/// <summary>
/// Provides extension methods for creating immutable equatable arrays.
/// </summary>
public static class ImmutableEquatableArray
{
    /// <summary>
    /// Converts a sequence to an <see cref="ImmutableEquatableArray{T}"/>.
    /// </summary>
    public static ImmutableEquatableArray<T> ToImmutableEquatableArray<T>(
        this IEnumerable<T> values)
        where T : IEquatable<T>
        => new(values);
}
