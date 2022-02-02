using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI
{
    /// <summary>
    /// When resolving a field, this implementation calls
    /// <see cref="IResolveFieldContext.RequestServices"/>.<see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)">CreateScope</see>
    /// to create a dependency injection scope. Then it calls a predefined <see cref="Func{T, TResult}"/>, passing the scoped service provider
    /// within <see cref="IResolveFieldContext.RequestServices"/>, and returns the result.
    /// The returned value must be of an <see cref="Task{TResult}"/> type.
    /// </summary>
    public class ScopedAsyncFieldResolver<TReturnType> : AsyncFieldResolver<TReturnType>
    {
        /// <summary>
        /// Initializes a new instance that creates a service scope and runs the specified delegate when resolving a field.
        /// </summary>
        public ScopedAsyncFieldResolver(Func<IResolveFieldContext, Task<TReturnType>> resolver) : base(GetScopedResolver(resolver)) { }

        private static Func<IResolveFieldContext, Task<TReturnType>> GetScopedResolver(Func<IResolveFieldContext, Task<TReturnType>> resolver)
        {
            return async (context) =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return await resolver(new ScopedResolveFieldContextAdapter<object>(context, scope.ServiceProvider)).ConfigureAwait(false);
                }
            };
        }
    }

    /// <inheritdoc cref="ScopedAsyncFieldResolver{TReturnType}"/>
    public class ScopedAsyncFieldResolver<TSourceType, TReturnType> : AsyncFieldResolver<TReturnType>
    {
        /// <summary>
        /// Initializes a new instance that creates a service scope and runs the specified delegate when resolving a field.
        /// </summary>
        public ScopedAsyncFieldResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver) : base(GetScopedResolver(resolver)) { }

        private static Func<IResolveFieldContext, Task<TReturnType>> GetScopedResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            return async (context) =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return await resolver(new ScopedResolveFieldContextAdapter<TSourceType>(context, scope.ServiceProvider)).ConfigureAwait(false);
                }
            };
        }
    }
}
