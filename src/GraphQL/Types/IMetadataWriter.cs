namespace GraphQL.Types;

/// <summary>
/// Provides basic capabilities for getting and setting arbitrary meta information.
/// This interface is implemented by field builders and field types for configuring metadata.
/// </summary>
public interface IMetadataWriter : IProvideMetadata
{
    /// <summary>
    /// Provides access to read metadata.
    /// </summary>
    IMetadataReader MetadataReader { get; }
}
