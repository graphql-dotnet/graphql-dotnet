#nullable enable

using GraphQL.DataLoader;
using GraphQL.DI;

namespace GraphQL;

/// <inheritdoc cref="GraphQLBuilderExtensions"/>
public static class DataLoaderGraphQLBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="DataLoaderDocumentListener"/> and <see cref="DataLoaderContextAccessor"/> within the
    /// dependency injection framework and configures the document listener to be added to the list of document
    /// listeners within <see cref="ExecutionOptions.Listeners"/> upon document execution.
    /// </summary>
    public static IGraphQLBuilder AddDataLoader(this IGraphQLBuilder builder)
    {
        builder.Services.Register<IDataLoaderContextAccessor, DataLoaderContextAccessor>(ServiceLifetime.Singleton);
        return builder.AddDocumentListener<DataLoaderDocumentListener>();
    }
}
