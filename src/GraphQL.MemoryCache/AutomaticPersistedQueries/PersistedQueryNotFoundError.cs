using GraphQL.Execution;

namespace GraphQL.Caching;

/// <summary>
/// An error in case a query hasn't been found by hash.
/// </summary>
public class PersistedQueryNotFoundError : RequestError
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public PersistedQueryNotFoundError(string hash)
        : base($"Persisted query with '{hash}' hash was not found.")
    {
    }
}
