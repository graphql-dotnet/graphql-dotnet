namespace GraphQL.Types
{
    /// <summary>
    /// Provides properties for returning the graph type for the argument or field. Also used for <see cref="ListGraphType"/> and <see cref="NonNullGraphType"/>.
    /// </summary>
    public interface IProvideResolvedType
    {
        /// <summary>
        /// Returns the graph type of this argument or field.
        /// </summary>
        IGraphType ResolvedType { get; }
    }
}
