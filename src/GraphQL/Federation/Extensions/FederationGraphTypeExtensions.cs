using GraphQL.Builders;
using GraphQL.DataLoader;
using GraphQL.Federation.Types;
using GraphQL.Types;
using GraphQL.Utilities;
using static GraphQL.Federation.Extensions.FederationHelper;

namespace GraphQL.Federation.Extensions;

/// <summary>
/// Federation extensions for Graph Types.
/// </summary>
public static class FederationGraphTypeExtensions
{
    /// <summary>
    /// Adds "_service" field. Intended to use on Query in conjunction with .AddFederation(..., addFields = false).
    /// </summary>
    public static FieldBuilder<T, object> AddServices<T>(this ComplexGraphType<T> graphType) =>
        graphType.Field<NonNullGraphType<ServiceGraphType>>("_service")
            .Resolve(context => new { });

    /// <summary>
    /// Adds "_entities" field. Intended to use on Query in conjunction with .AddFederation(..., addFields = false).
    /// </summary>
    public static FieldBuilder<T, object> AddEntities<T>(this ComplexGraphType<T> graphType)
    {
        return graphType.Field<NonNullGraphType<ListGraphType<EntityType>>>("_entities")
            .Argument<NonNullGraphType<ListGraphType<NonNullGraphType<Utilities.Federation.AnyScalarGraphType>>>>("representations")
            .Resolve(FederationQuerySchemaNodeVisitor.ResolveEntities);
    }

    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, TSourceType> resolver) =>
        graphType.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, Task<TSourceType>> resolver) =>
        graphType.Metadata[RESOLVER_METADATA] = new AsyncFederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this ObjectGraphType<TSourceType> graphType, Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TSourceType>> resolver) =>
        graphType.Metadata[RESOLVER_METADATA] = new DataLoaderFederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this TypeConfig typeConfig, Func<IResolveFieldContext, TSourceType, TSourceType> resolver) =>
        typeConfig.Metadata[RESOLVER_METADATA] = new FederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this TypeConfig typeConfig, Func<IResolveFieldContext, TSourceType, Task<TSourceType>> resolver) =>
        typeConfig.Metadata[RESOLVER_METADATA] = new AsyncFederationResolver<TSourceType>(resolver);

    /// <summary>
    /// Specifies reference resolver for the type.
    /// </summary>
    public static void ResolveReference<TSourceType>(this TypeConfig typeConfig, Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TSourceType>> resolver) =>
        typeConfig.Metadata[RESOLVER_METADATA] = new DataLoaderFederationResolver<TSourceType>(resolver);
}
