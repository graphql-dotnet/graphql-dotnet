using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI
{
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
                using var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope();
                return await resolver.ResolveAsync(new ScopedResolveFieldContextAdapter<object>(context, scope.ServiceProvider)).ConfigureAwait(false);
            };
        }

        /// <inheritdoc/>
        public ValueTask<IObservable<object?>> ResolveAsync(IResolveFieldContext context) => _resolverFunc(context);
    }
}
