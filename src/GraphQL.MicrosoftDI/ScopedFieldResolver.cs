using System;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI
{
    /// <summary>
    /// When resolving a field, this implementation calls
    /// <see cref="IResolveFieldContext.RequestServices"/>.<see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)">CreateScope</see>
    /// to create a dependency injection scope. Then it calls a predefined <see cref="Func{T, TResult}"/>, passing the scoped service provider
    /// within <see cref="IResolveFieldContext.RequestServices"/>, and returns the result.
    /// </summary>
    public class ScopedFieldResolver<TSourceType, TReturnType> : FuncFieldResolver<TSourceType, TReturnType>
    {
        /// <summary>
        /// Initializes a new instance that creates a service scope and runs the specified delegate when resolving a field.
        /// </summary>
        public ScopedFieldResolver(Func<IResolveFieldContext<TSourceType>, TReturnType> resolver) : base(GetScopedResolver(resolver)) { }

        private static Func<IResolveFieldContext<TSourceType>, TReturnType> GetScopedResolver(Func<IResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            return context =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return resolver(new ScopedResolveFieldContextAdapter<TSourceType>(context, scope.ServiceProvider));
                }
            };
        }
    }
}
