#if !NETSTANDARD2_0
// for .NET Standard 2.0, this requires the Microsoft.Bcl.AsyncInterfaces NuGet package

using System.Security.Claims;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Resolvers;

internal class ObservableFromAsyncEnumerable<T> : IObservable<object?>, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private IAsyncEnumerable<T>? _enumerable;

    private ObservableFromAsyncEnumerable(CancellationTokenSource cts, IAsyncEnumerable<T> enumerable)
    {
        _cancellationTokenSource = cts;
        _enumerable = enumerable;
    }

    /// <summary>
    /// Returns a delegate that can be used to create an <see cref="IObservable{T}"/> from an <see cref="IAsyncEnumerable{T}"/>.
    /// Each execution will create a new <see cref="ObservableFromAsyncEnumerable{T}"/> instance which can only be used once.
    /// </summary>
    public static Func<IResolveFieldContext, ValueTask<IObservable<object?>>> Create(Func<IResolveFieldContext, ValueTask<IAsyncEnumerable<T>>> func)
    {
        return async context =>
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
            var enumerable = await func(new OverrideCancellationContext(context, cts.Token)).ConfigureAwait(false);
            return new ObservableFromAsyncEnumerable<T>(cts, enumerable);
        };
    }

    /// <inheritdoc cref="Create(Func{IResolveFieldContext, ValueTask{IAsyncEnumerable{T}}})"/>
    public static Func<IResolveFieldContext, ValueTask<IObservable<object?>>> Create(Func<IResolveFieldContext, Task<IAsyncEnumerable<T>>> func)
        => Create(context => new ValueTask<IAsyncEnumerable<T>>(func(context)));

    /// <inheritdoc cref="Create(Func{IResolveFieldContext, ValueTask{IAsyncEnumerable{T}}})"/>
    public static Func<IResolveFieldContext, ValueTask<IObservable<object?>>> Create(Func<IResolveFieldContext, IAsyncEnumerable<T>> func)
        => Create(context => new ValueTask<IAsyncEnumerable<T>>(func(context)));

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<object?> observer)
    {
        var enumerable = Interlocked.Exchange(ref _enumerable, null)
            ?? throw new InvalidOperationException("This method can only be called once.");

        _cancellationTokenSource.Token.ThrowIfCancellationRequested();

        // Fire and forget
        SubscribeInternalAsync(observer, enumerable);

        return this;
    }

    private async void SubscribeInternalAsync(IObserver<object?> observer, IAsyncEnumerable<T> enumerable)
    {
        try
        {
            // Enumerate the source and pass the items to the observer
            await foreach (var z in enumerable.WithCancellation(_cancellationTokenSource.Token).ConfigureAwait(false))
            {
                observer.OnNext(z);

                if (_cancellationTokenSource.IsCancellationRequested)
                    return;
            }

            observer.OnCompleted();
        }
        catch (Exception ex)
        {
            // may occur within func, the source enumerable, or within the observer
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                observer.OnError(ex);
            }
        }
    }

    void IDisposable.Dispose()
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
            _cancellationTokenSource.Cancel();
    }

    private sealed class OverrideCancellationContext : IResolveFieldContext
    {
        private readonly IResolveFieldContext _context;

        public OverrideCancellationContext(IResolveFieldContext context, CancellationToken cancellationToken)
        {
            _context = context;
            CancellationToken = cancellationToken;
        }

        public GraphQLField FieldAst => _context.FieldAst;

        public FieldType FieldDefinition => _context.FieldDefinition;

        public IObjectGraphType ParentType => _context.ParentType;

        public IResolveFieldContext? Parent => _context.Parent;

        public IDictionary<string, ArgumentValue>? Arguments => _context.Arguments;

        public IDictionary<string, DirectiveInfo>? Directives => _context.Directives;

        public object? RootValue => _context.RootValue;

        public object? Source => _context.Source;

        public ISchema Schema => _context.Schema;

        public GraphQLDocument Document => _context.Document;

        public GraphQLOperationDefinition Operation => _context.Operation;

        public Variables Variables => _context.Variables;

        public CancellationToken CancellationToken { get; }

        public Metrics Metrics => _context.Metrics;

        public ExecutionErrors Errors => _context.Errors;

        public IEnumerable<object> Path => _context.Path;

        public IEnumerable<object> ResponsePath => _context.ResponsePath;

        public Dictionary<string, (GraphQLField Field, FieldType FieldType)>? SubFields => _context.SubFields;

        public IReadOnlyDictionary<string, object?> InputExtensions => _context.InputExtensions;

        public IDictionary<string, object?> OutputExtensions => _context.OutputExtensions;

        public IServiceProvider? RequestServices => _context.RequestServices;

        public IExecutionArrayPool ArrayPool => _context.ArrayPool;

        public ClaimsPrincipal? User => _context.User;

        public IDictionary<string, object?> UserContext => _context.UserContext;
    }
}

#endif
