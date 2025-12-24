namespace GraphQL.Types;

/// <summary>
/// Provides properties for enumerating supported GraphQL interface types for an output graph type.
/// </summary>
public interface IImplementInterfaces : IGraphType
{
    /// <summary>
    /// Gets or sets a list of .NET types of supported GraphQL interface types.
    /// </summary>
    public Interfaces Interfaces { get; }

    /// <summary>
    /// Gets or sets a list of instances of supported GraphQL interface types.
    /// </summary>
    public ResolvedInterfaces ResolvedInterfaces { get; }

    /// <summary>
    /// Adds an instance of <see cref="IInterfaceGraphType"/> to the list of interface instances supported by this object graph type.
    /// </summary>
    public void AddResolvedInterface(IInterfaceGraphType graphType);
}
