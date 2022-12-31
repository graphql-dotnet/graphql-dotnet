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
public class TelemetryProvider : IConfigureExecution
{
    private readonly ActivitySource _activitySource;
    private readonly TelemetryOptions _telemetryOptions;
    private const string ACTIVITY_OPERATION_NAME = "graphql";

    /// <summary>
    /// Gets the source name used for telemetry.
    /// </summary>
    public static string SourceName => "GraphQL.NET";

    /// <summary>
    /// Initializes a new instance with the specified options.
    /// </summary>
    public TelemetryProvider(TelemetryOptions options)
    {
        _activitySource = new ActivitySource(SourceName, typeof(TelemetryProvider).Assembly.GetNuGetVersion());
        _telemetryOptions = options;
    }

    /// <inheritdoc/>
    public virtual float SortOrder => GraphQLBuilderExtensions.SORT_ORDER_CONFIGURATION;

    /// <inheritdoc/>
    public virtual async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
    {
        // start the Activity
        using var activity = await StartActivity(options).ConfigureAwait(false);

        // if no event listeners, do not record any telemetry
        if (activity == null)
            return await next(options).ConfigureAwait(false);

        // record the requested operation name and optionally the GraphQL document
        await SetInitialTags(activity, options).ConfigureAwait(false);

        // record the operation type and the operation name (which may be specified within the
        // document even if not specified in the request)
        options.Listeners.Add(new TelemetryListener(this, activity, options));

        // execute the request
        var result = await next(options).ConfigureAwait(false);

        // record the status
        await SetResultTags(activity, options, result).ConfigureAwait(false);

        // return the result
        return result;
    }

    /// <summary>
    /// Creates an <see cref="Activity"/> for the specified <see cref="ExecutionOptions"/> and starts it.
    /// </summary>
    protected virtual ValueTask<Activity?> StartActivity(ExecutionOptions options)
        => new(_activitySource.StartActivity(ACTIVITY_OPERATION_NAME));

    /// <summary>
    /// Sets the <see cref="Activity"/> tags based on the specified <see cref="ExecutionOptions"/>.
    /// The default implementation sets the <c>graphql.operation.name</c> and <c>graphql.document</c> tags.
    /// </summary>
    protected virtual Task SetInitialTags(Activity activity, ExecutionOptions options)
    {
        activity.SetTag("graphql.operation.name", options.OperationName);
        if (_telemetryOptions.RecordDocument && activity.IsAllDataRequested)
            activity.SetTag("graphql.document", options.Query);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the <see cref="Activity"/> tags based on the specified <see cref="GraphQLOperationDefinition"/> and other values.
    /// The default implementation sets the <c>graphql.operation.name</c> and <c>graphql.operation.type</c> tags, and sets the
    /// <see cref="Activity.DisplayName"/> property based on the operation name and type.
    /// </summary>
    protected virtual Task SetOperationTags(Activity activity, ExecutionOptions options, ISchema schema, GraphQLDocument document, GraphQLOperationDefinition operation)
    {
        var operationType = operation.Operation.ToString().ToLowerInvariant();
        activity.SetTag("graphql.operation.type", operationType);
        var operationName = operation.Name?.StringValue;
        activity.SetTag("graphql.operation.name", operationName);
        activity.DisplayName = operationName == null ? operationType : $"{operationType} {operationName}";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the <see cref="Activity"/> tags based on the specified <see cref="ExecutionResult"/>.
    /// The default implementation for .NET 6+ sets the status code tag based on the <see cref="ExecutionResult.Errors"/> property.
    /// </summary>
    protected virtual Task SetResultTags(Activity activity, ExecutionOptions executionOptions, ExecutionResult result)
    {
#if NET6_0_OR_GREATER
        var failed = result.Errors?.Count > 0;
        activity.SetStatus(failed ? ActivityStatusCode.Error : ActivityStatusCode.Ok);
#endif
        return Task.CompletedTask;
    }

    // note: this could be implemented as a validation rule with no public API changes
    private class TelemetryListener : DocumentExecutionListenerBase
    {
        private readonly TelemetryProvider _telemetryConfiguration;
        private readonly Activity _activity;
        private readonly ExecutionOptions _options;

        public TelemetryListener(TelemetryProvider telemetryConfiguration, Activity activity, ExecutionOptions options)
        {
            _telemetryConfiguration = telemetryConfiguration;
            _activity = activity;
            _options = options;
        }

        public override Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult)
            => _telemetryConfiguration.SetOperationTags(_activity, _options, context.Schema, context.Document, context.Operation);
    }
}

#endif

