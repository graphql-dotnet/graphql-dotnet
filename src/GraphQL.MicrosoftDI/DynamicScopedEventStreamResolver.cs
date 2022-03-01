using GraphQL.Resolvers;
using GraphQL.Subscription;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI
{
    /// <summary>
    /// When resolving a field, this implementation calls
    /// <see cref="IResolveFieldContext.RequestServices"/>.<see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)">CreateScope</see>
    /// to create a dependency injection scope. Then it calls the specified <see cref="IAsyncEventStreamResolver"/>, passing the scoped service provider
    /// within <see cref="IResolveFieldContext.RequestServices"/>, and returns the result.
    /// </summary>
    internal class DynamicScopedEventStreamResolver : IAsyncEventStreamResolver
    {
        private readonly Func<IResolveEventStreamContext, Task<IObservable<object?>>> _resolverFunc;

        /// <summary>
        /// Initializes a new instance that creates a service scope and runs the specified delegate when resolving a field.
        /// </summary>
        public DynamicScopedEventStreamResolver(IAsyncEventStreamResolver resolver)
        {
            _resolverFunc = async context =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return await resolver.SubscribeAsync(new ScopedResolveEventStreamContextAdapter(context, scope.ServiceProvider)).ConfigureAwait(false);
                }
            };
        }

        /// <inheritdoc/>
        public Task<IObservable<object?>> SubscribeAsync(IResolveEventStreamContext context) => _resolverFunc(context);
    }
}
