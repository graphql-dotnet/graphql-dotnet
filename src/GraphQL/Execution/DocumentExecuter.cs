using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQL.Validation;
using GraphQLParser.AST;
using ExecutionContext = GraphQL.Execution.ExecutionContext;

namespace GraphQL;

/// <summary>
/// <inheritdoc cref="IDocumentExecuter"/>
/// <br/><br/>
/// Default implementation for <see cref="IDocumentExecuter"/>.
/// </summary>
public class DocumentExecuter : IDocumentExecuter
{
    private readonly IDocumentBuilder _documentBuilder;
    private readonly IDocumentValidator _documentValidator;
    private readonly ExecutionDelegate _execution;
    private readonly IExecutionStrategySelector _executionStrategySelector;

    /// <summary>
    /// Initializes a new instance with default <see cref="IDocumentBuilder"/> and
    /// <see cref="IDocumentValidator"/> instances, and without document caching.
    /// </summary>
    public DocumentExecuter()
        : this(new GraphQLDocumentBuilder(), new DocumentValidator())
    {
    }

    /// <summary>
    /// Initializes a new instance with specified <see cref="IDocumentBuilder"/> and
    /// <see cref="IDocumentValidator"/>.
    /// </summary>
    public DocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator)
        : this(documentBuilder, documentValidator, new DefaultExecutionStrategySelector(), Array.Empty<IConfigureExecution>())
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified <see cref="IDocumentBuilder"/>,
    /// <see cref="IDocumentValidator"/>, <see cref="IExecutionStrategySelector"/> and
    /// a set of <see cref="IConfigureExecution"/> instances.
    /// </summary>
    public DocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IExecutionStrategySelector executionStrategySelector, IEnumerable<IConfigureExecution> configurations)
    {
        _documentBuilder = documentBuilder ?? throw new ArgumentNullException(nameof(documentBuilder));
        _documentValidator = documentValidator ?? throw new ArgumentNullException(nameof(documentValidator));
        _executionStrategySelector = executionStrategySelector ?? throw new ArgumentNullException(nameof(executionStrategySelector));
        _execution = BuildExecutionDelegate(configurations);
    }

    private ExecutionDelegate BuildExecutionDelegate(IEnumerable<IConfigureExecution> configurations)
    {
        ExecutionDelegate execution = CoreExecuteAsync;

        // OrderBy here performs a stable sort; that is, if the sort order of two elements are equal,
        // the order of the elements are preserved. The order is reversed since each execution wraps
        // the prior configured executions. OrderByDescending is not used because that would result
        // in a different sort order when there are two executions with equal sort orders.
        foreach (var action in configurations.OrderBy(x => x.SortOrder).Reverse())
        {
            var nextExecution = execution;
            execution = async options => await action.ExecuteAsync(options, nextExecution).ConfigureAwait(false);
        }
        return async options =>
        {
            try
            {
                return await execution(options).ConfigureAwait(false);
            }
            catch (GraphQLTimeoutException)
            {
                throw;
            }
            catch (OperationCanceledException) when (options.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ExecutionError ex)
            {
                return new ExecutionResult(ex);
            }
            catch (Exception ex)
            {
                if (options.ThrowOnUnhandledException)
                    throw;

                UnhandledExceptionContext? exceptionContext = null;
                if (options.UnhandledExceptionDelegate != null)
                {
                    exceptionContext = new UnhandledExceptionContext(options, ex);
                    await options.UnhandledExceptionDelegate(exceptionContext).ConfigureAwait(false);
                    ex = exceptionContext.Exception;
                }

                var executionError = ex as ExecutionError ??
                    new UnhandledError(exceptionContext?.ErrorMessage ?? "Error executing document.", ex);

                return new ExecutionResult(executionError);
            }
        };
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
            return new ExecutionResult(new QueryMissingError());
        options.CancellationToken.ThrowIfCancellationRequested();

        var metrics = (options.EnableMetrics ? new Metrics() : Metrics.None).Start(options.OperationName);

        ExecutionResult? result = null;
        ExecutionContext? context = null;
        bool executionOccurred = false;
        var originalCancellationToken = options.CancellationToken;
        CancellationTokenSource? cts = null;

        try
        {
            if (options.Timeout != Timeout.InfiniteTimeSpan)
            {
                cts = CancellationTokenSource.CreateLinkedTokenSource(originalCancellationToken);
                cts.CancelAfter(options.Timeout);
                options.CancellationToken = cts.Token;
            }
            if (!options.Schema.Initialized)
            {
                using (metrics.Subject("schema", "Initializing schema"))
                {
                    options.Schema.Initialize();
                }
            }

            var document = options.Document;
            if (document == null)
            {
                using (metrics.Subject("document", "Building document"))
                    document = _documentBuilder.Build(options.Query!);
            }

            var operation = GetOperation(options.OperationName, document);
            metrics.SetOperationName(operation.Name);

            IValidationResult validationResult;
            using (metrics.Subject("document", "Validating document"))
            {
                validationResult = await _documentValidator.ValidateAsync(
                    new ValidationOptions
                    {
                        Document = document,
                        Rules = options.ValidationRules,
                        Operation = operation,
                        UserContext = options.UserContext,
                        RequestServices = options.RequestServices,
                        User = options.User,
                        CancellationToken = options.CancellationToken,
                        Schema = options.Schema,
                        Metrics = metrics,
                        Variables = options.Variables ?? Inputs.Empty,
                        Extensions = options.Extensions ?? Inputs.Empty,
                    }).ConfigureAwait(false);
            }

            context = BuildExecutionContext(options, document, operation, validationResult, metrics);

            foreach (var listener in options.Listeners)
            {
                await listener.AfterValidationAsync(context, validationResult) // TODO: remove ExecutionContext or make different type ?
                    .ConfigureAwait(false);
            }

            if (!validationResult.IsValid || context.Errors.Count > 0)
            {
                var errors = !validationResult.IsValid ? validationResult.Errors : context.Errors;
                if (!validationResult.IsValid && context.Errors.Count > 0)
                    errors.AddRange(context.Errors);
                return new ExecutionResult
                {
                    Errors = errors,
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
        catch (OperationCanceledException) when (originalCancellationToken.IsCancellationRequested)
        {
            // Re-throw the original cancellation exception when the client disconnects or
            // the CancellationToken is canceled
            throw;
        }
        catch (OperationCanceledException) when (options.CancellationToken.IsCancellationRequested)
        {
            // If the operation was canceled due to a timeout, return a result with no data and an error
            // or throw a TimeoutException based on the configuration
            if (options.TimeoutAction == TimeoutAction.ThrowTimeoutException)
                throw new GraphQLTimeoutException();

            // Clear any pending execution result as it will not be left in a consistent state
            executionOccurred = false;
            (result = new()).AddError(new TimeoutError());
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
                exceptionContext = context == null
                    ? new UnhandledExceptionContext(options, ex)
                    : new UnhandledExceptionContext(context, null, ex);
                await options.UnhandledExceptionDelegate(exceptionContext).ConfigureAwait(false);
                ex = exceptionContext.Exception;
            }

            (result ??= new()).AddError(ex is ExecutionError executionError ? executionError : new UnhandledError(exceptionContext?.ErrorMessage ?? "Error executing document.", ex));
        }
        finally
        {
            options.CancellationToken = originalCancellationToken;
            cts?.Dispose();
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
    protected virtual ExecutionContext BuildExecutionContext(ExecutionOptions options, GraphQLDocument document, GraphQLOperationDefinition operation, IValidationResult validationResult, Metrics metrics)
    {
        var context = new ExecutionContext
        {
            Document = document,
            Schema = options.Schema!,
            RootValue = options.Root,
            UserContext = options.UserContext,
            ExecutionOptions = options,

            Operation = operation,
            Variables = validationResult.Variables ?? Variables.None,
            ArgumentValues = validationResult.ArgumentValues,
            DirectiveValues = validationResult.DirectiveValues,
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
            User = options.User,
        };

        context.ExecutionStrategy = SelectExecutionStrategy(context);

        return context;
    }

    /// <summary>
    /// Returns the selected <see cref="GraphQLOperationDefinition"/> given a specified <see cref="GraphQLDocument"/> and operation name.
    /// <br/><br/>
    /// Returns <see langword="null"/> if an operation cannot be found that matches the given criteria.
    /// Returns the first operation from the document if no operation name was specified.
    /// </summary>
    /// <exception cref="InvalidOperationNameError">Thrown when the operation name is specified but no operation can be found with that name.</exception>
    /// <exception cref="NoOperationNameError">Thrown when the document includes multiple operations, but no operation name was specified in the request.</exception>
    /// <exception cref="NoOperationError">Thrown when the document does not include any operations.</exception>
    protected virtual GraphQLOperationDefinition GetOperation(string? operationName, GraphQLDocument document)
    {
        if (operationName == null)
        {
            GraphQLOperationDefinition? match = null;
            foreach (var def in document.Definitions)
            {
                if (def is GraphQLOperationDefinition op)
                {
                    if (match != null)
                        throw new NoOperationNameError();
                    match = op;
                }
            }

            return match ?? throw new NoOperationError();
        }

        foreach (var def in document.Definitions)
        {
            if (def is GraphQLOperationDefinition op && op.Name == operationName)
                return op;
        }

        throw new InvalidOperationNameError(operationName);
    }

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

    /// <inheritdoc cref="TimeoutException"/>
    /// <remarks>
    /// In order that it be known that this exception is only thrown when the operation is canceled due to a timeout
    /// within <see cref="BuildExecutionDelegate(IEnumerable{IConfigureExecution})"/>, this class is private and only
    /// instantiated within <see cref="CoreExecuteAsync(ExecutionOptions)"/>. Callers should look for <see cref="TimeoutException"/>
    /// just like any other .NET timeout exception as this class derives from it.
    /// </remarks>
    private class GraphQLTimeoutException : TimeoutException;
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

        options.Schema = options.RequestServicesOrThrow().GetRequiredService<TSchema>();
        return _documentExecuter.ExecuteAsync(options);
    }
}
