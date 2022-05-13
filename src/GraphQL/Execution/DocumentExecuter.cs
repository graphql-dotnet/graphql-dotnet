using GraphQL.Caching;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQLParser;
using GraphQLParser.AST;
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
        private readonly ExecutionDelegate _execution;
        private readonly IExecutionStrategySelector _executionStrategySelector;

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
            : this(documentBuilder, documentValidator, complexityAnalyzer, documentCache, new DefaultExecutionStrategySelector(), Array.Empty<IConfigureExecution>())
        {
        }

        /// <summary>
        /// Initializes a new instance with specified <see cref="IDocumentBuilder"/>,
        /// <see cref="IDocumentValidator"/>, <see cref="IComplexityAnalyzer"/>,
        /// <see cref="IDocumentCache"/> and a set of <see cref="IConfigureExecutionOptions"/> instances.
        /// </summary>
        [Obsolete("Use the constructor that accepts IConfigureExecution; this constructor will be removed in v6")]
        public DocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer, IDocumentCache documentCache, IEnumerable<IConfigureExecutionOptions> configurations)
            : this(documentBuilder, documentValidator, complexityAnalyzer, documentCache, new DefaultExecutionStrategySelector(), new IConfigureExecution[] { new ConfigureExecutionOptionsMapper(configurations) })
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified <see cref="IDocumentBuilder"/>,
        /// <see cref="IDocumentValidator"/>, <see cref="IComplexityAnalyzer"/>,
        /// <see cref="IDocumentCache"/> and a set of <see cref="IConfigureExecutionOptions"/> instances.
        /// </summary>
        [Obsolete("Use the constructor that accepts IConfigureExecution; this constructor will be removed in v6")]
        public DocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer, IDocumentCache documentCache, IEnumerable<IConfigureExecutionOptions> configurations, IExecutionStrategySelector executionStrategySelector)
            : this(documentBuilder, documentValidator, complexityAnalyzer, documentCache, executionStrategySelector, new IConfigureExecution[] { new ConfigureExecutionOptionsMapper(configurations) })
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified <see cref="IDocumentBuilder"/>,
        /// <see cref="IDocumentValidator"/>, <see cref="IComplexityAnalyzer"/>,
        /// <see cref="IDocumentCache"/> and a set of <see cref="IConfigureExecutionOptions"/> instances.
        /// </summary>
        public DocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer, IDocumentCache documentCache, IExecutionStrategySelector executionStrategySelector, IEnumerable<IConfigureExecution> configurations, IEnumerable<IConfigureExecutionOptions> optionsConfigurations)
#pragma warning disable CS0618 // Type or member is obsolete
            : this(documentBuilder, documentValidator, complexityAnalyzer, documentCache, executionStrategySelector, configurations.Append(new ConfigureExecutionOptionsMapper(optionsConfigurations)))
#pragma warning restore CS0618 // Type or member is obsolete
        {
            // TODO: remove in v6
        }

        /// <summary>
        /// Initializes a new instance with the specified <see cref="IDocumentBuilder"/>,
        /// <see cref="IDocumentValidator"/>, <see cref="IComplexityAnalyzer"/>,
        /// <see cref="IDocumentCache"/> and a set of <see cref="IConfigureExecutionOptions"/> instances.
        /// </summary>
        private DocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer, IDocumentCache documentCache, IExecutionStrategySelector executionStrategySelector, IEnumerable<IConfigureExecution> configurations)
        {
            // TODO: in v6 make this public
            _documentBuilder = documentBuilder ?? throw new ArgumentNullException(nameof(documentBuilder));
            _documentValidator = documentValidator ?? throw new ArgumentNullException(nameof(documentValidator));
            _complexityAnalyzer = complexityAnalyzer ?? throw new ArgumentNullException(nameof(complexityAnalyzer));
            _documentCache = documentCache ?? throw new ArgumentNullException(nameof(documentCache));
            _executionStrategySelector = executionStrategySelector ?? throw new ArgumentNullException(nameof(executionStrategySelector));
            _execution = BuildExecutionDelegate(configurations);
        }

        private ExecutionDelegate BuildExecutionDelegate(IEnumerable<IConfigureExecution> configurations)
        {
            ExecutionDelegate execution = CoreExecuteAsync;
            var configurationArray = configurations.ToArray();
            for (var i = configurationArray.Length - 1; i >= 0; i--)
            {
                var action = configurationArray[i];
                var nextExecution = execution;
                execution = async options => await action.ExecuteAsync(options, nextExecution).ConfigureAwait(false);
            }
            return execution;
        }

        /// <inheritdoc/>
        public virtual Task<ExecutionResult> ExecuteAsync(ExecutionOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return _execution(options);
        }

        private async Task<ExecutionResult> CoreExecuteAsync(ExecutionOptions options)
        {
            if (options.Schema == null)
                throw new InvalidOperationException("Cannot execute request if no schema is specified");
            if (options.Query == null && options.Document == null)
                return new ExecutionResult { Errors = new ExecutionErrors { new QueryMissingError() } };

            var metrics = (options.EnableMetrics ? new Metrics() : Metrics.None).Start(options.OperationName);

            ExecutionResult? result = null;
            ExecutionContext? context = null;
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
                    if (document == null && (document = await _documentCache.GetAsync(options.Query!).ConfigureAwait(false)) != null)
                    {
                        // none of the default validation rules yet are dependent on the inputs, and the
                        // operation name is not passed to the document validator, so any successfully cached
                        // document should not need any validation rules run on it
                        validationRules = options.CachedDocumentValidationRules ?? Array.Empty<IValidationRule>();
                        analyzeComplexity = false;
                    }
                    if (document == null)
                    {
                        document = _documentBuilder.Build(options.Query!);
                        saveInCache = true;
                    }
                }

                if (document.OperationsCount() == 0)
                {
                    throw new NoOperationError();
                }

                var operation = GetOperation(options.OperationName, document);
                if (operation == null)
                {
                    throw new InvalidOperationError($"Query does not contain operation '{options.OperationName}'.");
                }
                metrics.SetOperationName(operation.Name);

                IValidationResult validationResult;
                Variables variables;
                using (metrics.Subject("document", "Validating document"))
                {
                    (validationResult, variables) = await _documentValidator.ValidateAsync(
                        new ValidationOptions
                        {
                            Document = document,
                            Rules = validationRules,
                            Operation = operation,
                            UserContext = options.UserContext,
                            RequestServices = options.RequestServices,
                            CancellationToken = options.CancellationToken,
                            Schema = options.Schema,
                            Variables = options.Variables ?? Inputs.Empty,
                            Extensions = options.Extensions ?? Inputs.Empty,
                        }).ConfigureAwait(false);
                }

                if (options.ComplexityConfiguration != null && validationResult.IsValid && analyzeComplexity)
                {
                    using (metrics.Subject("document", "Analyzing complexity"))
                        _complexityAnalyzer.Validate(document, options.ComplexityConfiguration);
                }

                if (saveInCache && validationResult.IsValid)
                {
                    await _documentCache.SetAsync(options.Query!, document).ConfigureAwait(false);
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
                (result ??= new()).AddError(ex);
            }
            catch (Exception ex)
            {
                if (options.ThrowOnUnhandledException)
                    throw;

                UnhandledExceptionContext? exceptionContext = null;
                if (options.UnhandledExceptionDelegate != null)
                {
                    exceptionContext = new UnhandledExceptionContext(context, null, ex);
                    await options.UnhandledExceptionDelegate(exceptionContext).ConfigureAwait(false);
                    ex = exceptionContext.Exception;
                }

                (result ??= new()).AddError(ex is ExecutionError executionError ? executionError : new UnhandledError(exceptionContext?.ErrorMessage ?? "Error executing document.", ex));
            }
            finally
            {
                result ??= new();
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
        protected virtual ExecutionContext BuildExecutionContext(ExecutionOptions options, GraphQLDocument document, GraphQLOperationDefinition operation, Variables variables, Metrics metrics)
        {
            var context = new ExecutionContext
            {
                Document = document,
                Schema = options.Schema!,
                RootValue = options.Root,
                UserContext = options.UserContext,

                Operation = operation,
                Variables = variables,
                Errors = new ExecutionErrors(),
                InputExtensions = options.Extensions ?? Inputs.Empty,
                OutputExtensions = new Dictionary<string, object?>(),
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
        /// Returns the selected <see cref="GraphQLOperationDefinition"/> given a specified <see cref="GraphQLDocument"/> and operation name.
        /// <br/><br/>
        /// Returns <c>null</c> if an operation cannot be found that matches the given criteria.
        /// Returns the first operation from the document if no operation name was specified.
        /// </summary>
        protected virtual GraphQLOperationDefinition? GetOperation(string? operationName, GraphQLDocument document)
            => document.OperationWithName(operationName);

        /// <summary>
        /// Returns an instance of an <see cref="IExecutionStrategy"/> given specified execution parameters.
        /// <br/><br/>
        /// Typically the strategy is selected based on the type of operation.
        /// <br/><br/>
        /// By default, the selection is handled by the <see cref="IExecutionStrategySelector"/> implementation passed to the
        /// constructor, which will select an execution strategy based on a set of <see cref="ExecutionStrategyRegistration"/>
        /// instances passed to it.
        /// <br/><br/>
        /// For the <see cref="DefaultExecutionStrategySelector"/> without any registrations,
        /// query operations will return a <see cref="ParallelExecutionStrategy"/> while mutation operations return a
        /// <see cref="SerialExecutionStrategy"/>. Subscription operations return a <see cref="SubscriptionExecutionStrategy"/>.
        /// </summary>
        protected virtual IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
            => _executionStrategySelector.Select(context);
    }

    internal class DocumentExecuter<TSchema> : IDocumentExecuter<TSchema>
        where TSchema : ISchema
    {
        private readonly IDocumentExecuter _documentExecuter;
        public DocumentExecuter(IDocumentExecuter documentExecuter)
        {
            _documentExecuter = documentExecuter ?? throw new ArgumentNullException(nameof(documentExecuter));
        }

        public Task<ExecutionResult> ExecuteAsync(ExecutionOptions options)
        {
            if (options.Schema != null)
                throw new InvalidOperationException("ExecutionOptions.Schema must be null when calling this typed IDocumentExecuter<> implementation; it will be pulled from the dependency injection provider.");

            var requestServices = options.RequestServices ?? throw new MissingRequestServicesException();
            var schema = requestServices.GetRequiredService<TSchema>();
            options.Schema = schema;
            return _documentExecuter.ExecuteAsync(options);
        }
    }
}
