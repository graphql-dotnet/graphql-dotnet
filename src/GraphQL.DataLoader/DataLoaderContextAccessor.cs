namespace GraphQL.DataLoader;

/// <inheritdoc cref="IDataLoaderContextAccessor"/>
public class DataLoaderContextAccessor : IDataLoaderContextAccessor
{
    private static readonly AsyncLocal<DataLoaderContext?> _current = new();

    /// <inheritdoc/>
    [AllowNull]
    public DataLoaderContext Context
    {
        get => _current.Value ?? ThrowMissingContext();
        set => _current.Value = value;
    }

    [DoesNotReturn]
    private DataLoaderContext ThrowMissingContext()
        => throw new InvalidOperationException("DataLoaderContext is null. Ensure that DataLoaderDocumentListener is registered in the IoC container");
}
