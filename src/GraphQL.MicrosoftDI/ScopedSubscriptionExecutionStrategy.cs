using GraphQL.Execution;
using Microsoft.Extensions.DependencyInjection;
using ExecutionContext = GraphQL.Execution.ExecutionContext;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// Executes a subscription request. During the execution of subsequent
/// data events, a dedicated service scope created and provided to
/// the execution strategy. As scoped services are typically not
/// designed to be multi-threaded, a serial execution strategy is
/// default, although any execution strategy can be specified.
/// <br/><br/>
/// Note that it is still required to execute the initial request via the
/// <see cref="DocumentExecuter"/> with a scoped service provider.
/// Once the <see cref="ExecutionResult"/> is returned, references
/// to the scoped service provider will have been released and
/// it can be safely disposed of.
/// </summary>
public class ScopedSubscriptionExecutionStrategy : SubscriptionExecutionStrategy
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Initializes a new instance with a serial execution strategy.
    /// </summary>
    public ScopedSubscriptionExecutionStrategy(IServiceScopeFactory serviceScopeFactory)
        : this(serviceScopeFactory, new SerialExecutionStrategy()) // create new instance of SerialExecutionStrategy so there is a new instance of the reusable fields
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified execution strategy for child nodes.
    /// </summary>
    public ScopedSubscriptionExecutionStrategy(IServiceScopeFactory serviceScopeFactory, IExecutionStrategy executionStrategy)
        : base(executionStrategy)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <inheritdoc/>
    protected override ExecutionContext CloneExecutionContext(ExecutionContext context, CancellationToken token)
    {
        var newContext = base.CloneExecutionContext(context, token);
        // prevent the initial service provider, likely a scoped service provider
        // which will be disposed after the initial execution completes,
        // from having a reference held to it from the preserved execution context
        // necessary for subsequent data event executions
        newContext.RequestServices = null;
        return newContext;
    }

    /// <summary>
    /// Processes data from the source stream via <see cref="IObserver{T}.OnNext(T)"/> and
    /// returns an <see cref="ExecutionResult"/>.
    /// <br/><br/>
    /// Executes with a scoped service provider in <see cref="ExecutionContext.RequestServices"/>
    /// which is disposed once this method completes.
    /// </summary>
    protected override async ValueTask<ExecutionResult> ProcessDataAsync(ExecutionContext context, ExecutionNode node, object? value)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        context.RequestServices = scope.ServiceProvider;
        return await base.ProcessDataAsync(context, node, value).ConfigureAwait(false);
    }

    /// <summary>
    /// Encapsulates an error within an <see cref="ExecutionResult"/> for errors generated
    /// by the event stream via <see cref="IObserver{T}.OnError(Exception)"/>.
    /// <br/><br/>
    /// Executes with a scoped service provider in <see cref="ExecutionContext.RequestServices"/>
    /// which is disposed once this method completes.
    /// </summary>
    protected override Task<ExecutionError> ProcessErrorAsync(ExecutionContext context, ExecutionNode node, Exception exception)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        context.RequestServices = scope.ServiceProvider;
        return base.ProcessErrorAsync(context, node, exception);
    }
}
