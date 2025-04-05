using System.Collections;

namespace GraphQL.Types;

/// <summary>
/// A class that represents a set of possible types for <see cref="IAbstractGraphType"/> i.e. <see cref="InterfaceGraphType"/> or <see cref="UnionGraphType"/>.
/// </summary>
public class PossibleTypeReferences : IEnumerable<Type>
{
    internal List<Type> List { get; } = new List<Type>();

    /// <summary>
    /// Gets the count of possible types.
    /// </summary>
    public int Count => List.Count;

    internal void Add(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (!typeof(IObjectGraphType).IsAssignableFrom(type))
            throw new ArgumentException("The specified type must implement IObjectGraphType.", nameof(type));

        if (!List.Contains(type))
            List.Add(type);
    }

    /// <summary>
    /// Determines if the specified graph type is in the list.
    /// </summary>
    public bool Contains(Type type) => List.Contains(type ?? throw new ArgumentNullException(nameof(type)));

    /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
    public IEnumerator<Type> GetEnumerator() => List.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
