using GraphQL.Execution;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// When resolving a field, this implementation calls
/// <see cref="IResolveFieldContext.RequestServices"/>.<see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)">CreateScope</see>
/// to create a dependency injection scope. Then it calls a predefined <see cref="Func{T, TResult}"/>, passing the scoped service provider
/// within <see cref="IResolveFieldContext.RequestServices"/>, and returns the result.
/// </summary>
[Obsolete("Please use ScopedFieldMiddleware instead. This class will be removed in a future release.")]
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
            var newContext = new ScopedResolveFieldContextAdapter<object>(context, scope.ServiceProvider);
            var accessor = scope.ServiceProvider.GetService<IResolveFieldContextAccessor>();
            accessor?.Context = newContext;
            try
            {
                return resolver(newContext);
            }
            finally
            {
                accessor?.Context = context;
            }
        };
    }

    /// <inheritdoc cref="ScopedFieldResolver{TReturnType}(Func{IResolveFieldContext, TReturnType})"/>
    public ScopedFieldResolver(Func<IResolveFieldContext, ValueTask<TReturnType?>> resolver) : base(GetScopedResolver(resolver)) { }

    private static Func<IResolveFieldContext, ValueTask<TReturnType?>> GetScopedResolver(Func<IResolveFieldContext, ValueTask<TReturnType?>> resolver)
    {
        return async context =>
        {
            using var scope = context.RequestServicesOrThrow().CreateScope();
            var newContext = new ScopedResolveFieldContextAdapter<object>(context, scope.ServiceProvider);
            var accessor = scope.ServiceProvider.GetService<IResolveFieldContextAccessor>();
            accessor?.Context = newContext;
            try
            {
                return await resolver(newContext).ConfigureAwait(false);
            }
            finally
            {
                accessor?.Context = context;
            }
        };
    }
}

/// <inheritdoc cref="ScopedFieldResolver{TReturnType}"/>
[Obsolete("Please use ScopedFieldMiddleware instead. This class will be removed in a future release.")]
public class ScopedFieldResolver<TSourceType, TReturnType> : FuncFieldResolver<TReturnType>
{
    /// <inheritdoc cref="ScopedFieldResolver{TReturnType}(Func{IResolveFieldContext, TReturnType})"/>
    public ScopedFieldResolver(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver) : base(GetScopedResolver(resolver)) { }

    private static Func<IResolveFieldContext, TReturnType?> GetScopedResolver(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver)
    {
        return context =>
        {
            using var scope = context.RequestServicesOrThrow().CreateScope();
            var newContext = new ScopedResolveFieldContextAdapter<TSourceType>(context, scope.ServiceProvider);
            var accessor = scope.ServiceProvider.GetService<IResolveFieldContextAccessor>();
            accessor?.Context = newContext;
            try
            {
                return resolver(newContext);
            }
            finally
            {
                accessor?.Context = context;
            }
        };
    }

    /// <inheritdoc cref="ScopedFieldResolver{TSourceType, TReturnType}(Func{IResolveFieldContext{TSourceType}, TReturnType})" />
    public ScopedFieldResolver(Func<IResolveFieldContext<TSourceType>, ValueTask<TReturnType?>> resolver) : base(GetScopedResolver(resolver)) { }

    private static Func<IResolveFieldContext, ValueTask<TReturnType?>> GetScopedResolver(Func<IResolveFieldContext<TSourceType>, ValueTask<TReturnType?>> resolver)
    {
        return async context =>
        {
            using var scope = context.RequestServicesOrThrow().CreateScope();
            var newContext = new ScopedResolveFieldContextAdapter<TSourceType>(context, scope.ServiceProvider);
            var accessor = scope.ServiceProvider.GetService<IResolveFieldContextAccessor>();
            accessor?.Context = newContext;
            try
            {
                return await resolver(newContext).ConfigureAwait(false);
            }
            finally
            {
                accessor?.Context = context;
            }
        };
    }
}
