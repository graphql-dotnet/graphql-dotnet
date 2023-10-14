using GraphQL.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// Extension methods for creating connection resolver builders.
/// </summary>
public static class ScopedConnectionBuilderExtensions
{
    /// <summary>
    /// Sets the resolver for the connection field. A dependency injection scope is created for the duration of the resolver's execution
    /// and the scoped service provider is passed within <see cref="IResolveFieldContext.RequestServices"/>. This method must be called after
    /// <see cref="ConnectionBuilder{TSourceType, TReturnType}.PageSize(int?)">PageSize</see> and/or
    /// <see cref="ConnectionBuilder{TSourceType, TReturnType}.Bidirectional">Bidirectional</see> have been called.
    /// </summary>
    public static void ResolveScoped<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType> builder, Func<IResolveConnectionContext<TSourceType>, TReturnType?> resolver)
    {
        if (resolver == null)
            throw new ArgumentNullException(nameof(resolver));
        builder.Resolve(context =>
        {
            using var scope = context.RequestServicesOrThrow().CreateScope();
            return resolver(new ScopedResolveConnectionContextAdapter<TSourceType>(context, scope.ServiceProvider));
        });
    }

    /// <inheritdoc cref="ResolveScoped{TSourceType, TReturnType}(ConnectionBuilder{TSourceType}, Func{IResolveConnectionContext{TSourceType}, TReturnType})"/>
    public static void ResolveScopedAsync<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType> builder, Func<IResolveConnectionContext<TSourceType>, Task<TReturnType?>> resolver)
    {
        if (resolver == null)
            throw new ArgumentNullException(nameof(resolver));
        builder.ResolveAsync(async context =>
        {
            using var scope = context.RequestServicesOrThrow().CreateScope();
            return await resolver(new ScopedResolveConnectionContextAdapter<TSourceType>(context, scope.ServiceProvider)).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Creates a resolve builder for the connection field. This method must be called after
    /// <see cref="ConnectionBuilder{TSourceType, TReturnType}.PageSize(int?)">PageSize</see> and/or
    /// <see cref="ConnectionBuilder{TSourceType, TReturnType}.Bidirectional">Bidirectional</see> have been called.
    /// </summary>
    public static ConnectionResolverBuilder<TSourceType, object> Resolve<TSourceType>(this ConnectionBuilder<TSourceType> builder)
        => new(builder.Returns<object>(), false);

    /// <inheritdoc cref="ResolveScoped{TSourceType, TReturnType}(ConnectionBuilder{TSourceType}, Func{IResolveConnectionContext{TSourceType}, TReturnType})"/>
    public static void ResolveScoped<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType, TReturnType> builder, Func<IResolveConnectionContext<TSourceType>, TReturnType?> resolver)
    {
        if (resolver == null)
            throw new ArgumentNullException(nameof(resolver));
        builder.Resolve(context =>
        {
            using var scope = context.RequestServicesOrThrow().CreateScope();
            return resolver(new ScopedResolveConnectionContextAdapter<TSourceType>(context, scope.ServiceProvider));
        });
    }

    /// <inheritdoc cref="ResolveScopedAsync{TSourceType, TReturnType}(ConnectionBuilder{TSourceType}, Func{IResolveConnectionContext{TSourceType}, Task{TReturnType}})"/>
    public static void ResolveScopedAsync<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType, TReturnType> builder, Func<IResolveConnectionContext<TSourceType>, Task<TReturnType?>> resolver)
    {
        if (resolver == null)
            throw new ArgumentNullException(nameof(resolver));
        builder.ResolveAsync(async context =>
        {
            using var scope = context.RequestServicesOrThrow().CreateScope();
            return await resolver(new ScopedResolveConnectionContextAdapter<TSourceType>(context, scope.ServiceProvider)).ConfigureAwait(false);
        });
    }

    /// <inheritdoc cref="Resolve{TSourceType}(ConnectionBuilder{TSourceType})"/>
    public static ConnectionResolverBuilder<TSourceType, TReturnType> Resolve<TSourceType, TReturnType>(this ConnectionBuilder<TSourceType, TReturnType> builder)
        => new(builder, false);

    /// <summary>
    /// Returns <see cref="IResolveFieldContext.RequestServices"/> if specified or throws <see cref="MissingRequestServicesException"/>.
    /// </summary>
    internal static IServiceProvider RequestServicesOrThrow(this IResolveFieldContext context)
        => context.RequestServices ?? throw new MissingRequestServicesException();
}
