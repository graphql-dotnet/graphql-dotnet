using GraphQL.Subscription;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
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
            : this(new ParallelExecutionStrategy())
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
        /// of one or more streams of GraphQL responses, returned within <see cref="SubscriptionExecutionResult.Streams"/>.
        /// No serializable <see cref="ExecutionResult"/> is directly returned.
        /// </summary>
        public override async Task<ExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            var rootType = GetOperationRootType(context);
            var rootNode = BuildExecutionRootNode(context, rootType);

            var streams = await ExecuteSubscriptionNodesAsync(context, rootNode.SubFields!).ConfigureAwait(false);

            // if execution is successful, errors and extensions are not returned per the graphql-ws protocol
            // if execution is unsuccessful, the DocumentExecuter will add context errors to the result

            return new SubscriptionExecutionResult(context)
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
                var stream = await ResolveEventStreamAsync(context, node).ConfigureAwait(false);
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
        protected virtual async Task<IObservable<ExecutionResult>?> ResolveEventStreamAsync(ExecutionContext context, ExecutionNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var resolveContext = new ReadonlyResolveFieldContext(node, context);

            IObservable<object?> subscription;

            try
            {
                if (node.FieldDefinition?.Subscriber == null)
                {
                    // this is normally caught by schema validation
                    throw new InvalidOperationException($"Subscriber not set for field '{node.Field.Name}'.");
                }

                subscription = await node.FieldDefinition.Subscriber.SubscribeAsync(resolveContext).ConfigureAwait(false);

                if (subscription == null)
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
                    $"Could not subscribe to field '{node.Field.Name}'.").ConfigureAwait(false));
                return null;
            }

            // cannot throw an exception here
            return subscription
                .SelectCatchAsync(
                    async (value, token) =>
                    {
                        // duplicate context to prevent multiple event streams from sharing the same context,
                        // clear errors/metrics/extensions, and free array pool leased arrays
                        using var childContext = new ExecutionContext(context)
                        {
                            Errors = new ExecutionErrors(),
                            OutputExtensions = new Dictionary<string, object?>(),
                            Metrics = Instrumentation.Metrics.None,
                            CancellationToken = token,
                        };

                        return await ProcessDataAsync(childContext, node, value).ConfigureAwait(false);
                    },
                    async (exception, token) =>
                    {
                        using var childContext = new ExecutionContext(context)
                        {
                            Errors = new ExecutionErrors(),
                            OutputExtensions = new Dictionary<string, object?>(),
                            Metrics = Instrumentation.Metrics.None,
                            CancellationToken = token,
                        };

                        return await ProcessErrorAsync(context, node, exception).ConfigureAwait(false);
                    });
        }

        /// <summary>
        /// Processes data from the event source via <see cref="IObserver{T}.OnNext(T)"/> and
        /// returns an <see cref="ExecutionResult"/>.
        /// <br/><br/>
        /// Override this method to mutate <see cref="ExecutionContext"/> as necessary, such
        /// as changing the <see cref="ExecutionContext.RequestServices"/> property to a scoped instance.
        /// </summary>
        protected virtual async Task<ExecutionResult> ProcessDataAsync(ExecutionContext context, ExecutionNode node, object? value)
        {
            var result = new ExecutionResult(context);

            try
            {
                if (context.Listeners != null)
                {
                    foreach (var listener in context.Listeners)
                    {
                        await listener.BeforeExecutionAsync(context)
                            .ConfigureAwait(false);
                    }
                }

                var executionNode = BuildSubscriptionExecutionNode(node.Parent!, node.GraphType!, node.Field, node.FieldDefinition, node.IndexInParentNode, value!);

                // Execute the whole execution tree and return the result
                await ExecuteNodeTreeAsync(context, executionNode).ConfigureAwait(false);

                if (context.Listeners != null)
                {
                    foreach (var listener in context.Listeners)
                    {
                        await listener.AfterExecutionAsync(context)
                            .ConfigureAwait(false);
                    }
                }

                // Set the execution node's value to null if necessary
                var dataIsNull = executionNode.PropagateNull();

                // Return the result
                result.Executed = true;
                if (!dataIsNull || executionNode.FieldDefinition.ResolvedType is not NonNullGraphType)
                {
                    result.Data = new RootExecutionNode(null!, null)
                    {
                        SubFields = new ExecutionNode[]
                        {
                            executionNode,
                        }
                    };
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
                    $"Could not process event data for field '{node.Field.Name}'.").ConfigureAwait(false));
            }

            result.AddErrors(context.Errors);

            return result;
        }

        /// <summary>
        /// Encapsulates an error within an <see cref="ExecutionResult"/> for errors generated
        /// by the event stream via <see cref="IObserver{T}.OnError(Exception)"/>.
        /// </summary>
        protected virtual Task<ExecutionError> ProcessErrorAsync(ExecutionContext context, ExecutionNode node, Exception exception)
            => HandleExceptionInternalAsync(context, node, exception, $"Event stream error for field '{node.Field.Name}'.");

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
}
