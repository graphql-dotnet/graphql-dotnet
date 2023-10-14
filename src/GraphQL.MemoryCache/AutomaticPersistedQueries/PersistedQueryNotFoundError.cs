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
        : base("PersistedQueryNotFound")
    {
        // note: the message "PersistedQueryNotFound" is defined by the spec to be
        // returned when the hash cannot be found in the local cache

        // https://github.com/apollographql/apollo-link-persisted-queries
        this.AddExtension("reason", $"Persisted query with hash '{hash}' was not found.");
    }
}
