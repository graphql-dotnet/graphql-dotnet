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
        /// <br/><br/>
        /// Also configures <see cref="ComplexityConfiguration"/> to be pulled from the dependency
        /// injection provider, overwriting values within <see cref="ExecutionOptions.ComplexityConfiguration"/>
        /// with values configured within the registered instance if set there.
        /// </summary>
        protected virtual void Initialize()
        {
            // configure an error to be displayed when no IDocumentWriter is registered
            this.TryRegister<IDocumentWriter>(_ =>
            {
                throw new InvalidOperationException(
                    "IDocumentWriter not set in DI container. " +
                    "Add a IDocumentWriter implementation, for example " +
                    "GraphQL.SystemTextJson.DocumentWriter or GraphQL.NewtonsoftJson.DocumentWriter." +
                    "For more information, see: https://github.com/graphql-dotnet/graphql-dotnet/blob/master/README.md and https://github.com/graphql-dotnet/server/blob/develop/README.md.");
            }, ServiceLifetime.Transient);

            // configure service implementations to use the configured default services when not overridden by a user
            this.TryRegister<IDocumentExecuter, DocumentExecuter>(ServiceLifetime.Singleton);
            this.TryRegister<IDocumentBuilder, GraphQLDocumentBuilder>(ServiceLifetime.Singleton);
            this.TryRegister<IDocumentValidator, DocumentValidator>(ServiceLifetime.Singleton);
            this.TryRegister<IComplexityAnalyzer, ComplexityAnalyzer>(ServiceLifetime.Singleton);
            this.TryRegister<IDocumentCache>(DefaultDocumentCache.Instance);
            this.TryRegister<IErrorInfoProvider, ErrorInfoProvider>(ServiceLifetime.Singleton);

            // configure an error message to be displayed if RequestServices is null,
            // and configure the ComplexityAnalyzer to be pulled from DI and configured (but left unchanged if not configured in DI)
            var defaultMaxRecursionCount = new ComplexityConfiguration().MaxRecursionCount;
            this.ConfigureExecution(options =>
            {
                if (options.RequestServices == null)
                    throw new InvalidOperationException("Cannot execute request if RequestServices is null.");

                if (options.RequestServices.GetService(typeof(ComplexityConfiguration)) is ComplexityConfiguration complexityConfiguration)
                {
                    if (options.ComplexityConfiguration == null)
                    {
                        options.ComplexityConfiguration = complexityConfiguration;
                    }
                    else
                    {
                        if (complexityConfiguration.FieldImpact.HasValue)
                            options.ComplexityConfiguration.FieldImpact = complexityConfiguration.FieldImpact;

                        if (complexityConfiguration.MaxComplexity.HasValue)
                            options.ComplexityConfiguration.MaxComplexity = complexityConfiguration.MaxComplexity;

                        if (complexityConfiguration.MaxDepth.HasValue)
                            options.ComplexityConfiguration.MaxDepth = complexityConfiguration.MaxDepth;

                        if (complexityConfiguration.MaxRecursionCount != defaultMaxRecursionCount)
                            options.ComplexityConfiguration.MaxRecursionCount = complexityConfiguration.MaxRecursionCount;
                    }
                }
            });

            // configure mapping for IOptions<ComplexityConfiguation> and IOptions<ErrorInfoProviderOptions>
            Configure<ComplexityConfiguration>();
            Configure<ErrorInfoProviderOptions>();
        }

        /// <inheritdoc/>
        public abstract IGraphQLBuilder Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder Register(Type serviceType, object implementationInstance);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder TryRegister(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder TryRegister(Type serviceType, object implementationInstance);

        /// <inheritdoc/>
        public abstract IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider> action = null)
            where TOptions : class, new();
    }
}
