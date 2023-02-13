#if NET5_0_OR_GREATER

using System.Diagnostics;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Instrumentation;

/// <summary>
/// Provides telemetry through the <see cref="Activity">System.Diagnostics.Activity API</see> as an <see cref="IConfigureExecution"/> implementation.
/// Derive from this class to add additional telemetry.
/// <para>
/// To use a derived class, call <see cref="GraphQLBuilderExtensions.ConfigureExecution{TConfigureExecution}(IGraphQLBuilder)"/> with the type of your derived class.
/// </para>
/// </summary>
public class GraphQLTelemetryProvider : IConfigureExecution
{
    private readonly GraphQLTelemetryOptions _telemetryOptions;
    private const string ACTIVITY_OPERATION_NAME = "graphql";

    /// <summary>
    /// Returns an <see cref="System.Diagnostics.ActivitySource"/> instance to be used for GraphQL.NET telemetry.
    /// </summary>
    protected static ActivitySource ActivitySource { get; }

    /// <summary>
    /// Gets the source name used for telemetry.
    /// </summary>
    public static string SourceName => "GraphQL";

    static GraphQLTelemetryProvider()
    {
        ActivitySource = new ActivitySource(SourceName, typeof(GraphQLTelemetryProvider).Assembly.GetNuGetVersion());
    }

    /// <summary>
    /// Initializes a new instance with the specified options.
    /// </summary>
    public GraphQLTelemetryProvider(GraphQLTelemetryOptions options)
    {
        _telemetryOptions = options;
    }

    /// <inheritdoc/>
    public virtual float SortOrder => GraphQLBuilderExtensions.SORT_ORDER_CONFIGURATION;

    /// <inheritdoc/>
    public virtual async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
    {
        if (!_telemetryOptions.Filter(options))
            return await next(options).ConfigureAwait(false);

        // start the Activity, in fact Activity.Stop() will be called from within Activity.Dispose() at the end of using block 
        using var activity = await StartActivityAsync(options).ConfigureAwait(false);

        // do not record any telemetry if there are no listeners or it decided not to sample the current request
        if (activity == null)
            return await next(options).ConfigureAwait(false);

        // record the requested operation name and optionally the GraphQL document
        await SetInitialTagsAsync(activity, options).ConfigureAwait(false);

        // record the operation type and the operation name (which may be specified within the
        // document even if not specified in the request)
        options.Listeners.Add(new TelemetryListener(this, activity, options));

        // execute the request
        var result = await next(options).ConfigureAwait(false);

        // record the status
        await SetResultTagsAsync(activity, options, result).ConfigureAwait(false);

        // return the result
        return result;
    }

    /// <summary>
    /// Creates an <see cref="Activity"/> for the specified <see cref="ExecutionOptions"/> and starts it.
    /// </summary>
    protected virtual ValueTask<Activity?> StartActivityAsync(ExecutionOptions options)
        => new(ActivitySource.StartActivity(ACTIVITY_OPERATION_NAME));

    /// <summary>
    /// Sets the <see cref="Activity"/> tags based on the specified <see cref="ExecutionOptions"/>.
    /// The default implementation sets the <c>graphql.operation.name</c> and <c>graphql.document</c> tags.
    /// </summary>
    protected virtual Task SetInitialTagsAsync(Activity activity, ExecutionOptions options)
    {
        activity.SetTag("graphql.operation.name", options.OperationName);
        if (_telemetryOptions.RecordDocument && activity.IsAllDataRequested)
            activity.SetTag("graphql.document", options.Query);
        _telemetryOptions.EnrichWithExecutionOptions(activity, options);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the <see cref="Activity"/> tags based on the specified <see cref="GraphQLOperationDefinition"/> and other values.
    /// The default implementation sets the <c>graphql.operation.name</c> and <c>graphql.operation.type</c> tags, and sets the
    /// <see cref="Activity.DisplayName"/> property based on the operation name and type.
    /// </summary>
    protected virtual Task SetOperationTagsAsync(Activity activity, ExecutionOptions options, ISchema schema, GraphQLDocument document, GraphQLOperationDefinition operation)
    {
        var operationType = operation.Operation switch
        {
            OperationType.Query => "query",
            OperationType.Mutation => "mutation",
            OperationType.Subscription => "subscription",
            _ => "unknown", // cannot occur
        };
        // https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/instrumentation/graphql/
        activity.SetTag("graphql.operation.type", operationType);
        var operationName = operation.Name?.StringValue;
        activity.SetTag("graphql.operation.name", operationName);
        activity.DisplayName = operationName == null ? operationType : $"{operationType} {operationName}";
        _telemetryOptions.EnrichWithDocument(activity, options, schema, document, operation);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the <see cref="Activity"/> tags based on the specified <see cref="ExecutionResult"/>.
    /// The default implementation for .NET 6+ sets the status code tag based on the <see cref="ExecutionResult.Errors"/> property.
    /// For other platforms method does nothing.
    /// </summary>
    protected virtual Task SetResultTagsAsync(Activity activity, ExecutionOptions executionOptions, ExecutionResult result)
    {
        if (result.Errors?.Count > 0)
        {
#if NET6_0_OR_GREATER
            activity.SetStatus(ActivityStatusCode.Error);
#else
            activity.SetTag("otel.status_code", "ERROR");
#endif
        }
        _telemetryOptions.EnrichWithExecutionResult(activity, executionOptions, result);
        return Task.CompletedTask;
    }

    // note: this could be implemented as a validation rule with no public API changes
    private class TelemetryListener : DocumentExecutionListenerBase
    {
        private readonly GraphQLTelemetryProvider _provider;
        private readonly Activity _activity;
        private readonly ExecutionOptions _options;

        public TelemetryListener(GraphQLTelemetryProvider provider, Activity activity, ExecutionOptions options)
        {
            _provider = provider;
            _activity = activity;
            _options = options;
        }

        public override Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult)
            => _provider.SetOperationTagsAsync(_activity, _options, context.Schema, context.Document, context.Operation);
    }
}

#endif

