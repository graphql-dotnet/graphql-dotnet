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
            IObservable<object?> observable;
            try
            {
                var scopedContext = new ScopedResolveFieldContextAdapter<object>(context, scope.ServiceProvider);
                observable = await resolver.ResolveAsync(scopedContext).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("The source stream resolver returned null.");
            }
            catch
            {
                scope.Dispose();
                throw;
            }
            // keep the service scope alive until the subscription has been disposed
            return new ObservableMapper(observable, scope);
        };
    }

    /// <inheritdoc/>
    public ValueTask<IObservable<object?>> ResolveAsync(IResolveFieldContext context) => _resolverFunc(context);

    /// <summary>
    /// Disposes of a service scope when the source stream is disposed.
    /// </summary>
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
            // note: this error should not occur, as GraphQL.NET does not call IObservable<T>.Subscribe
            // more than once after executing the source stream resolver
            var observable = Interlocked.Exchange(ref _observable, null)
                ?? throw new InvalidOperationException("This method can only be called once.");
            _disposable = observable.Subscribe(observer);
            return this;
        }

        void IDisposable.Dispose()
        {
            Interlocked.Exchange(ref _disposable, null)?.Dispose();
            Interlocked.Exchange(ref _serviceScope, null)?.Dispose();
        }
    }
}
