using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Caching;
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
    /// <summary>
    /// <inheritdoc cref="IDocumentExecuter"/>
    /// <br/><br/>
    /// Default implementation for <see cref="IDocumentExecuter"/>.
    /// </summary>
    public class DocumentExecuter : IDocumentExecuter
    {
        private readonly IDocumentBuilder _documentBuilder;
        private readonly IDocumentValidator _documentValidator;
        private readonly IComplexityAnalyzer _complexityAnalyzer;
        private readonly IDocumentCache _documentCache;

        /// <summary>
        /// Initializes a new instance with default <see cref="IDocumentBuilder"/>,
        /// <see cref="IDocumentValidator"/> and <see cref="IComplexityAnalyzer"/> instances,
        /// and without document caching.
        /// </summary>
        public DocumentExecuter()
            : this(new GraphQLDocumentBuilder(), new DocumentValidator(), new ComplexityAnalyzer(), DefaultDocumentCache.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance with specified <see cref="IDocumentBuilder"/>,
        /// <see cref="IDocumentValidator"/> and <see cref="IComplexityAnalyzer"/> instances,
        /// and without document caching.
        /// </summary>
        public DocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer)
            : this(documentBuilder, documentValidator, complexityAnalyzer, DefaultDocumentCache.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance with specified <see cref="IDocumentBuilder"/>,
        /// <see cref="IDocumentValidator"/>, <see cref="IComplexityAnalyzer"/>,
        /// and <see cref="IDocumentCache"/> instances.
        /// </summary>
        public DocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer, IDocumentCache documentCache)
        {
            _documentBuilder = documentBuilder ?? throw new ArgumentNullException(nameof(documentBuilder));
            _documentValidator = documentValidator ?? throw new ArgumentNullException(nameof(documentValidator));
            _complexityAnalyzer = complexityAnalyzer ?? throw new ArgumentNullException(nameof(complexityAnalyzer));
            _documentCache = documentCache ?? throw new ArgumentNullException(nameof(documentCache));
        }

        /// <inheritdoc/>
        public virtual async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.Schema == null)
                throw new InvalidOperationException("Cannot execute request if no schema is specified");
            if (options.Query == null)
                throw new InvalidOperationException("Cannot execute request if no query is specified");
            if (options.FieldMiddleware == null)
                throw new InvalidOperationException("Cannot execute request if no middleware builder specified");

            var metrics = new Metrics(options.EnableMetrics).Start(options.OperationName);

            ExecutionResult result = null;
            ExecutionContext context = null;

            try
            {
                if (!options.Schema.Initialized)
                {
                    using (metrics.Subject("schema", "Initializing schema"))
                    {
                        options.FieldMiddleware.ApplyTo(options.Schema);
                        options.Schema.Initialize();
                    }
                }

                var document = options.Document;
                bool saveInCache = false;
                bool analyzeComplexity = true;
                var validationRules = options.ValidationRules;
                using (metrics.Subject("document", "Building document"))
                {
                    if (document == null && (document = _documentCache[options.Query]) != null)
                    {
                        // none of the default validation rules yet are dependent on the inputs, and the
                        // operation name is not passed to the document validator, so any successfully cached
                        // document should not need any validation rules run on it
                        validationRules = options.CachedDocumentValidationRules ?? Array.Empty<IValidationRule>();
                        analyzeComplexity = false;
                    }
                    if (document == null)
                    {
                        document = _documentBuilder.Build(options.Query);
                        saveInCache = true;
                    }
                }

                if (document.Operations.Count == 0)
                {
                    throw new NoOperationError();
                }

                var operation = GetOperation(options.OperationName, document);
                metrics.SetOperationName(operation?.Name);

                if (operation == null)
                {
                    throw new InvalidOperationException($"Query does not contain operation '{options.OperationName}'.");
                }

                IValidationResult validationResult;
                using (metrics.Subject("document", "Validating document"))
                {
                    validationResult = await _documentValidator.ValidateAsync(
                        options.Query,
                        options.Schema,
                        document,
                        validationRules,
                        options.UserContext,
                        options.Inputs);
                }

                if (options.ComplexityConfiguration != null && validationResult.IsValid && analyzeComplexity)
                {
                    using (metrics.Subject("document", "Analyzing complexity"))
                        _complexityAnalyzer.Validate(document, options.ComplexityConfiguration);
                }

                if (saveInCache && validationResult.IsValid)
                {
                    _documentCache[options.Query] = document;
                }

                try
                {
                    context = BuildExecutionContext(
                        options.Schema,
                        options.Root,
                        document,
                        operation,
                        options.Inputs ?? Inputs.Empty,
                        options.UserContext,
                        options.CancellationToken,
                        metrics,
                        options.Listeners,
                        options.ThrowOnUnhandledException,
                        options.UnhandledExceptionDelegate,
                        options.MaxParallelExecutionCount,
                        options.RequestServices);
                }
                catch (InvalidVariableError)
                {
                    // error parsing variables
                    // attempt to run AfterValidationAsync with null for the 'ExecutionContext.Variables' property

                    context = BuildExecutionContext(
                        options.Schema,
                        options.Root,
                        document,
                        operation,
                        null,
                        options.UserContext,
                        options.CancellationToken,
                        metrics,
                        options.Listeners,
                        options.ThrowOnUnhandledException,
                        options.UnhandledExceptionDelegate,
                        options.MaxParallelExecutionCount,
                        options.RequestServices);

                    try
                    {
                        foreach (var listener in options.Listeners)
                        {
                            await listener.AfterValidationAsync(context, validationResult)
                                .ConfigureAwait(false);
                        }

                        // if there was a validation error, return that, and ignore the variable parsing error
                        if (!validationResult.IsValid)
                        {
                            return new ExecutionResult
                            {
                                Errors = validationResult.Errors,
                                Perf = metrics.Finish()
                            };
                        }
                    }
                    catch
                    {
                        // if there was an error within AfterValidationAsync (such as a NullReferenceException
                        // due to ExecutionContext.Variables being null), skip this step and throw the variable parsing error
                    }

                    // if there was no validation errors returned, throw the variable parsing error
                    throw;
                }

                foreach (var listener in options.Listeners)
                {
                    await listener.AfterValidationAsync(context, validationResult)
                        .ConfigureAwait(false);
                }

                if (!validationResult.IsValid)
                {
                    return new ExecutionResult
                    {
                        Errors = validationResult.Errors,
                        Perf = metrics.Finish()
                    };
                }

                if (context.Errors.Count > 0)
                {
                    return new ExecutionResult
                    {
                        Errors = context.Errors,
                        Perf = metrics.Finish()
                    };
                }

                using (metrics.Subject("execution", "Executing operation"))
                {
                    if (context.Listeners != null)
                        foreach (var listener in context.Listeners)
                        {
                            await listener.BeforeExecutionAsync(context)
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
#pragma warning disable CS0612 // Type or member is obsolete
                            await listener.BeforeExecutionAwaitedAsync(context)
#pragma warning restore CS0612 // Type or member is obsolete
                                .ConfigureAwait(false);
                        }

                    result = await task;

                    if (context.Listeners != null)
                        foreach (var listener in context.Listeners)
                        {
                            await listener.AfterExecutionAsync(context)
                                .ConfigureAwait(false);
                        }
                }

                if (context.Errors.Count > 0)
                {
                    result.Errors = context.Errors;
                }
            }
            catch (OperationCanceledException) when (options.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ExecutionError ex)
            {
                result = new ExecutionResult
                {
                    Errors = new ExecutionErrors
                    {
                        ex
                    }
                };
            }
            catch (Exception ex)
            {
                if (options.ThrowOnUnhandledException)
                    throw;

                UnhandledExceptionContext exceptionContext = null;
                if (options.UnhandledExceptionDelegate != null)
                {
                    exceptionContext = new UnhandledExceptionContext(context, null, ex);
                    options.UnhandledExceptionDelegate(exceptionContext);
                    ex = exceptionContext.Exception;
                }

                result = new ExecutionResult
                {
                    Errors = new ExecutionErrors
                    {
                        ex is ExecutionError executionError ? executionError : new UnhandledError(exceptionContext?.ErrorMessage ?? "Error executing document.", ex)
                    }
                };
            }
            finally
            {
                result ??= new ExecutionResult();
                result.Perf = metrics.Finish();
                context?.Dispose();
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
            int? maxParallelExecutionCount,
            IServiceProvider requestServices)
        {
            var context = new ExecutionContext
            {
                Document = document,
                Schema = schema,
                RootValue = root,
                UserContext = userContext,

                Operation = operation,
                Variables = inputs == null ? null : GetVariableValues(document, schema, operation?.Variables, inputs),
                Fragments = document.Fragments,
                Errors = new ExecutionErrors(),
                Extensions = new Dictionary<string, object>(),
                CancellationToken = cancellationToken,

                Metrics = metrics,
                Listeners = listeners,
                ThrowOnUnhandledException = throwOnUnhandledException,
                UnhandledExceptionDelegate = unhandledExceptionDelegate,
                MaxParallelExecutionCount = maxParallelExecutionCount,
                RequestServices = requestServices
            };

            return context;
        }

        /// <summary>
        /// Returns the selected <see cref="Operation"/> given a specified <see cref="Document"/> and operation name.
        /// <br/><br/>
        /// Returns <c>null</c> if an operation cannot be found that matches the given criteria.
        /// Returns the first operation from the document if no operation name was specified.
        /// </summary>
        protected virtual Operation GetOperation(string operationName, Document document)
        {
            return !string.IsNullOrWhiteSpace(operationName)
                ? document.Operations.WithName(operationName)
                : document.Operations.FirstOrDefault();
        }

        /// <summary>
        /// Returns an instance of an <see cref="IExecutionStrategy"/> given specified execution parameters.
        /// <br/><br/>
        /// Typically the strategy is selected based on the type of operation.
        /// <br/><br/>
        /// By default, query operations will return a <see cref="ParallelExecutionStrategy"/> while mutation operations return a
        /// <see cref="SerialExecutionStrategy"/>. Subscription operations return a special strategy defined in some separate project,
        /// for example it can be SubscriptionExecutionStrategy from GraphQL.SystemReactive.
        /// </summary>
        protected virtual IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
        {
            // TODO: Should we use cached instances of the default execution strategies?
            return context.Operation.OperationType switch
            {
                OperationType.Query => new ParallelExecutionStrategy(),
                OperationType.Mutation => new SerialExecutionStrategy(),
                OperationType.Subscription => throw new NotSupportedException($"DocumentExecuter does not support executing subscriptions. You can use SubscriptionDocumentExecuter from GraphQL.SystemReactive package to handle subscriptions."),
                _ => throw new InvalidOperationException($"Unexpected OperationType {context.Operation.OperationType}")
            };
        }
    }
}
