using GraphQL.Execution;
#if NET5_0_OR_GREATER
using GraphQL.Telemetry;
#endif
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Validation;

namespace GraphQL.DI;

/// <summary>
/// Base implementation of <see cref="IGraphQLBuilder"/>.
/// </summary>
public abstract class GraphQLBuilderBase : IGraphQLBuilder
{
    /// <summary>
    /// Register the basic services required by GraphQL if they have not already been registered,
    /// excluding graph types required for connection builders (GraphQL Relay) and generic graph types
    /// such as <see cref="EnumerationGraphType{TEnum}"/> and <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>.
    /// <br/><br/>
    /// Does not include <see cref="IGraphQLSerializer"/>.
    /// </summary>
    protected virtual void RegisterBasicServices()
    {
        Services.TryRegister<IDocumentExecuter, DocumentExecuter>(ServiceLifetime.Singleton);
        Services.TryRegister<IDocumentExecuter<ISchema>, DocumentExecuter<ISchema>>(ServiceLifetime.Singleton);
        Services.TryRegister<IDocumentBuilder, GraphQLDocumentBuilder>(ServiceLifetime.Singleton);
        Services.TryRegister<IDocumentValidator, DocumentValidator>(ServiceLifetime.Singleton);
        Services.TryRegister<IErrorInfoProvider, ErrorInfoProvider>(ServiceLifetime.Singleton);
        Services.TryRegister<IExecutionStrategySelector, DefaultExecutionStrategySelector>(ServiceLifetime.Singleton);
        Services.Configure<ErrorInfoProviderOptions>();
    }

    /// <summary>
    /// Register the default services required by GraphQL if they have not already been registered.
    /// Includes graph types required for connection builders (GraphQL Relay) and generic graph types
    /// such as <see cref="EnumerationGraphType{TEnum}"/> and <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>.
    /// <br/><br/>
    /// Does not include <see cref="IGraphQLSerializer"/>.
    /// </summary>
    [RequiresUnreferencedCode("Public constructors for requested generic types may not be statically referenced.")]
    protected virtual void RegisterDefaultServices()
    {
        RegisterBasicServices();

        // configure an error to be displayed when no IGraphQLSerializer is registered
        Services.TryRegister<IGraphQLSerializer>(_ =>
        {
            throw new InvalidOperationException(
                "IGraphQLSerializer not set in DI container. " +
                "Add a IGraphQLSerializer implementation, for example " +
                "GraphQL.SystemTextJson.GraphQLSerializer or GraphQL.NewtonsoftJson.GraphQLSerializer. " +
                "For more information, see: https://github.com/graphql-dotnet/graphql-dotnet/blob/master/README.md.");
        }, ServiceLifetime.Transient);
        // configure an error to be displayed when no IGraphQLTextSerializer is registered
        Services.TryRegister<IGraphQLTextSerializer>(_ =>
        {
            throw new InvalidOperationException(
                "IGraphQLTextSerializer not set in DI container. " +
                "Add a IGraphQLTextSerializer implementation, for example " +
                "GraphQL.SystemTextJson.GraphQLSerializer or GraphQL.NewtonsoftJson.GraphQLSerializer. " +
                "For more information, see: https://github.com/graphql-dotnet/graphql-dotnet/blob/master/README.md.");
        }, ServiceLifetime.Transient);

        // configure service implementations to use the configured default services when not overridden by a user
        Services.TryRegister(typeof(IDocumentExecuter<>), typeof(DocumentExecuter<>), ServiceLifetime.Singleton);

        // configure relay graph types
        Services.TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient);
        Services.TryRegister(typeof(ConnectionType<>), typeof(ConnectionType<>), ServiceLifetime.Transient);
        Services.TryRegister(typeof(ConnectionType<,>), typeof(ConnectionType<,>), ServiceLifetime.Transient);
        Services.TryRegister<PageInfoType>(ServiceLifetime.Transient);

        // configure generic graph types
        Services.TryRegister(typeof(EnumerationGraphType<>), typeof(EnumerationGraphType<>), ServiceLifetime.Transient);
        Services.TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient);
        Services.TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient);
        Services.TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient);
        Services.TryRegister(typeof(AutoRegisteringInterfaceGraphType<>), typeof(AutoRegisteringInterfaceGraphType<>), ServiceLifetime.Transient);

        // configure execution to use the default registered schema if none specified
        this.ConfigureExecutionOptions(options =>
        {
            if (options.RequestServices != null && options.Schema == null)
            {
                options.Schema = options.RequestServices.GetService(typeof(ISchema)) as ISchema;
            }
        });

#if NET5_0_OR_GREATER
        if (OpenTelemetry.AutoInstrumentation.Initializer.Enabled)
        {
            // this will run prior to any other calls to UseTelemetry
            // it will also cause telemetry to be called first in the pipeline
            this.ConfigureExecution(GraphQLTelemetryProvider.AutoTelemetryProvider);
        }
#endif
    }

    /// <inheritdoc />
    public abstract IServiceRegister Services { get; }
}
