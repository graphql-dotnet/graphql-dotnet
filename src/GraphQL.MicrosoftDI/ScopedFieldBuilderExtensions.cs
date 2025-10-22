using GraphQL.Builders;
using GraphQL.DataLoader;
using GraphQL.Instrumentation;
using GraphQL.Types;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// Extension methods for creating resolver builders.
/// </summary>
public static class ScopedFieldBuilderExtensions
{
    // Cached transform delegate to avoid allocations
    private static readonly Func<IServiceProvider, IFieldMiddleware> _scopedTransform = static _ => ScopedFieldMiddleware.Instance;

    /// <summary>
    /// Applies scoped middleware to the field. A dependency injection scope is created for the duration of the field resolver's execution
    /// and the scoped service provider is passed within <see cref="IResolveFieldContext.RequestServices"/>.
    /// </summary>
    [AllowedOn<IObjectGraphType>]
    internal static FieldBuilder<TSourceType, TReturnType> Scoped<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder)
    {
        // Apply scoped middleware to the field using transform function
        var existingTransform = builder.FieldType.Middleware;
        if (existingTransform == null)
        {
            builder.FieldType.Middleware = _scopedTransform;
        }
        else
        {
            // Chain the middleware
            builder.FieldType.Middleware = serviceProvider =>
            {
                var existing = existingTransform(serviceProvider);
                return new ScopedFieldMiddleware(existing);
            };
        }

        // Wrap the stream resolver, if any, to create a scope for subscriptions
        if (builder.FieldType.StreamResolver != null && builder.FieldType.StreamResolver is not DynamicScopedSourceStreamResolver)
            builder.FieldType.StreamResolver = new DynamicScopedSourceStreamResolver(builder.FieldType.StreamResolver);

        return builder;
    }

    /// <summary>
    /// Sets the resolver for the field. A dependency injection scope is created for the duration of the resolver's execution
    /// and the scoped service provider is passed within <see cref="IResolveFieldContext.RequestServices"/>.
    /// </summary>
    [AllowedOn<IObjectGraphType>]
    public static FieldBuilder<TSourceType, TReturnType> ResolveScoped<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder, Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver)
        => builder.Resolve(resolver).Scoped();

    /// <inheritdoc cref="ResolveScoped{TSourceType, TReturnType}(FieldBuilder{TSourceType, TReturnType}, Func{IResolveFieldContext{TSourceType}, TReturnType})"/>
    [AllowedOn<IObjectGraphType>]
    public static FieldBuilder<TSourceType, TReturnType> ResolveScopedAsync<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder, Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver)
        => builder.ResolveAsync(resolver).Scoped();

    /// <summary>
    /// Creates a resolve builder for the field.
    /// </summary>
    [AllowedOn<IObjectGraphType>]
    public static ResolverBuilder<TSourceType, TReturnType> Resolve<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder)
        => new(builder, false);

    /// <summary>
    /// Indicates that the resolver for this field will be returning a data loader.
    /// </summary>
    internal static FieldBuilder<TSourceType, IDataLoaderResult<TReturnType>> ReturnsDataLoader<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder)
        => builder.Returns<IDataLoaderResult<TReturnType>>();
}
