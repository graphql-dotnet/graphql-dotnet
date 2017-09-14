using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using Field = GraphQL.Language.AST.Field;

namespace GraphQL.Subscription
{
    public class SubscriptionExecuter : DocumentExecuter, ISubscriptionExecuter
    {
        private readonly IDocumentBuilder _documentBuilder;
        private readonly IDocumentValidator _documentValidator;
        private readonly IComplexityAnalyzer _complexityAnalyzer;

        public SubscriptionExecuter()
            : this(new GraphQLDocumentBuilder(), new DocumentValidator(), new ComplexityAnalyzer())
        {
        }

        public SubscriptionExecuter(
            IDocumentBuilder documentBuilder, 
            IDocumentValidator documentValidator, 
            IComplexityAnalyzer complexityAnalyzer) : base(documentBuilder, documentValidator, complexityAnalyzer)
        {
            _documentBuilder = documentBuilder;
            _documentValidator = documentValidator;
            _complexityAnalyzer = complexityAnalyzer;
        }

        public async Task<SubscriptionExecutionResult> SubscribeAsync(ExecutionOptions config)
        {
            var metrics = new Metrics();
            metrics.Start(config.OperationName);

            config.Schema.FieldNameConverter = config.FieldNameConverter;

            var result = new SubscriptionExecutionResult
            {
                Query = config.Query,
                ExposeExceptions = config.ExposeExceptions
            };

            try
            {
                if (!config.Schema.Initialized)
                    using (metrics.Subject("schema", "Initializing schema"))
                    {
                        config.FieldMiddleware.ApplyTo(config.Schema);
                        config.Schema.Initialize();
                    }

                var document = config.Document;
                using (metrics.Subject("document", "Building document"))
                {
                    if (document == null)
                        document = _documentBuilder.Build(config.Query);
                }

                result.Document = document;

                var operation = GetOperation(config.OperationName, document);

                // is this needed? 
                if (operation.OperationType != OperationType.Subscription)
                    throw new InvalidOperationException(
                        $"Cannot subscribe to query '{config.Query}'. {nameof(SubscribeAsync)} only supports subscriptions");

                result.Operation = operation;
                metrics.SetOperationName(operation?.Name);

                if (config.ComplexityConfiguration != null)
                    using (metrics.Subject("document", "Analyzing complexity"))
                    {
                        _complexityAnalyzer.Validate(document, config.ComplexityConfiguration);
                    }

                IValidationResult validationResult;
                using (metrics.Subject("document", "Validating document"))
                {
                    validationResult = _documentValidator.Validate(
                        config.Query,
                        config.Schema,
                        document,
                        config.ValidationRules,
                        config.UserContext);
                }

                foreach (var listener in config.Listeners)
                    await listener.AfterValidationAsync(
                            config.UserContext,
                            validationResult,
                            config.CancellationToken)
                        .ConfigureAwait(false);

                if (validationResult.IsValid)
                {
                    var context = BuildExecutionContext(
                        config.Schema,
                        config.Root,
                        document,
                        operation,
                        config.Inputs,
                        config.UserContext,
                        config.CancellationToken,
                        metrics);

                    if (context.Errors.Any())
                    {
                        result.Errors = context.Errors;
                        return result;
                    }

                    using (metrics.Subject("execution", "Executing operation"))
                    {
                        foreach (var listener in config.Listeners)
                            await listener.BeforeExecutionAsync(config.UserContext, config.CancellationToken)
                                .ConfigureAwait(false);

                        var streams = ExecuteSubscription(context);
                        result.Streams = streams;

                        foreach (var listener in config.Listeners)
                            await listener.AfterExecutionAsync(config.UserContext, config.CancellationToken)
                                .ConfigureAwait(false);
                    }

                    if (context.Errors.Any())
                        result.Errors = context.Errors;
                }
                else
                {
                    result.Streams = null;
                    result.Errors = validationResult.Errors;
                }

                return result;
            }
            catch (Exception exc)
            {
                if (result.Errors == null)
                    result.Errors = new ExecutionErrors();

                result.Streams = null;
                result.Errors.Add(new ExecutionError(exc.Message, exc));
                return result;
            }
            finally
            {
                result.Perf = metrics.Finish().ToArray();
            }
        }

        public IDictionary<string, IObservable<ExecutionResult>> ExecuteSubscription(ExecutionContext context)
        {
            var rootType = GetOperationRootType(context.Document, context.Schema, context.Operation);
            var fields = CollectFields(
                context,
                rootType,
                context.Operation.SelectionSet,
                new Dictionary<string, Fields>(),
                new List<string>());

            return ExecuteSubscriptionFields(context, rootType, context.RootValue, fields);
        }

        public IDictionary<string, IObservable<ExecutionResult>> ExecuteSubscriptionFields(
           ExecutionContext context,
           IObjectGraphType rootType,
           object source,
           Dictionary<string, Fields> fields)
        {
            var result = new ConcurrentDictionary<string, IObservable<ExecutionResult>>();

            foreach (var field in fields)
            {
                var key = field.Key;

                var fieldResult = ResolveEventStream(context, rootType, source, field.Value);

                if (fieldResult.Skip)
                    continue;

                result[key] = fieldResult.Value;
            }

            return result;
        }

        public ResolveEventStreamResult ResolveEventStream(ExecutionContext context,
            IObjectGraphType parentType, object source, Fields fields)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var resolveResult = new ResolveEventStreamResult
            {
                Skip = false
            };

            var field = fields.First();

            if (!(GetFieldDefinition(context.Schema, parentType, field) is EventStreamFieldType fieldDefinition))
            {
                resolveResult.Skip = true;
                return resolveResult;
            }

            var arguments = GetArgumentValues(context.Schema, fieldDefinition.Arguments, field.Arguments,
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

                if (fieldDefinition.Subscriber == null)
                    return GenerateError(resolveResult, field, context,
                        new InvalidOperationException($"Subscriber not set for field {field.Name}"));

                var result = fieldDefinition.Subscriber.Subscribe(resolveContext);

                var valueTransformer = result
                    .Select(value =>
                    {
                        var r = ResolveFieldAsync(context, parentType, value, fields).GetAwaiter().GetResult();

                        return new ExecutionResult()
                        {
                            Data = r.Value
                        };
                    })
                    .Catch<ExecutionResult, Exception>(exception => Observable.Return(
                            new ExecutionResult
                            {
                                Errors = new ExecutionErrors
                                {
                                    new ExecutionError(
                                        $"Error in subscription '{resolveContext.Document.OriginalQuery}'",
                                        exception)
                                }
                            }));

                resolveResult.Value = valueTransformer;
                return resolveResult;
            }
            catch (Exception exc)
            {
                return GenerateError(resolveResult, field, context, exc);
            }
        }

        private ResolveEventStreamResult GenerateError(ResolveEventStreamResult resolveResult, Field field,
            ExecutionContext context, Exception exc)
        {
            var error = new ExecutionError("Error trying to resolve {0}.".ToFormat(field.Name), exc);
            error.AddLocation(field, context.Document);
            context.Errors.Add(error);
            resolveResult.Skip = false;
            return resolveResult;
        }
    }
}
