using System;
using GraphQL.Caching;
using GraphQL.Execution;
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
        /// <br/><br/>
        /// Does not include <see cref="IDocumentWriter"/>, and the default <see cref="IDocumentExecuter"/>
        /// implementation does not support subscriptions.
        /// </summary>
        protected void Initialize()
        {
            // configure an error to be displayed when no IDocumentWriter is registered
            TryRegister<IDocumentWriter>(ServiceLifetime.Transient, _ =>
            {
                throw new InvalidOperationException(
                    "IDocumentWriter not set in DI container. " +
                    "Add a IDocumentWriter implementation, for example " +
                    "GraphQL.SystemTextJson.DocumentWriter or GraphQL.NewtonsoftJson.DocumentWriter." +
                    "For more information, see: https://github.com/graphql-dotnet/graphql-dotnet/blob/master/README.md and https://github.com/graphql-dotnet/server/blob/develop/README.md.");
            });

            // configure service implementations to use the configured default services when not overridden by a user
            this.TryRegister<IDocumentExecuter, DocumentExecuter>(ServiceLifetime.Singleton);
            this.TryRegister<IDocumentBuilder, GraphQLDocumentBuilder>(ServiceLifetime.Singleton);
            this.TryRegister<IDocumentValidator, DocumentValidator>(ServiceLifetime.Singleton);
            this.TryRegister<IComplexityAnalyzer, ComplexityAnalyzer>(ServiceLifetime.Singleton);
            TryRegister<IDocumentCache>(ServiceLifetime.Singleton, _ => DefaultDocumentCache.Instance);
            this.TryRegister<IErrorInfoProvider, ErrorInfoProvider>(ServiceLifetime.Singleton);

            // configure an error message to be displayed if RequestServices is null,
            // and configure the ComplexityAnalyzer to be pulled from DI and configured (but left unchanged if not configured in DI)
            Action<ExecutionOptions> configureComplexityConfiguration = options =>
            {
                if (options.RequestServices == null)
                    throw new InvalidOperationException("Cannot execute request if RequestServices is null.");

                if (options.RequestServices.GetService(typeof(ComplexityConfiguration)) is ComplexityConfiguration complexityConfiguration)
                    options.ComplexityConfiguration = complexityConfiguration;
            };
            Register(ServiceLifetime.Singleton, _ => configureComplexityConfiguration);

            // configure mapping for IOptions<ComplexityConfiguation> and IOptions<ErrorInfoProviderOptions>
            // note that this code will cause a null to be passed into applicable constructor arguments during DI injection if these objects are unconfigured
            Configure<ComplexityConfiguration>();
            Configure<ErrorInfoProviderOptions>();

        }

        /// <inheritdoc/>
        public abstract IGraphQLBuilder Register<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TService> implementationFactory)
            where TService : class;

        /// <inheritdoc/>
        public abstract IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder TryRegister<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TService> implementationFactory)
            where TService : class;

        /// <inheritdoc/>
        public abstract IGraphQLBuilder TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider> action = null)
            where TOptions : class, new();


        /// <inheritdoc/>
        public abstract IGraphQLBuilder ConfigureDefaults<TOptions>(Action<TOptions, IServiceProvider> action)
            where TOptions : class, new();
    }
}
