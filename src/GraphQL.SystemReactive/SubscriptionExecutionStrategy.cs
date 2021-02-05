using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Subscription;
using GraphQL.Types;
using static GraphQL.Execution.ExecutionHelper;

namespace GraphQL.Execution
{
    public class SubscriptionExecutionStrategy : ParallelExecutionStrategy
    {
        /// <summary>
        /// Gets a static instance of <see cref="SubscriptionExecutionStrategy"/> strategy.
        /// </summary>
        public static new SubscriptionExecutionStrategy Instance { get; } = new SubscriptionExecutionStrategy();

        public override async Task<ExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            var rootType = GetOperationRootType(context.Document, context.Schema, context.Operation);
            var rootNode = BuildExecutionRootNode(context, rootType);

            var streams = await ExecuteSubscriptionNodesAsync(context, rootNode.SubFields).ConfigureAwait(false);

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
                if (!(node.FieldDefinition is EventStreamFieldType))
                    continue;

                streams[node.Name] = await ResolveEventStreamAsync(context, node).ConfigureAwait(false);
            }

            return streams;
        }

        protected virtual async Task<IObservable<ExecutionResult>> ResolveEventStreamAsync(ExecutionContext context, ExecutionNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var arguments = GetArgumentValues(
                node.FieldDefinition.Arguments,
                node.Field.Arguments,
                context.Variables);

            object source = (node.Parent != null)
                ? node.Parent.Result
                : context.RootValue;

            try
            {
                var resolveContext = new ResolveEventStreamContext
                {
                    FieldName = node.Field.Name,
                    FieldAst = node.Field,
                    FieldDefinition = node.FieldDefinition,
                    ReturnType = node.FieldDefinition.ResolvedType,
                    ParentType = node.GetParentType(context.Schema),
                    Arguments = arguments,
                    Source = source,
                    Schema = context.Schema,
                    Document = context.Document,
                    Fragments = context.Fragments,
                    RootValue = context.RootValue,
                    UserContext = context.UserContext,
                    Operation = context.Operation,
                    Variables = context.Variables,
                    CancellationToken = context.CancellationToken,
                    Metrics = context.Metrics,
                    Errors = context.Errors,
                    Path = node.Path,
                    RequestServices = context.RequestServices,
                };

                var eventStreamField = node.FieldDefinition as EventStreamFieldType;


                IObservable<object> subscription;

                if (eventStreamField?.Subscriber != null)
                {
                    subscription = eventStreamField.Subscriber.Subscribe(resolveContext);
                }
                else if (eventStreamField?.AsyncSubscriber != null)
                {
                    subscription = await eventStreamField.AsyncSubscriber.SubscribeAsync(resolveContext).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException($"Subscriber not set for field '{node.Field.Name}'.");
                }

                return subscription
                    .Select(value => BuildSubscriptionExecutionNode(node.Parent, node.GraphType, node.Field, node.FieldDefinition, node.IndexInParentNode, value))
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

                        return new ExecutionResult
                        {
                            Data = new Dictionary<string, object>
                            {
                                { executionNode.Name, executionNode.ToValue() }
                            }
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
                                        $"Could not subscribe to field '{node.Field.Name}' in query '{context.Document.OriginalQuery}'.",
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
        protected ExecutionNode BuildSubscriptionExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode, object source)
        {
            if (graphType is NonNullGraphType nonNullFieldType)
                graphType = nonNullFieldType.ResolvedType;

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
            Field field,
            IEnumerable<object> path,
            Exception ex = null) => new ExecutionError(message, ex) { Path = path }.AddLocation(field, context.Document);
    }
}
