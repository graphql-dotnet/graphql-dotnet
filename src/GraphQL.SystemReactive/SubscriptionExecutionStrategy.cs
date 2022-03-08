using System.Reactive.Linq;
using GraphQL.Subscription;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Executes a subscription.
    /// </summary>
    public class SubscriptionExecutionStrategy : ParallelExecutionStrategy
    {
        /// <summary>
        /// Gets a static instance of <see cref="SubscriptionExecutionStrategy"/> strategy.
        /// </summary>
        public static new SubscriptionExecutionStrategy Instance { get; } = new SubscriptionExecutionStrategy();

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

            ExecutionResult result = new SubscriptionExecutionResult
            {
                Executed = true,
                Streams = streams
            }.With(context);

            return result;
        }

        private async Task<IDictionary<string, IObservable<ExecutionResult>>> ExecuteSubscriptionNodesAsync(ExecutionContext context, ExecutionNode[] nodes)
        {
            var streams = new Dictionary<string, IObservable<ExecutionResult>>();

            foreach (var node in nodes)
            {
                var stream = await ResolveEventStreamAsync(context, node).ConfigureAwait(false);

                if (stream != null)
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

            object? source = (node.Parent != null)
                ? node.Parent.Result
                : context.RootValue;

            try
            {
                var resolveContext = new ResolveFieldContext(new ReadonlyResolveFieldContext(node, context));

                IObservable<object?> subscription;

                if (node.FieldDefinition?.Subscriber != null)
                {
                    subscription = await node.FieldDefinition.Subscriber.SubscribeAsync(resolveContext).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException($"Subscriber not set for field '{node.Field.Name}'.");
                }

                // The IServiceProvider instance will be disposed at this point, set it to null instead of exposing the disposed object
                resolveContext.RequestServices = null;

                return subscription
                    .Select(value => BuildSubscriptionExecutionNode(node.Parent!, node.GraphType!, node.Field, node.FieldDefinition, node.IndexInParentNode, value!))
                    .SelectMany(async executionNode =>
                    {
                        if (context.Listeners != null)
                        {
                            foreach (var listener in context.Listeners)
                            {
                                await listener.BeforeExecutionAsync(context)
                                    .ConfigureAwait(false);
                            }
                        }

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
                        // Note: assumes that the subscription field is nullable, regardless of how it was defined
                        // See https://github.com/graphql-dotnet/graphql-dotnet/pull/2240#discussion_r570631402
                        //TODO: check if a non-null subscription field is allowed per the spec
                        //TODO: check if errors should be returned along with the data
                        executionNode.PropagateNull();

                        // Return the result
                        return new ExecutionResult
                        {
                            Executed = true,
                            Data = new RootExecutionNode(null!, null)
                            {
                                SubFields = new ExecutionNode[]
                                {
                                    executionNode,
                                }
                            },
                        }.With(context);
                    })
                    .Catch<ExecutionResult, Exception>(exception =>
                        Observable.Return(
                            new ExecutionResult
                            {
                                Errors = new ExecutionErrors
                                {
                                    GenerateError(
                                        context,
                                        $"Could not subscribe to field '{node.Field.Name}' in query '{context.Document.Source}'.",
                                        node.Field,
                                        node.ResponsePath,
                                        exception)
                                }
                            }.With(context)));
            }
            catch (Exception ex)
            {
                var message = $"Error trying to resolve field '{node.Field.Name}'.";
                var error = GenerateError(context, message, node.Field, node.ResponsePath, ex);
                context.Errors.Add(error);
                return null;
            }
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

        private ExecutionError GenerateError(
            ExecutionContext context,
            string message,
            GraphQLField field,
            IEnumerable<object> path,
            Exception? ex = null) => new ExecutionError(message, ex) { Path = path }.AddLocation(field, context.Document);
    }
}
