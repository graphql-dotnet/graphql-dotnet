#nullable enable

using GraphQL.DI;

namespace GraphQL.DataLoader
{
    /// <inheritdoc cref="GraphQL.GraphQLBuilderExtensions"/>
    public static class GraphQLBuilderExtensions
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
}
