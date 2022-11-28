using GraphQL.Execution;
using GraphQL.Validation;

namespace GraphQL.DataLoader;

/// <summary>
/// Used to manage the <seealso cref="DataLoaderContext"/>
/// and automatically dispatch data loader operations at each execution step.
/// </summary>
public class DataLoaderDocumentListener : IDocumentExecutionListener
{
    private readonly IDataLoaderContextAccessor _accessor;

    /// <summary>
    /// Constructs a <see cref="DataLoaderDocumentListener"/> with the specified <see cref="IDataLoaderContextAccessor"/>
    /// </summary>
    public DataLoaderDocumentListener(IDataLoaderContextAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <inheritdoc/>
    public Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task BeforeExecutionAsync(IExecutionContext context)
    {
        _accessor.Context ??= new();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task AfterExecutionAsync(IExecutionContext context)
    {
        _accessor.Context = null;

        return Task.CompletedTask;
    }
}
