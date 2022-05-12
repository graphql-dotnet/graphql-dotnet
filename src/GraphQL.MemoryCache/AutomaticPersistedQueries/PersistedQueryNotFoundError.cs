using GraphQL.Validation;

namespace GraphQL.Caching;

/// <summary>
/// An error in case a query hasn't been found by hash.
/// </summary>
[Serializable]
public class PersistedQueryNotFoundError : ValidationError
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public PersistedQueryNotFoundError(string hash)
        : base($"Persisted query with '{hash}' hash was not found.")
    {
    }
}
