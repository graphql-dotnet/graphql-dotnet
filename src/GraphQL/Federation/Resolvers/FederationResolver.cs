using GraphQL.DataLoader;

namespace GraphQL.Federation.Resolvers;

/// <summary>
/// Provides a concrete implementation of <see cref="IFederationResolver"/> for a specific source type.
/// This class simplifies the resolution process when the source and return types are the same.
/// </summary>
/// <typeparam name="TClrType">The CLR type of the source and return representation that this resolver handles.</typeparam>
public class FederationResolver<TClrType> : FederationResolver<TClrType, TClrType>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FederationResolver{TClrType}"/> class
    /// using a synchronous resolve function.
    /// </summary>
    /// <param name="resolveFunc">The function used to resolve the source representation.</param>
    public FederationResolver(Func<IResolveFieldContext, TClrType, TClrType?> resolveFunc)
        : base(resolveFunc)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationResolver{TClrType}"/> class
    /// using an asynchronous resolve function.
    /// </summary>
    /// <param name="resolveFunc">The function used to asynchronously resolve the source representation.</param>
    public FederationResolver(Func<IResolveFieldContext, TClrType, Task<TClrType?>> resolveFunc)
        : base(resolveFunc)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationResolver{TClrType}"/> class
    /// using a data loader resolve function.
    /// </summary>
    /// <param name="resolveFunc">The function used to resolve the source representation using a data loader.</param>
    public FederationResolver(Func<IResolveFieldContext, TClrType, IDataLoaderResult<TClrType?>> resolveFunc)
        : base(resolveFunc)
    {
    }
}

/// <summary>
/// Provides a concrete implementation of <see cref="IFederationResolver"/> for specific source and return types.
/// This class supports various resolve function signatures, including synchronous, asynchronous, and data loader based.
/// </summary>
/// <typeparam name="TSourceType">The CLR type of the source representation that this resolver handles.</typeparam>
/// <typeparam name="TReturnType">The CLR type of the resolved object returned by this resolver.</typeparam>
public class FederationResolver<TSourceType, TReturnType> : IFederationResolver
{
    internal readonly Func<IResolveFieldContext, TSourceType, ValueTask<object?>> _resolveFunc;

    /// <summary>
    /// Gets the CLR type of the representation that this resolver is responsible for.
    /// </summary>
    public Type SourceType => typeof(TSourceType);

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationResolver{TSourceType, TReturnType}"/> class
    /// using a synchronous resolve function.
    /// </summary>
    /// <param name="resolveFunc">The function used to resolve the source representation.</param>
    public FederationResolver(Func<IResolveFieldContext, TSourceType, TReturnType?> resolveFunc)
    {
        _resolveFunc = (ctx, source) => new(resolveFunc(ctx, source));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationResolver{TSourceType, TReturnType}"/> class
    /// using an asynchronous resolve function.
    /// </summary>
    /// <param name="resolveFunc">The function used to asynchronously resolve the source representation.</param>
    public FederationResolver(Func<IResolveFieldContext, TSourceType, Task<TReturnType?>> resolveFunc)
    {
        _resolveFunc = async (ctx, source) => (await resolveFunc(ctx, source).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationResolver{TSourceType, TReturnType}"/> class
    /// using a data loader resolve function.
    /// </summary>
    /// <param name="resolveFunc">The function used to resolve the source representation using a data loader.</param>
    public FederationResolver(Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TReturnType?>> resolveFunc)
    {
        _resolveFunc = (ctx, source) => new(resolveFunc(ctx, source));
    }

    /// <summary>
    /// Asynchronously resolves an object based on the given context and source representation.
    /// </summary>
    /// <param name="context">The context of the field being resolved, providing access to various aspects of the GraphQL execution.</param>
    /// <param name="source">The source representation, converted to the CLR type specified by <see cref="SourceType"/>.</param>
    /// <returns>A task that represents the asynchronous resolve operation. The task result contains the resolved object.</returns>
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context, object source) => _resolveFunc(context, (TSourceType)source)!;
}
