using GraphQL.Builders;
using GraphQL.DataLoader;
using GraphQL.Types;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// Extension methods for creating resolver builders.
/// </summary>
public static class ScopedFieldBuilderExtensions
{
    /// <summary>
    /// Sets the resolver for the field. A dependency injection scope is created for the duration of the resolver's execution
    /// and the scoped service provider is passed within <see cref="IResolveFieldContext.RequestServices"/>.
    /// </summary>
    [AllowedOn<IObjectGraphType>]
    public static FieldBuilder<TSourceType, TReturnType> ResolveScoped<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder, Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver)
        => builder.Resolve(new ScopedFieldResolver<TSourceType, TReturnType>(resolver));

    /// <inheritdoc cref="ResolveScoped{TSourceType, TReturnType}(FieldBuilder{TSourceType, TReturnType}, Func{IResolveFieldContext{TSourceType}, TReturnType})"/>
    [AllowedOn<IObjectGraphType>]
    public static FieldBuilder<TSourceType, TReturnType> ResolveScopedAsync<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder, Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver)
        => builder.Resolve(new ScopedFieldResolver<TSourceType, TReturnType>(context => new ValueTask<TReturnType?>(resolver(context))));

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
