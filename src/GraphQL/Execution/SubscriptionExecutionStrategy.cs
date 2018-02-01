using System;
using System.Collections.Concurrent;
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
        public override Task<ExecutionResult> ExecuteAsync(ExecutionContext context, IObjectGraphType rootType, object source, Dictionary<string, Field> fields)
        {
            var streams = ExecuteSubscriptionFields(context, rootType, context.RootValue, fields, new string[0]);

            ExecutionResult result = new SubscriptionExecutionResult
            {
                Streams = streams
            };

            return Task.FromResult(result);
        }

        private IDictionary<string, IObservable<ExecutionResult>> ExecuteSubscriptionFields(
           ExecutionContext context,
           IObjectGraphType rootType,
           object source,
           Dictionary<string, Field> fields,
           IEnumerable<string> path)
        {
            var result = new ConcurrentDictionary<string, IObservable<ExecutionResult>>();

            var parentPath = path.ToList();

            foreach (var field in fields)
            {
                var key = field.Key;

                var fieldResult = ResolveEventStream(context, rootType, source, field.Value, parentPath.Concat(new[] { key }));

                if (fieldResult.Skip)
                    continue;

                result[key] = fieldResult.Value;
            }

            return result;
        }

        private ResolveEventStreamResult ResolveEventStream(
            ExecutionContext context,
            IObjectGraphType parentType,
            object source,
            Field field,
            IEnumerable<string> path)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var fieldPath = path?.ToList() ?? new List<string>();

            var resolveResult = new ResolveEventStreamResult
            {
                Skip = false
            };

            if (!(GetFieldDefinition(context.Document, context.Schema, parentType, field) is EventStreamFieldType fieldDefinition))
            {
                resolveResult.Skip = true;
                return resolveResult;
            }

            var arguments = GetArgumentValues(
                context.Schema,
                fieldDefinition.Arguments,
                field.Arguments,
                context.Variables);

            try
            {
                var resolveContext = new ResolveEventStreamContext();
                resolveContext.FieldName = field.Name;
                resolveContext.FieldAst = field;
                resolveContext.FieldDefinition = fieldDefinition;
                resolveContext.ReturnType = fieldDefinition.ResolvedType;
                resolveContext.ParentType = parentType;
                resolveContext.Arguments = arguments;
                resolveContext.Source = source;
                resolveContext.Schema = context.Schema;
                resolveContext.Document = context.Document;
                resolveContext.Fragments = context.Fragments;
                resolveContext.RootValue = context.RootValue;
                resolveContext.UserContext = context.UserContext;
                resolveContext.Operation = context.Operation;
                resolveContext.Variables = context.Variables;
                resolveContext.CancellationToken = context.CancellationToken;
                resolveContext.Metrics = context.Metrics;
                resolveContext.Errors = context.Errors;
                resolveContext.Path = fieldPath;

                if (fieldDefinition.Subscriber == null)
                {
                    return GenerateError(resolveResult, field, context,
                       new InvalidOperationException($"Subscriber not set for field {field.Name}"),
                       fieldPath);
                }

                var result = fieldDefinition.Subscriber.Subscribe(resolveContext);

                resolveResult.Value = result
                    .SelectMany(async value =>
                    {
                        var fieldResolveResult = await ResolveFieldAsync(context, parentType, value, field, fieldPath)
                            .ConfigureAwait(false);
                        return new ExecutionResult
                        {
                            Data = new Dictionary<string, object>()
                            {
                                { field.Name,fieldResolveResult.Value }
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
                                        $"Could not subscribe to field '{field.Name}' in query '{context.Document.OriginalQuery}'",
                                        exception)
                                    {
                                        Path = path
                                    }
                                }
                            }));

                return resolveResult;
            }
            catch (Exception exc)
            {
                return GenerateError(resolveResult, field, context, exc, path);
            }
        }

        private ResolveEventStreamResult GenerateError(
            ResolveEventStreamResult resolveResult,
            Field field,
            ExecutionContext context,
            Exception exc,
            IEnumerable<string> path)
        {
            var error = new ExecutionError("Error trying to resolve {0}.".ToFormat(field.Name), exc);
            error.AddLocation(field, context.Document);
            error.Path = path;
            context.Errors.Add(error);
            resolveResult.Skip = false;
            return resolveResult;
        }
    }
}
