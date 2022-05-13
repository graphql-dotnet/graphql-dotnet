namespace GraphQL.Execution;
/// <summary>
/// Represents an error indicating that no GraphQL query was provided to the request.
/// </summary>
public class QueryMissingError : RequestError
{
    /// <inheritdoc cref="QueryMissingError"/>
    public QueryMissingError() : base("GraphQL query is missing.") { }
}

/// <summary>
/// Represents an error that occurred prior to the execution of the request.
/// </summary>
public class RequestError : ExecutionError
{
    /// <summary>
    /// Initializes an instance with the specified message.
    /// </summary>
    public RequestError(string message) : base(message)
    {
        Code = GetErrorCode();
    }

    /// <summary>
    /// Initializes an instance with the specified message and inner exception.
    /// </summary>
    public RequestError(string message, Exception? innerException) : base(message, innerException)
    {
        Code = GetErrorCode();
    }

    internal string GetErrorCode()
    {
        var code = ErrorInfoProvider.GetErrorCode(GetType());
        if (code != "REQUEST_ERROR" && code.EndsWith("_ERROR", StringComparison.Ordinal))
            code = code.Substring(0, code.Length - 6);
        return code;
    }
}
