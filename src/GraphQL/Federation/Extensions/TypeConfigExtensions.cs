using GraphQL.DataLoader;
using GraphQL.Federation.Resolvers;
using GraphQL.Utilities;

namespace GraphQL.Federation;

/// <summary>
/// Provides extension methods for configuring type resolution in a GraphQL federation setup.
/// </summary>
public static class TypeConfigExtensions
{
    /// <summary>
    /// Configures synchronous resolution of a reference using a resolver function.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <param name="typeConfig">The type configuration to apply the resolver to.</param>
    /// <param name="resolver">The function used to resolve the source representation.</param>
    [RequiresUnreferencedCode("This uses reflection at runtime to deserialize the representation.")]
    public static void ResolveReference<TSourceType>(this TypeConfig typeConfig, Func<IResolveFieldContext, TSourceType, TSourceType?> resolver) =>
        typeConfig.Metadata[FederationHelper.RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Configures asynchronous resolution of a reference using a task-based resolver function.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <param name="typeConfig">The type configuration to apply the resolver to.</param>
    /// <param name="resolver">The asynchronous function used to resolve the source representation.</param>
    [RequiresUnreferencedCode("This uses reflection at runtime to deserialize the representation.")]
    public static void ResolveReference<TSourceType>(this TypeConfig typeConfig, Func<IResolveFieldContext, TSourceType, Task<TSourceType?>> resolver) =>
        typeConfig.Metadata[FederationHelper.RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Configures resolution of a reference using a data loader-based resolver function.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <param name="typeConfig">The type configuration to apply the resolver to.</param>
    /// <param name="resolver">The data loader function used to resolve the source representation.</param>
    [RequiresUnreferencedCode("This uses reflection at runtime to deserialize the representation.")]
    public static void ResolveReference<TSourceType>(this TypeConfig typeConfig, Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TSourceType?>> resolver) =>
        typeConfig.Metadata[FederationHelper.RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Configures asynchronous resolution of a reference using a task-based resolver function.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <typeparam name="TReturnType">The CLR type of the resolved object returned by the resolver.</typeparam>
    /// <param name="config">The type configuration to apply the resolver to.</param>
    /// <param name="resolver">The asynchronous function used to resolve the source representation.</param>
    [RequiresUnreferencedCode("This uses reflection at runtime to deserialize the representation.")]
    public static void ResolveReference<TSourceType, TReturnType>(this TypeConfig config, Func<IResolveFieldContext, TSourceType, Task<TReturnType?>> resolver)
    {
        config.Metadata[FederationHelper.RESOLVER_METADATA] = new FederationResolver<TSourceType, TReturnType>(resolver);
    }

    /// <summary>
    /// Configures asynchronous resolution of a reference using a data loader-based resolver function.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <typeparam name="TReturnType">The CLR type of the resolved object returned by the resolver.</typeparam>
    /// <param name="config">The type configuration to apply the resolver to.</param>
    /// <param name="resolver">The data loader function used to resolve the source representation.</param>
    [RequiresUnreferencedCode("This uses reflection at runtime to deserialize the representation.")]
    public static void ResolveReference<TSourceType, TReturnType>(this TypeConfig config, Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TReturnType?>> resolver)
    {
        config.Metadata[FederationHelper.RESOLVER_METADATA] = new FederationResolver<TSourceType, TReturnType>(resolver);
    }

    /// <summary>
    /// Configures synchronous resolution of a reference using a resolver function.
    /// </summary>
    /// <typeparam name="TSourceType">The CLR type of the source representation that the resolver handles.</typeparam>
    /// <typeparam name="TReturnType">The CLR type of the resolved object returned by the resolver.</typeparam>
    /// <param name="config">The type configuration to apply the resolver to.</param>
    /// <param name="resolver">The function used to resolve the source representation.</param>
    [RequiresUnreferencedCode("This uses reflection at runtime to deserialize the representation.")]
    public static void ResolveReference<TSourceType, TReturnType>(this TypeConfig config, Func<IResolveFieldContext, TSourceType, TReturnType?> resolver)
    {
        config.Metadata[FederationHelper.RESOLVER_METADATA] = new FederationResolver<TSourceType, TReturnType>(resolver);
    }

    /// <summary>
    /// Configures resolution of a reference using a specified federation resolver.
    /// </summary>
    /// <param name="config">The type configuration to apply the resolver to.</param>
    /// <param name="resolver">The federation resolver used to resolve the source representation.</param>
    public static void ResolveReference(this TypeConfig config, IFederationResolver resolver)
    {
        config.Metadata[FederationHelper.RESOLVER_METADATA] = resolver;
    }
}
