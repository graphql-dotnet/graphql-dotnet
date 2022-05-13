using GraphQL.Execution;

namespace GraphQL.Caching;

/// <summary>
/// An error in case of unsupported version.
/// </summary>
public class PersistedQueryUnsupportedVersionError : RequestError
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public PersistedQueryUnsupportedVersionError(string? version)
        : base($"Automatic persisted queries protocol of version '{version}' is not supported.")
    {
    }
}
