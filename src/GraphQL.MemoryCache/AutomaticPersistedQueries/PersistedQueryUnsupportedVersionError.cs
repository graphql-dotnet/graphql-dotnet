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
#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable IDE0060 // Remove unused parameter.
    public PersistedQueryUnsupportedVersionError(string? version)
#pragma warning restore IDE0060 // Remove unused parameter.
#pragma warning restore RCS1163 // Unused parameter.
        : base("PersistedQueryNotSupported")
    {
        // note: the message "PersistedQueryNotSupported" is defined by the spec
        // to be returned when APQ support is unavailable; in this case it is
        // unavailable because the version number is unsupported

        // https://github.com/apollographql/apollo-link-persisted-queries
    }
}
