using GraphQL.Builders;
using GraphQL.DataLoader;
using GraphQL.Federation.Types;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;
using static GraphQL.Federation.Extensions.FederationHelper;

namespace GraphQL.Federation.Extensions;

/// <summary>
/// Federation extensions for Graph Types.
/// </summary>
public static partial class FederationGraphTypeExtensions
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
    /// Adds "@key" directive.
    /// </summary>
    public static void Key(this IObjectGraphType graphType, string[] fields, bool resolvable = true) =>
        graphType.Key(string.Join(" ", fields.Select(x => x.ToCamelCase())), resolvable);

    /// <summary>
    /// Adds "@key" directive.
    /// </summary>
    public static void Key(this IObjectGraphType graphType, string fields, bool resolvable = true)
    {
        var astMetadata = graphType.BuildAstMetadata();
        var directive = new GraphQLDirective(new(KEY_DIRECTIVE));
        directive.AddFieldsArgument(fields.ToCamelCase());
        directive.AddResolvableArgument(resolvable);
        astMetadata!.Directives!.Items.Add(directive);
    }

    /// <summary>
    /// Adds "@shareable" directive.
    /// </summary>
    public static void Shareable(this IGraphType graphType)
    {
        if (graphType.IsInputType())
            throw new ArgumentOutOfRangeException(nameof(graphType), graphType, "Input types are not supported.");
        var astMetadata = graphType.BuildAstMetadata();
        var directive = new GraphQLDirective(new(SHAREABLE_DIRECTIVE));
        astMetadata!.Directives!.Items.Add(directive);
    }

    /// <summary>
    /// Adds "@inaccessible" directive.
    /// </summary>
    public static void Inaccessible(this IGraphType graphType)
    {
        if (graphType.IsInputType())
            throw new ArgumentOutOfRangeException(nameof(graphType), graphType, "Input types are not supported.");
        var astMetadata = graphType.BuildAstMetadata();
        var directive = new GraphQLDirective(new(INACCESSIBLE_DIRECTIVE));
        astMetadata!.Directives!.Items.Add(directive);
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
