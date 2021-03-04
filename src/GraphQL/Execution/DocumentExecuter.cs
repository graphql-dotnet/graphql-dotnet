using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Caching;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

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

            var metrics = (options.EnableMetrics ? new Metrics() : Metrics.None).Start(options.OperationName);

            ExecutionResult result = null;
            ExecutionContext context = null;
            bool executionOccurred = false;

            try
            {
                if (!options.Schema.Initialized)
                {
                    using (metrics.Subject("schema", "Initializing schema"))
                    {
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
                        try
                        {
                            document = _documentBuilder.Build(options.Query);
                        }
                        catch(SyntaxError ex)
                        { 
                            return new ExecutionResult
                            {
                                Errors = new ExecutionErrors
                                {
                                    ex
                                },
                                Perf = metrics.Finish()
                            };
                        }
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
                Variables variables;
                using (metrics.Subject("document", "Validating document"))
                {
                    (validationResult, variables) = await _documentValidator.ValidateAsync(
                        options.Schema,
                        document,
                        operation.Variables,
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

                context = BuildExecutionContext(options, document, operation, variables, metrics);

                foreach (var listener in options.Listeners)
                {
                    await listener.AfterValidationAsync(context, validationResult) // TODO: remove ExecutionContext or make different type ?
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

                executionOccurred = true;

                using (metrics.Subject("execution", "Executing operation"))
                {
                    if (context.Listeners != null)
                    {
                        foreach (var listener in context.Listeners)
                        {
                            await listener.BeforeExecutionAsync(context)
                                .ConfigureAwait(false);
                        }
                    }

                    var task = (context.ExecutionStrategy ?? throw new InvalidOperationException("Execution strategy not specified")).ExecuteAsync(context)
                        .ConfigureAwait(false);

                    if (context.Listeners != null)
                    {
                        foreach (var listener in context.Listeners)
                        {
#pragma warning disable CS0612 // Type or member is obsolete
                            await listener.BeforeExecutionAwaitedAsync(context)
#pragma warning restore CS0612 // Type or member is obsolete
                                .ConfigureAwait(false);
                        }
                    }

                    result = await task;

                    if (context.Listeners != null)
                    {
                        foreach (var listener in context.Listeners)
                        {
                            await listener.AfterExecutionAsync(context)
                                .ConfigureAwait(false);
                        }
                    }
                }

                result.AddErrors(context.Errors);
            }
            catch (OperationCanceledException) when (options.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ExecutionError ex)
            {
                (result ??= new ExecutionResult()).AddError(ex);
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

                (result ??= new ExecutionResult()).AddError(ex is ExecutionError executionError ? executionError : new UnhandledError(exceptionContext?.ErrorMessage ?? "Error executing document.", ex));
            }
            finally
            {
                result ??= new ExecutionResult();
                result.Perf = metrics.Finish();
                if (executionOccurred)
                    result.Executed = true;
                context?.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Builds a <see cref="ExecutionContext"/> instance from the provided values.
        /// </summary>
        protected virtual ExecutionContext BuildExecutionContext(ExecutionOptions options, Document document, Operation operation, Variables variables, Metrics metrics)
        {
            var context = new ExecutionContext
            {
                Document = document,
                Schema = options.Schema,
                RootValue = options.Root,
                UserContext = options.UserContext,

                Operation = operation,
                Variables = variables,
                Fragments = document.Fragments,
                Errors = new ExecutionErrors(),
                Extensions = new Dictionary<string, object>(),
                CancellationToken = options.CancellationToken,

                Metrics = metrics,
                Listeners = options.Listeners,
                ThrowOnUnhandledException = options.ThrowOnUnhandledException,
                UnhandledExceptionDelegate = options.UnhandledExceptionDelegate,
                MaxParallelExecutionCount = options.MaxParallelExecutionCount,
                RequestServices = options.RequestServices,
            };

            context.ExecutionStrategy = SelectExecutionStrategy(context);

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
            return string.IsNullOrWhiteSpace(operationName)
                ? document.Operations.FirstOrDefault()
                : document.Operations.WithName(operationName);
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
                OperationType.Query => ParallelExecutionStrategy.Instance,
                OperationType.Mutation => SerialExecutionStrategy.Instance,
                OperationType.Subscription => throw new NotSupportedException($"DocumentExecuter does not support executing subscriptions. You can use SubscriptionDocumentExecuter from GraphQL.SystemReactive package to handle subscriptions."),
                _ => throw new InvalidOperationException($"Unexpected OperationType {context.Operation.OperationType}")
            };
        }
    }
}
