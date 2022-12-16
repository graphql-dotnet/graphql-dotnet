using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// When resolving a field, this implementation calls
/// <see cref="IResolveFieldContext.RequestServices"/>.<see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)">CreateScope</see>
/// to create a dependency injection scope. Then it calls the specified <see cref="ISourceStreamResolver"/>, passing the scoped service provider
/// within <see cref="IResolveFieldContext.RequestServices"/>, and returns the result.
/// </summary>
internal class DynamicScopedSourceStreamResolver : ISourceStreamResolver
{
    private readonly Func<IResolveFieldContext, ValueTask<IObservable<object?>>> _resolverFunc;

    /// <summary>
    /// Initializes a new instance that creates a service scope and runs the specified delegate when resolving a field.
    /// </summary>
    public DynamicScopedSourceStreamResolver(ISourceStreamResolver resolver)
    {
        _resolverFunc = async context =>
        {
            var scope = context.RequestServicesOrThrow().CreateScope();
            var scopedContext = new ScopedResolveFieldContextAdapter<object>(context, scope.ServiceProvider);
            var observable = await resolver.ResolveAsync(scopedContext).ConfigureAwait(false);
            return new ObservableMapper(observable, scope);
        };
    }

    /// <inheritdoc/>
    public ValueTask<IObservable<object?>> ResolveAsync(IResolveFieldContext context) => _resolverFunc(context);

    private class ObservableMapper : IObservable<object?>, IDisposable
    {
        private IObservable<object?>? _observable;
        private IServiceScope? _serviceScope;
        private IDisposable? _disposable;

        public ObservableMapper(IObservable<object?> observable, IServiceScope serviceScope)
        {
            _observable = observable;
            _serviceScope = serviceScope;
        }

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            var observable = Interlocked.Exchange(ref _observable, null)
                ?? throw new InvalidOperationException("This method can only be called once.");
            _disposable = observable.Subscribe(observer);
            return this;
        }

        void IDisposable.Dispose()
        {
            var disposable = Interlocked.Exchange(ref _disposable, null);
            disposable?.Dispose();
            var scope = Interlocked.Exchange(ref _serviceScope, null);
            scope?.Dispose();
        }
    }
}
