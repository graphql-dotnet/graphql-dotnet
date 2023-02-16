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
#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable IDE0060 // Remove unused parameter.
    public PersistedQueryNotFoundError(string hash)
#pragma warning restore IDE0060 // Remove unused parameter.
#pragma warning restore RCS1163 // Unused parameter.
        : base("PersistedQueryNotFound")
    {
        // note: the message "PersistedQueryNotFound" is defined by the spec to be
        // returned when the hash cannot be found in the local cache

        // https://github.com/apollographql/apollo-link-persisted-queries
    }
}
