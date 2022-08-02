using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// When resolving a field, this implementation calls
/// <see cref="IResolveFieldContext.RequestServices"/>.<see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)">CreateScope</see>
/// to create a dependency injection scope. Then it calls a predefined <see cref="Func{T, TResult}"/>, passing the scoped service provider
/// within <see cref="IResolveFieldContext.RequestServices"/>, and returns the result.
/// </summary>
public class ScopedFieldResolver<TReturnType> : FuncFieldResolver<TReturnType>
{
    /// <summary>
    /// Initializes a new instance that creates a service scope and runs the specified delegate when resolving a field.
    /// </summary>
    public ScopedFieldResolver(Func<IResolveFieldContext, TReturnType?> resolver) : base(GetScopedResolver(resolver)) { }

    private static Func<IResolveFieldContext, TReturnType?> GetScopedResolver(Func<IResolveFieldContext, TReturnType?> resolver)
    {
        return context =>
        {
            using var scope = context.RequestServicesOrThrow().CreateScope();
            return resolver(new ScopedResolveFieldContextAdapter<object>(context, scope.ServiceProvider));
        };
    }

    /// <inheritdoc cref="ScopedFieldResolver{TReturnType}.ScopedFieldResolver(Func{IResolveFieldContext, TReturnType})"/>
    public ScopedFieldResolver(Func<IResolveFieldContext, ValueTask<TReturnType?>> resolver) : base(GetScopedResolver(resolver)) { }

    private static Func<IResolveFieldContext, ValueTask<TReturnType?>> GetScopedResolver(Func<IResolveFieldContext, ValueTask<TReturnType?>> resolver)
    {
        return async context =>
        {
            using var scope = context.RequestServicesOrThrow().CreateScope();
            return await resolver(new ScopedResolveFieldContextAdapter<object>(context, scope.ServiceProvider)).ConfigureAwait(false);
        };
    }
}

/// <inheritdoc cref="ScopedFieldResolver{TReturnType}"/>
public class ScopedFieldResolver<TSourceType, TReturnType> : FuncFieldResolver<TReturnType>
{
    /// <inheritdoc cref="ScopedFieldResolver{TReturnType}.ScopedFieldResolver(Func{IResolveFieldContext, TReturnType})"/>
    public ScopedFieldResolver(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver) : base(GetScopedResolver(resolver)) { }

    private static Func<IResolveFieldContext, TReturnType?> GetScopedResolver(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver)
    {
        return context =>
        {
            using var scope = context.RequestServicesOrThrow().CreateScope();
            return resolver(new ScopedResolveFieldContextAdapter<TSourceType>(context, scope.ServiceProvider));
        };
    }

    /// <inheritdoc cref="ScopedFieldResolver{TSourceType, TReturnType}.ScopedFieldResolver(Func{IResolveFieldContext{TSourceType}, TReturnType})" />
    public ScopedFieldResolver(Func<IResolveFieldContext<TSourceType>, ValueTask<TReturnType?>> resolver) : base(GetScopedResolver(resolver)) { }

    private static Func<IResolveFieldContext, ValueTask<TReturnType?>> GetScopedResolver(Func<IResolveFieldContext<TSourceType>, ValueTask<TReturnType?>> resolver)
    {
        return async context =>
        {
            using var scope = context.RequestServicesOrThrow().CreateScope();
            return await resolver(new ScopedResolveFieldContextAdapter<TSourceType>(context, scope.ServiceProvider)).ConfigureAwait(false);
        };
    }
}
