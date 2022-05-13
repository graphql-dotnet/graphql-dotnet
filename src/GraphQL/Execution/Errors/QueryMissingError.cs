namespace GraphQL.Execution;

/// <summary>
/// Represents an error that occurred prior to the execution of the GraphQL request.
/// This refers to any errors that arise before passing the request inside the GraphQL engine, that is, even before its validation.
/// </summary>
public class QueryMissingError : RequestError
{
    /// <inheritdoc cref="QueryMissingError"/>
    public QueryMissingError() : base("GraphQL query is missing.") { }
}
