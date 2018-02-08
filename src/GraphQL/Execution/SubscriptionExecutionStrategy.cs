using System;
using System.Collections.Generic;
using System.Linq;
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
        public override Task<ExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            var rootType = GetOperationRootType(context.Document, context.Schema, context.Operation);
            var rootNode = BuildExecutionRootNode(context, rootType);

            var streams = ExecuteSubscriptionNodes(context, rootNode.SubFields);

            ExecutionResult result = new SubscriptionExecutionResult
            {
                Streams = streams
            };

            return Task.FromResult(result);
        }

        private IDictionary<string, IObservable<ExecutionResult>> ExecuteSubscriptionNodes(ExecutionContext context, IDictionary<string, ExecutionNode> nodes)
        {
            var streams = new Dictionary<string, IObservable<ExecutionResult>>();

            foreach (var kvp in nodes)
            {
                var name = kvp.Key;
                var node = kvp.Value;

                if (!(node.FieldDefinition is EventStreamFieldType fieldDefinition))
                    continue;

                streams[name] = ResolveEventStream(context, node);
            }

            return streams;
        }

        protected virtual IObservable<ExecutionResult> ResolveEventStream(ExecutionContext context, ExecutionNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var arguments = GetArgumentValues(
                context.Schema,
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
                    ParentType = node.GraphType as IObjectGraphType,
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
                    Path = node.Path
                };

                var eventStreamField = node.FieldDefinition as EventStreamFieldType;

                if (eventStreamField?.Subscriber == null)
                {
                    throw new InvalidOperationException($"Subscriber not set for field {node.Field.Name}");
                }

                var subscription = eventStreamField.Subscriber.Subscribe(resolveContext);

                return subscription
                    .Select(value =>
                    {
                        // Create new execution node
                        return new ObjectExecutionNode(null, node.GraphType, node.Field, node.FieldDefinition, node.Path)
                        {
                            Source = value
                        };
                    })
                    .SelectMany(async objectNode =>
                    {
                        // Execute the whole execution tree and return the result
                        await ExecuteNodeTreeAsync(context, objectNode)
                            .ConfigureAwait(false);

                        return new ExecutionResult
                        {
                            Data = new Dictionary<string, object>
                            {
                                { objectNode.Name, objectNode.ToValue() }
                            }
                        };
                    })
                    .Catch<ExecutionResult, Exception>(exception =>
                        Observable.Return(
                            new ExecutionResult
                            {
                                Errors = new ExecutionErrors
                                {
                                    new ExecutionError(
                                        $"Could not subscribe to field '{node.Field.Name}' in query '{context.Document.OriginalQuery}'",
                                        exception)
                                    {
                                        Path = node.Path
                                    }
                                }
                            }));
            }
            catch (Exception ex)
            {
                GenerateError(context, node.Field, ex, node.Path);
                return null;
            }
        }

        private void GenerateError(
            ExecutionContext context,
            Field field,
            Exception ex,
            IEnumerable<string> path)
        {
            var error = new ExecutionError("Error trying to resolve {0}.".ToFormat(field.Name), ex);
            error.AddLocation(field, context.Document);
            error.Path = path;

            context.Errors.Add(error);
        }
    }
}
