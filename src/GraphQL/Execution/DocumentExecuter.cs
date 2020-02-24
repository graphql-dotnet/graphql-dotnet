using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using static GraphQL.Execution.ExecutionHelper;
using ExecutionContext = GraphQL.Execution.ExecutionContext;

namespace GraphQL
{

    public class DocumentExecuter : IDocumentExecuter
    {
        private readonly IDocumentBuilder _documentBuilder;
        private readonly IDocumentValidator _documentValidator;
        private readonly IComplexityAnalyzer _complexityAnalyzer;

        public DocumentExecuter()
            : this(new GraphQLDocumentBuilder(), new DocumentValidator(), new ComplexityAnalyzer())
        {
        }

        public DocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer)
        {
            _documentBuilder = documentBuilder;
            _documentValidator = documentValidator;
            _complexityAnalyzer = complexityAnalyzer;
        }

        public Task<ExecutionResult> ExecuteAsync(Action<ExecutionOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new ExecutionOptions();
            configure(options);
            return ExecuteAsync(options);
        }

        private void ValidateOptions(ExecutionOptions options)
        {
            if (options.Schema == null)
            {
                throw new ExecutionError("A schema is required.");
            }

            if (string.IsNullOrWhiteSpace(options.Query))
            {
                throw new ExecutionError("A query is required.");
            }
        }

        public async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.Schema == null)
                throw new InvalidOperationException("Cannot execute request if no schema is specified");

            var metrics = new Metrics(options.EnableMetrics).Start(options.OperationName);

            options.Schema.FieldNameConverter = options.FieldNameConverter;
            options.Schema.Filter = options.SchemaFilter;

            ExecutionResult result = null;
            ExecutionContext context = null;

            try
            {
                ValidateOptions(options);

                if (!options.Schema.Initialized)
                {
                    using (metrics.Subject("schema", "Initializing schema"))
                    {
                        options.FieldMiddleware.ApplyTo(options.Schema);
                        options.Schema.Initialize();
                    }
                }

                var document = options.Document;
                using (metrics.Subject("document", "Building document"))
                {
                    if (document == null)
                    {
                        document = _documentBuilder.Build(options.Query);
                    }
                }

                var operation = GetOperation(options.OperationName, document);
                metrics.SetOperationName(operation?.Name);

                if (operation == null)
                {
                    throw new ExecutionError("Unable to determine operation from query.");
                }

                IValidationResult validationResult;
                using (metrics.Subject("document", "Validating document"))
                {
                    validationResult = await _documentValidator.ValidateAsync(
                        options.Query,
                        options.Schema,
                        document,
                        options.ValidationRules,
                        options.UserContext,
                        options.Inputs);
                }

                if (options.ComplexityConfiguration != null && validationResult.IsValid)
                {
                    using (metrics.Subject("document", "Analyzing complexity"))
                        _complexityAnalyzer.Validate(document, options.ComplexityConfiguration);
                }

                foreach (var listener in options.Listeners)
                {
                    await listener.AfterValidationAsync(
                            options.UserContext,
                            validationResult,
                            options.CancellationToken)
                        .ConfigureAwait(false);
                }

                if (!validationResult.IsValid)
                {
                    return new ExecutionResult
                    {
                        Errors = validationResult.Errors,
                        ExposeExceptions = options.ExposeExceptions,
                        Perf = metrics.Finish()
                    };
                }

                context = BuildExecutionContext(
                    options.Schema,
                    options.Root,
                    document,
                    operation,
                    options.Inputs,
                    options.UserContext,
                    options.CancellationToken,
                    metrics,
                    options.Listeners,
                    options.ThrowOnUnhandledException,
                    options.UnhandledExceptionDelegate,
                    options.MaxParallelExecutionCount);

                if (context.Errors.Count > 0)
                {
                    return new ExecutionResult
                    {
                        Errors = context.Errors,
                        ExposeExceptions = options.ExposeExceptions,
                        Perf = metrics.Finish()
                    };
                }

                using (metrics.Subject("execution", "Executing operation"))
                {
                    if (context.Listeners != null)
                        foreach (var listener in context.Listeners)
                        {
                            await listener.BeforeExecutionAsync(context.UserContext, context.CancellationToken)
                                .ConfigureAwait(false);
                        }

                    IExecutionStrategy executionStrategy = SelectExecutionStrategy(context);

                    if (executionStrategy == null)
                        throw new InvalidOperationException("Invalid ExecutionStrategy!");

                    var task = executionStrategy.ExecuteAsync(context)
                        .ConfigureAwait(false);

                    if (context.Listeners != null)
                        foreach (var listener in context.Listeners)
                        {
                            await listener.BeforeExecutionAwaitedAsync(context.UserContext, context.CancellationToken)
                                .ConfigureAwait(false);
                        }

                    result = await task;

                    if (context.Listeners != null)
                        foreach (var listener in context.Listeners)
                        {
                            await listener.AfterExecutionAsync(context.UserContext, context.CancellationToken)
                                .ConfigureAwait(false);
                        }
                }

                if (context.Errors.Count > 0)
                {
                    result.Errors = context.Errors;
                }
            }
            catch (Exception ex)
            {
                if (options.ThrowOnUnhandledException)
                    throw;

                if (options.UnhandledExceptionDelegate != null)
                {
                    var exceptionContext = new UnhandledExceptionContext(context, null, ex);
                    options.UnhandledExceptionDelegate(exceptionContext);
                    ex = exceptionContext.Exception;
                }

                result = new ExecutionResult
                {
                    Errors = new ExecutionErrors
                    {
                        new ExecutionError(ex.Message, ex)
                    }
                };
            }
            finally
            {
                result ??= new ExecutionResult();
                result.ExposeExceptions = options.ExposeExceptions;
                result.Perf = metrics.Finish();
            }

            return result;
        }

        private ExecutionContext BuildExecutionContext(
            ISchema schema,
            object root,
            Document document,
            Operation operation,
            Inputs inputs,
            IDictionary<string, object> userContext,
            CancellationToken cancellationToken,
            Metrics metrics,
            List<IDocumentExecutionListener> listeners,
            bool throwOnUnhandledException,
            Action<UnhandledExceptionContext> unhandledExceptionDelegate,
            int? maxParallelExecutionCount)
        {
            var context = new ExecutionContext
            {
                Document = document,
                Schema = schema,
                RootValue = root,
                UserContext = userContext,

                Operation = operation,
                Variables = GetVariableValues(document, schema, operation?.Variables, inputs),
                Fragments = document.Fragments,
                CancellationToken = cancellationToken,

                Metrics = metrics,
                Listeners = listeners,
                ThrowOnUnhandledException = throwOnUnhandledException,
                UnhandledExceptionDelegate = unhandledExceptionDelegate,
                MaxParallelExecutionCount = maxParallelExecutionCount
            };

            return context;
        }

        protected virtual Operation GetOperation(string operationName, Document document)
        {
            return !string.IsNullOrWhiteSpace(operationName)
                ? document.Operations.WithName(operationName)
                : document.Operations.FirstOrDefault();
        }

        protected virtual IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
        {
            // TODO: Should we use cached instances of the default execution strategies?
            return context.Operation.OperationType switch
            {
                OperationType.Query => new ParallelExecutionStrategy(),
                OperationType.Mutation => new SerialExecutionStrategy(),
                OperationType.Subscription => new SubscriptionExecutionStrategy(),
                _ => throw new InvalidOperationException($"Unexpected OperationType {context.Operation.OperationType}")
            };
        }
    }
}
