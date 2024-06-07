using GraphQL.DataLoader;
using GraphQL.Federation.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Federation;

/// <summary>
/// Federation extensions for Graph Types.
/// </summary>
public static class FederationGraphTypeExtensions
{
    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, TSourceType?> resolver) =>
        graphType.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, Task<TSourceType?>> resolver) =>
        graphType.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TSourceType?>> resolver) =>
        graphType.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this TypeConfig typeConfig, Func<IResolveFieldContext, TSourceType, TSourceType?> resolver) =>
        typeConfig.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this TypeConfig typeConfig, Func<IResolveFieldContext, TSourceType, Task<TSourceType?>> resolver) =>
        typeConfig.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this TypeConfig typeConfig, Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TSourceType?>> resolver) =>
        typeConfig.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);
}
