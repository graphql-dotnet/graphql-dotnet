using GraphQL.Validation;

namespace GraphQL.Caching;

/// <summary>
/// An error in case provided hash doesn't correspond to calculated hash.
/// </summary>
[Serializable]
public class PersistedQueryBadHashError : ValidationError
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public PersistedQueryBadHashError(string hash)
        : base($"The '{hash}' hash doesn't correspond to a query.")
    {
    }
}
