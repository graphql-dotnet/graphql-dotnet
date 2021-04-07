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
        public static ConnectionBuilder<TSourceType> ResolveScoped<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType> builder, Func<IResolveConnectionContext<TSourceType>, TReturnType> resolver)
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
            return builder;
        }

        /// <inheritdoc cref="ResolveScopedAsync{TSourceType, TReturnType}(ConnectionBuilder{TSourceType}, Func{IResolveConnectionContext{TSourceType}, Task{TReturnType}})"/>
        public static ConnectionBuilder<TSourceType> ResolveScopedAsync<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType> builder, Func<IResolveConnectionContext<TSourceType>, Task<TReturnType>> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            builder.ResolveAsync(async context =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return await resolver(new ScopedResolveConnectionContextAdapter<TSourceType>(context, scope.ServiceProvider));
                }
            });
            return builder;
        }

        /// <summary>
        /// Creates a resolve builder for the connection field.
        /// </summary>
        public static ConnectionResolverBuilder<TSourceType, object> Resolve<TSourceType>(this ConnectionBuilder<TSourceType> builder)
            => new ConnectionResolverBuilder<TSourceType, object>(builder.Returns<object>(), false);

        /// <summary>
        /// Sets the resolver for the connection field. A dependency injection scope is created for the duration of the resolver's execution
        /// and the scoped service provider is passed within <see cref="IResolveFieldContext.RequestServices"/>.
        /// </summary>
        public static ConnectionBuilder<TSourceType, TReturnType> ResolveScoped<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType, TReturnType> builder, Func<IResolveConnectionContext<TSourceType>, TReturnType> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            return builder.Resolve(context =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return resolver(new ScopedResolveConnectionContextAdapter<TSourceType>(context, scope.ServiceProvider));
                }
            });
        }

        /// <inheritdoc cref="ResolveScopedAsync{TSourceType, TReturnType}(ConnectionBuilder{TSourceType}, Func{IResolveConnectionContext{TSourceType}, Task{TReturnType}})"/>
        public static ConnectionBuilder<TSourceType, TReturnType> ResolveScopedAsync<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType, TReturnType> builder, Func<IResolveConnectionContext<TSourceType>, Task<TReturnType>> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            return builder.ResolveAsync(async context =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return await resolver(new ScopedResolveConnectionContextAdapter<TSourceType>(context, scope.ServiceProvider));
                }
            });
        }

        /// <summary>
        /// Creates a resolve builder for the connection field.
        /// </summary>
        public static ConnectionResolverBuilder<TSourceType, TReturnType> Resolve<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType, TReturnType> builder)
            => new ConnectionResolverBuilder<TSourceType, TReturnType>(builder, false);
    }
}
