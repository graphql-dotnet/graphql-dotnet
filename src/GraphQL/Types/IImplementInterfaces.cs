namespace GraphQL.Types;

/// <summary>
/// Provides properties for enumerating supported GraphQL interface types for an output graph type.
/// </summary>
public interface IImplementInterfaces : IGraphType
{
    /// <summary>
    /// Gets or sets a list of .NET types of supported GraphQL interface types.
    /// </summary>
    Interfaces Interfaces { get; }

    /// <summary>
    /// Gets or sets a list of instances of supported GraphQL interface types.
    /// </summary>
    ResolvedInterfaces ResolvedInterfaces { get; }

    /// <summary>
    /// Adds an instance of <see cref="IInterfaceGraphType"/> to the list of interface instances supported by this object graph type.
    /// </summary>
    void AddResolvedInterface(IInterfaceGraphType graphType);
}
