using GraphQL.DataLoader;
using GraphQL.Federation.Resolvers;
using GraphQL.Types;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Federation;

/// <summary>
/// Federation extensions for Graph Types.
/// </summary>
public static class FederationGraphTypeExtensions
{
    /// <summary>
    /// Configures synchronous resolution of a reference using a resolver function.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <param name="graphType">The graph type to apply the resolver to.</param>
    /// <param name="resolver">The function used to resolve the source representation.</param>
    public static void ResolveReference<TSourceType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, TSourceType?> resolver) =>
        graphType.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Configures asynchronous resolution of a reference using a task-based resolver function.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <param name="graphType">The graph type to apply the resolver to.</param>
    /// <param name="resolver">The asynchronous function used to resolve the source representation.</param>
    public static void ResolveReference<TSourceType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, Task<TSourceType?>> resolver) =>
        graphType.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Configures resolution of a reference using a data loader-based resolver function.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <param name="graphType">The graph type to apply the resolver to.</param>
    /// <param name="resolver">The data loader function used to resolve the source representation.</param>
    public static void ResolveReference<TSourceType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TSourceType?>> resolver) =>
        graphType.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Configures synchronous resolution of a reference using a resolver function with a return type.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <typeparam name="TReturnType">The CLR type of the resolved object returned by the resolver.</typeparam>
    /// <param name="graphType">The graph type to apply the resolver to.</param>
    /// <param name="resolver">The function used to resolve the source representation.</param>
    public static void ResolveReference<TSourceType, TReturnType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, TReturnType?> resolver)
    {
        graphType.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType, TReturnType>(resolver);
    }

    /// <summary>
    /// Configures asynchronous resolution of a reference using a task-based resolver function with a return type.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <typeparam name="TReturnType">The CLR type of the resolved object returned by the resolver.</typeparam>
    /// <param name="graphType">The graph type to apply the resolver to.</param>
    /// <param name="resolver">The asynchronous function used to resolve the source representation.</param>
    public static void ResolveReference<TSourceType, TReturnType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, Task<TReturnType?>> resolver)
    {
        graphType.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType, TReturnType>(resolver);
    }

    /// <summary>
    /// Configures resolution of a reference using a data loader-based resolver function with a return type.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <typeparam name="TReturnType">The CLR type of the resolved object returned by the resolver.</typeparam>
    /// <param name="graphType">The graph type to apply the resolver to.</param>
    /// <param name="resolver">The data loader function used to resolve the source representation.</param>
    public static void ResolveReference<TSourceType, TReturnType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TReturnType?>> resolver)
    {
        graphType.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType, TReturnType>(resolver);
    }

    /// <summary>
    /// Configures resolution of a reference using a specified federation resolver.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <param name="graphType">The graph type to apply the resolver to.</param>
    /// <param name="resolver">The federation resolver used to resolve the source representation.</param>
    public static void ResolveReference<TSourceType>(this ObjectGraphType<TSourceType> graphType, IFederationResolver resolver)
    {
        graphType.Metadata[RESOLVER_METADATA] = resolver;
    }
}
