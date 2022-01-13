namespace GraphQL.Execution
{
    /// <summary>
    /// Prepares <see cref="ExecutionError"/>s for serialization by the <see cref="IGraphQLSerializer"/>
    /// </summary>
    public interface IErrorInfoProvider
    {
        /// <summary>
        /// Parses an <see cref="ExecutionError"/> into a <see cref="ErrorInfo"/> struct
        /// </summary>
        ErrorInfo GetInfo(ExecutionError executionError);
    }
}
