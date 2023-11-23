using System.Diagnostics;

namespace GraphQL.DataLoader;

/// <inheritdoc cref="IDataLoaderContextAccessor"/>
public class DataLoaderContextAccessor : IDataLoaderContextAccessor
{
    private static readonly AsyncLocal<DataLoaderContext?> _current = new();

    /// <inheritdoc/>
    [AllowNull]
    public DataLoaderContext Context
    {
        get
        {
            Debug.Assert(_current.Value != null, "DataLoaderContext is null. Ensure that DataLoaderDocumentListener is registered in the IoC container");
            return _current.Value!;
        }
        set => _current.Value = value;
    }
}
