using System;
using System.Threading.Tasks;
using GraphQL.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI
{
    /// <summary>
    /// Extension methods for creating connection resolver builders.
    /// </summary>
    public static class ScopedConnectionBuilderExtensions
    {
        /// <summary>
        /// Sets the resolver for the connection field. A dependency injection scope is created for the duration of the resolver's execution
        /// and the scoped service provider is passed within <see cref="IResolveFieldContext.RequestServices"/>.
        /// </summary>
        public static void ResolveScoped<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType> builder, Func<IResolveConnectionContext<TSourceType>, TReturnType> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            builder.Resolve(context =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return resolver(new ScopedResolveConnectionContextAdapter<TSourceType>(context, scope.ServiceProvider));
                }
            });
        }

        /// <inheritdoc cref="ResolveScopedAsync{TSourceType, TReturnType}(ConnectionBuilder{TSourceType}, Func{IResolveConnectionContext{TSourceType}, Task{TReturnType}})"/>
        public static void ResolveScopedAsync<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType> builder, Func<IResolveConnectionContext<TSourceType>, Task<TReturnType>> resolver)
            => builder.ResolveScoped(resolver);

        /// <summary>
        /// Creates a resolve builder for the connection field.
        /// </summary>
        public static ConnectionResolverBuilder<TSourceType, object> Resolve<TSourceType>(this ConnectionBuilder<TSourceType> builder)
            => new ConnectionResolverBuilder<TSourceType, object>(builder, false);
    }
}
