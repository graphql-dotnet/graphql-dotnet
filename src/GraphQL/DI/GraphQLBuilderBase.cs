using System;
using GraphQL.Caching;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace GraphQL.DI
{
    /// <summary>
    /// Base implementation of <see cref="IGraphQLBuilder"/>.
    /// </summary>
    public abstract class GraphQLBuilderBase : IGraphQLBuilder
    {
        /// <summary>
        /// Register the default services required by GraphQL if they have not already been registered.
        /// Includes graph types required for connection builders (GraphQL Relay) and generic graph types
        /// such as <see cref="EnumerationGraphType{TEnum}"/> and <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>.
        /// <br/><br/>
        /// Does not include <see cref="IDocumentWriter"/>, and the default <see cref="IDocumentExecuter"/>
        /// implementation does not support subscriptions.
        /// </summary>
        protected virtual void Initialize()
        {
            // configure an error to be displayed when no IDocumentWriter is registered
            this.TryRegister<IDocumentWriter>(_ =>
            {
                throw new InvalidOperationException(
                    "IDocumentWriter not set in DI container. " +
                    "Add a IDocumentWriter implementation, for example " +
                    "GraphQL.SystemTextJson.DocumentWriter or GraphQL.NewtonsoftJson.DocumentWriter. " +
                    "For more information, see: https://github.com/graphql-dotnet/graphql-dotnet/blob/master/README.md and https://github.com/graphql-dotnet/server/blob/develop/README.md.");
            }, ServiceLifetime.Transient);

            // configure service implementations to use the configured default services when not overridden by a user
            this.TryRegister<IDocumentExecuter, DocumentExecuter>(ServiceLifetime.Singleton);
            this.TryRegister<IDocumentBuilder, GraphQLDocumentBuilder>(ServiceLifetime.Singleton);
            this.TryRegister<IDocumentValidator, DocumentValidator>(ServiceLifetime.Singleton);
            this.TryRegister<IComplexityAnalyzer, ComplexityAnalyzer>(ServiceLifetime.Singleton);
            this.TryRegister<IDocumentCache>(DefaultDocumentCache.Instance);
            this.TryRegister<IErrorInfoProvider, ErrorInfoProvider>(ServiceLifetime.Singleton);

            // configure relay graph types
            TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient);
            TryRegister(typeof(ConnectionType<>), typeof(ConnectionType<>), ServiceLifetime.Transient);
            TryRegister(typeof(ConnectionType<,>), typeof(ConnectionType<,>), ServiceLifetime.Transient);
            this.TryRegister<PageInfoType>(ServiceLifetime.Transient);

            // configure generic graph types
            TryRegister(typeof(EnumerationGraphType<>), typeof(EnumerationGraphType<>), ServiceLifetime.Transient);
            TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient);
            TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient);
            TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient);

            // configure execution to use the default registered schema if none specified
            this.ConfigureExecution(options =>
            {
                if (options.RequestServices != null && options.Schema == null)
                {
                    options.Schema = options.RequestServices.GetService(typeof(ISchema)) as ISchema;
                }
            });

            // configure mapping for IOptions<ErrorInfoProviderOptions>
            Configure<ErrorInfoProviderOptions>();
        }

        /// <inheritdoc/>
        public abstract IGraphQLBuilder Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, bool replace = false);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, bool replace = false);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder Register(Type serviceType, object implementationInstance, bool replace = false);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder TryRegister(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder TryRegister(Type serviceType, object implementationInstance);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider>? action = null)
            where TOptions : class, new();
    }
}
