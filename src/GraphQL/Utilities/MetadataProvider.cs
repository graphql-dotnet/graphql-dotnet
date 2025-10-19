using GraphQL.Types;

namespace GraphQL.Utilities;

/// <summary>
/// Default implementation of <see cref="IProvideMetadata"/>. This is the base class for numerous
/// descendants like <see cref="GraphType"/>, <see cref="FieldType"/>, <see cref="Schema"/> and others.
/// Provides access to metadata reading and writing extension methods through <see cref="IMetadataReader"/>
/// and <see cref="IMetadataWriter"/>.
/// </summary>
public class MetadataProvider : IMetadataReader, IMetadataWriter
{
    private Dictionary<string, object?>? _metadata;

    /// <inheritdoc />
    public Dictionary<string, object?> Metadata => _metadata ??= [];

    IMetadataReader IMetadataWriter.MetadataReader => this;

    /// <inheritdoc />
    public TType GetMetadata<TType>(string key, TType defaultValue = default!)
    {
        var local = _metadata;
        return local != null && local.TryGetValue(key, out object? item) ? (TType)item! : defaultValue;
    }

    /// <inheritdoc />
    public TType GetMetadata<TType>(string key, Func<TType> defaultValueFactory)
    {
        var local = _metadata;
        return local != null && local.TryGetValue(key, out object? item) ? (TType)item! : defaultValueFactory();
    }

    /// <inheritdoc />
    public bool HasMetadata(string key) => _metadata?.ContainsKey(key) ?? false;

    /// <summary>
    /// Copies metadata to the specified target.
    /// </summary>
    /// <param name="target">Target for copying metadata.</param>
    public void CopyMetadataTo(IMetadataWriter target)
    {
        var local = _metadata;
        if (local?.Count > 0)
        {
            var to = target.Metadata;
            foreach (var kv in local)
                to[kv.Key] = kv.Value;
        }
    }
}
