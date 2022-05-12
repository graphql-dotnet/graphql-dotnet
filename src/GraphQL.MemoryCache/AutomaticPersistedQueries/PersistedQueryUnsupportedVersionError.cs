using GraphQL.Validation;

namespace GraphQL.Caching;

/// <summary>
/// An error in case of unsupported version.
/// </summary>
[Serializable]
public class PersistedQueryUnsupportedVersionError : ValidationError
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public PersistedQueryUnsupportedVersionError(string? version)
        : base($"Persisted queries with '{version}' version are not supported.")
    {
    }
}
