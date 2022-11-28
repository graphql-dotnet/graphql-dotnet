using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Execution;

/// <summary>
/// Executes a subscription.
/// </summary>
public class SubscriptionExecutionStrategy : ExecutionStrategy
{
    private readonly IExecutionStrategy _baseExecutionStrategy;

    /// <summary>
    /// Initializes a new instance with a parallel execution strategy for child nodes.
    /// </summary>
    public SubscriptionExecutionStrategy()
        : this(new ParallelExecutionStrategy()) // new instance of shared variables within ParallelExecutionStrategy; do not use ParallelExecutionStrategy.Instance
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified execution strategy for child nodes.
    /// </summary>
    public SubscriptionExecutionStrategy(IExecutionStrategy baseExecutionStrategy)
    {
        _baseExecutionStrategy = baseExecutionStrategy ?? throw new ArgumentNullException(nameof(baseExecutionStrategy));
    }

    /// <summary>
    /// Gets a static instance of <see cref="SubscriptionExecutionStrategy"/>.
    /// </summary>
    public static SubscriptionExecutionStrategy Instance { get; } = new();

    /// <summary>
    /// Executes a GraphQL subscription request and returns the result. The result consists
    /// of one or more streams of GraphQL responses, returned within <see cref="ExecutionResult.Streams"/>.
    /// No serializable <see cref="ExecutionResult"/> is directly returned unless an error has occurred.
    /// This relates more to the protocol in use (defined in the transport layer) than the response here.
    /// <br/><br/>
    /// Keep in mind that if a scoped context is passed into <see cref="ExecutionContext.RequestServices"/>,
    /// and if it is disposed after the initial execution, node executions of subsequent data events will contain
    /// the disposed <see cref="ExecutionContext.RequestServices"/> instance and hence be unusable.
    /// <br/><br/>
    /// If scoped services are needed, it is recommended to utilize the ScopedSubscriptionExecutionStrategy
    /// class from the GraphQL.MicrosoftDI package, which will create a service scope during processing of data events.
    /// </summary>
    public override async Task<ExecutionResult> ExecuteAsync(ExecutionContext context)
    {
        var rootType = GetOperationRootType(context);
        var rootNode = BuildExecutionRootNode(context, rootType);

        var streams = await ExecuteSubscriptionNodesAsync(context, rootNode.SubFields!).ConfigureAwait(false);

        // if execution is successful, errors and extensions are not returned per the graphql-ws protocol
        // if execution is unsuccessful, the DocumentExecuter will add context errors to the result

        return new ExecutionResult(context)
        {
            Executed = true,
            Streams = streams,
        };
    }

    private async Task<IDictionary<string, IObservable<ExecutionResult>>?> ExecuteSubscriptionNodesAsync(ExecutionContext context, ExecutionNode[] nodes)
    {
        var streams = new Dictionary<string, IObservable<ExecutionResult>>();

        foreach (var node in nodes)
        {
            var stream = await ResolveResponseStreamAsync(context, node).ConfigureAwait(false);
            if (stream == null)
                return null;
            streams[node.Name!] = stream;
        }

        return streams;
    }

    /// <summary>
    /// Asynchronously returns a stream of <see cref="ExecutionResult"/> responses for the
    /// specified <see cref="ExecutionNode"/>.
    /// </summary>
    protected virtual async Task<IObservable<ExecutionResult>?> ResolveResponseStreamAsync(ExecutionContext context, ExecutionNode node)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var resolveContext = new ReadonlyResolveFieldContext(node, context);

        IObservable<object?> sourceStream;

        try
        {
            var resolver = node.FieldDefinition?.StreamResolver;

            if (resolver == null)
            {
                // todo: this should be caught by schema validation
                throw new InvalidOperationException($"Stream resolver not set for field '{node.Field.Name}'.");
            }

            sourceStream = await resolver.ResolveAsync(resolveContext).ConfigureAwait(false);

            if (sourceStream == null)
            {
                throw new InvalidOperationException($"No event stream returned for field '{node.Field.Name}'.");
            }
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ExecutionError error)
        {
            error.Path = node.ResponsePath;
            error.AddLocation(node.Field, context.Document);
            context.Errors.Add(error);
            return null;
        }
        catch (Exception exception)
        {
            context.Errors.Add(await HandleExceptionInternalAsync(context, node, exception,
                $"Could not resolve source stream for field '{node.Field.Name}'.").ConfigureAwait(false));
            return null;
        }

        // preserve required information from the context
        var preservedContext = CloneExecutionContext(context, default);

        // cannot throw an exception here
        return sourceStream
            .SelectCatchAsync(
                async (value, token) =>
                {
                    // duplicate context to prevent multiple event streams from sharing the same context,
                    // and clear errors/metrics/extensions. Free array pool leased arrays after execution.
                    using var childContext = CloneExecutionContext(preservedContext, token);

                    return await ProcessDataAsync(childContext, node, value).ConfigureAwait(false);
                },
                async (exception, token) =>
                {
                    using var childContext = CloneExecutionContext(preservedContext, token);

                    return await ProcessErrorAsync(childContext, node, exception).ConfigureAwait(false);
                });
    }

    /// <summary>
    /// Clones an execution context without stateful information -- errors, metrics, and output extensions.
    /// Sets the cancellation token on the cloned context to the specified value.
    /// <br/><br/>
    /// Override to clear a stored service provider from being preserved within a cloned execution context.
    /// </summary>
    protected virtual ExecutionContext CloneExecutionContext(ExecutionContext context, CancellationToken token) => new(context)
    {
        Errors = new ExecutionErrors(),
        OutputExtensions = new Dictionary<string, object?>(),
        Metrics = Instrumentation.Metrics.None,
        CancellationToken = token,
    };

    /// <summary>
    /// Processes data from the source stream via <see cref="IObserver{T}.OnNext(T)"/> and
    /// returns an <see cref="ExecutionResult"/>.
    /// <br/><br/>
    /// Override this method to mutate <see cref="ExecutionContext"/> as necessary, such
    /// as changing the <see cref="ExecutionContext.RequestServices"/> property to a scoped instance.
    /// </summary>
    protected virtual async ValueTask<ExecutionResult> ProcessDataAsync(ExecutionContext context, ExecutionNode node, object? value)
    {
        var result = new ExecutionResult(context);

        try
        {
            // "clone" the node and set the source
            // overwrite the 'node' variable here so it is picked up by exception handling code below
            // and will contain the source from this data event
            node = BuildSubscriptionExecutionNode(node.Parent!, node.GraphType!, node.Field, node.FieldDefinition, node.IndexInParentNode, value!);

            if (context.Listeners?.Count > 0)
            {
                foreach (var listener in context.Listeners)
                {
                    await listener.BeforeExecutionAsync(context)
                        .ConfigureAwait(false);
                }
            }

            if (context.Errors.Count > 0)
            {
                result.AddErrors(context.Errors);
                return result;
            }

            // Execute the whole execution tree and return the result
            await ExecuteNodeTreeAsync(context, node).ConfigureAwait(false);

            try
            {
                if (context.Listeners?.Count > 0)
                {
                    foreach (var listener in context.Listeners)
                    {
                        await listener.AfterExecutionAsync(context)
                            .ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                // Set the execution node's value to null if necessary
                var dataIsNull = node.PropagateNull();

                // Return the result
                result.Executed = true;
                if (!dataIsNull || node.FieldDefinition.ResolvedType is not NonNullGraphType)
                {
                    result.Data = new RootExecutionNode(null!, null)
                    {
                        SubFields = new ExecutionNode[]
                        {
                            node,
                        }
                    };
                }
            }
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            // generate a static error message here;
            // do not "throw;" back to the caller (the event source)
            result.AddError(GenerateError(context, node, "The operation was canceled."));
        }
        catch (ExecutionError executionError)
        {
            executionError.Path = node.ResponsePath;
            executionError.AddLocation(node.Field, context.Document);
            result.AddError(executionError);
        }
        catch (Exception exception)
        {
            result.AddError(await HandleExceptionInternalAsync(context, node, exception,
                $"Could not process source stream event data for field '{node.Field.Name}'.").ConfigureAwait(false));
        }

        result.AddErrors(context.Errors);

        return result;
    }

    /// <summary>
    /// Encapsulates an error within an <see cref="ExecutionResult"/> for errors generated
    /// by the event stream via <see cref="IObserver{T}.OnError(Exception)"/>.
    /// </summary>
    protected virtual Task<ExecutionError> ProcessErrorAsync(ExecutionContext context, ExecutionNode node, Exception exception)
        => HandleExceptionInternalAsync(context, node, exception, $"Response stream error for field '{node.Field.Name}'.");

    /// <summary>
    /// Generates an <see cref="ExecutionError"/> for the specified <see cref="Exception"/>
    /// and sets the <see cref="ExecutionError.Path"/> and <see cref="ExecutionError.Locations"/> properties.
    /// </summary>
    private async Task<ExecutionError> HandleExceptionInternalAsync(ExecutionContext context, ExecutionNode node, Exception exception, string defaultMessage)
    {
        var executionError = await HandleExceptionAsync(context, node, exception, defaultMessage).ConfigureAwait(false);
        executionError.Path = node.ResponsePath;
        executionError.AddLocation(node.Field, context.Document);
        return executionError;
    }

    /// <summary>
    /// Generates an <see cref="ExecutionError"/> for the specified <see cref="Exception"/>.
    /// </summary>
    protected virtual async Task<ExecutionError> HandleExceptionAsync(ExecutionContext context, ExecutionNode node, Exception exception, string defaultMessage)
    {
        UnhandledExceptionContext? exceptionContext = null;
        if (context.UnhandledExceptionDelegate != null)
        {
            exceptionContext = new UnhandledExceptionContext(context, new ReadonlyResolveFieldContext(node, context), exception);
            try
            {
                await context.UnhandledExceptionDelegate(exceptionContext).ConfigureAwait(false);
                exception = exceptionContext.Exception;
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                // absorb OperationCanceledExceptions within the unhandled exception delegate
                // do not "throw;" back to the caller (the event source)
            }
        }

        if (exception is not ExecutionError executionError)
        {
            executionError = new UnhandledError(exceptionContext?.ErrorMessage ?? defaultMessage, exception);
        }

        return executionError;
    }

    /// <summary>
    /// Builds an execution node with the specified parameters.
    /// </summary>
    protected ExecutionNode BuildSubscriptionExecutionNode(ExecutionNode parent, IGraphType graphType, GraphQLField field, FieldType fieldDefinition, int? indexInParentNode, object source)
    {
        if (graphType is NonNullGraphType nonNullFieldType)
            graphType = nonNullFieldType.ResolvedType!;

        return graphType switch
        {
            ListGraphType _ => new SubscriptionArrayExecutionNode(parent, graphType, field, fieldDefinition, indexInParentNode, source),
            IObjectGraphType _ => new SubscriptionObjectExecutionNode(parent, graphType, field, fieldDefinition, indexInParentNode, source),
            IAbstractGraphType _ => new SubscriptionObjectExecutionNode(parent, graphType, field, fieldDefinition, indexInParentNode, source),
            ScalarGraphType scalarGraphType => new SubscriptionValueExecutionNode(parent, scalarGraphType, field, fieldDefinition, indexInParentNode, source),
            _ => throw new InvalidOperationException($"Unexpected type: {graphType}")
        };
    }

    private ExecutionError GenerateError(ExecutionContext context, ExecutionNode node, string message, Exception? ex = null)
        => new ExecutionError(message, ex) { Path = node.ResponsePath }.AddLocation(node.Field, context.Document);

    /// <inheritdoc/>
    public override Task ExecuteNodeTreeAsync(ExecutionContext context, ExecutionNode rootNode)
        => _baseExecutionStrategy.ExecuteNodeTreeAsync(context, rootNode);
}
