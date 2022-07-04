namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an execution node with child nodes.
    /// </summary>
    public interface IParentExecutionNode
    {
        /// <summary>
        /// Returns a list of child execution nodes.
        /// </summary>
        IEnumerable<ExecutionNode> GetChildNodes();

        /// <summary>
        /// Applies the specified delegate to child execution nodes.
        /// </summary>
        /// <typeparam name="TState">Type of the provided state.</typeparam>
        /// <param name="action">Delegate to execute on every child node of this node.</param>
        /// <param name="state">An arbitrary state passed by the caller.</param>
        /// <param name="reverse">Specifies the direct or reverse direction of child nodes traversal.</param>
        void ApplyToChildren<TState>(Action<ExecutionNode, TState> action, TState state, bool reverse = false);
    }
}
