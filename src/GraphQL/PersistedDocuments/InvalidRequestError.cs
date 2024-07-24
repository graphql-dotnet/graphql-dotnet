using GraphQL.Execution;

namespace GraphQL.PersistedDocuments;

/// <summary>
/// Represents an error that occurs when a request contains both query and documentId parameters.
/// </summary>
public class InvalidRequestError : RequestError
{
    /// <inheritdoc cref="InvalidRequestError"/>
    public InvalidRequestError() : base("The request must not have both query and documentId parameters.")
    {
    }
}
