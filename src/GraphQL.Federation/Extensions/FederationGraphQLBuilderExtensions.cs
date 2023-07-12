using GraphQL.DI;
using GraphQL.Federation.Enums;
using GraphQL.Federation.Types;
using GraphQL.Utilities;

namespace GraphQL.Federation.Extensions;

/// <summary>
/// Federation extensions for <see cref="IGraphQLBuilder"/>.
/// </summary>
public static class FederationGraphQLBuilderExtensions
{
    /// <summary>
    /// Registers Federation types, directives and, optionally, Query fields.
    /// </summary>
    /// <param name="builder"> <see cref="IGraphQLBuilder"/> instance. </param>
    /// <param name="import"> Flags enum used to specify which Federation directives are used by subgraph. </param>
    /// <param name="addFields"> Pass false to skip adding Federation fields to Query (true by default). </param>
    /// <param name="schemaPrinterOptions"> SchemaPrinterOptions used to print _services { sdl }. </param>
    public static IGraphQLBuilder AddFederation(
        this IGraphQLBuilder builder,
        FederationDirectiveEnum import,
        bool addFields = true,
        SchemaPrinterOptions? schemaPrinterOptions = null)
    {
        builder.Services
            .Register(new ServiceGraphType(schemaPrinterOptions))
            .Register<Utilities.Federation.AnyScalarGraphType>(ServiceLifetime.Singleton)
            .Register<EntityType>(ServiceLifetime.Singleton)
            .Register<NeverType>(ServiceLifetime.Singleton)
            .Register<FederationEntitiesSchemaNodeVisitor>(ServiceLifetime.Singleton)
            .Register<FederationQuerySchemaNodeVisitor>(ServiceLifetime.Singleton);
        return builder
            .ConfigureSchema((schema, services) =>
            {
                schema.BuildLinkExtension(import);
                schema.RegisterType<ServiceGraphType>();
                schema.RegisterType<Utilities.Federation.AnyScalarGraphType>();
                schema.RegisterType<EntityType>();
                schema.RegisterType<NeverType>();
                schema.RegisterVisitor<FederationEntitiesSchemaNodeVisitor>();
                if (addFields)
                {
                    schema.RegisterVisitor<FederationQuerySchemaNodeVisitor>();
                }
            });
    }
}
