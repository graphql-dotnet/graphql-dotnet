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
        : base("PersistedQueryNotSupported")
    {
        // note: the message "PersistedQueryNotSupported" is defined by the spec
        // to be returned when APQ support is unavailable; in this case it is
        // unavailable because the version number is unsupported

        // https://github.com/apollographql/apollo-link-persisted-queries

        this.AddExtension("reason", $"Automatic persisted queries protocol version '{version}' is not supported.");
    }
}
