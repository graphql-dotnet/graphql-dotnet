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
    /// <param name="printOptions"> <see cref="PrintOptions"/> used to print _services { sdl }. </param>
    public static IGraphQLBuilder AddFederation(
        this IGraphQLBuilder builder,
        FederationDirectiveEnum import = FederationDirectiveEnum.All,
        bool addFields = true,
        PrintOptions? printOptions = null)
    {
        builder.Services
            .Register(new ServiceGraphType(printOptions))
            .Register<Utilities.Federation.AnyScalarGraphType>(ServiceLifetime.Singleton)
            .Register<EntityType>(ServiceLifetime.Transient)
            .Register<LinkPurposeGraphType>(ServiceLifetime.Singleton)
            .Register<LinkImportGraphType>(ServiceLifetime.Singleton)
            .Register<FieldSetGraphType>(ServiceLifetime.Singleton)
            .Register<FederationEntitiesSchemaNodeVisitor>(ServiceLifetime.Transient)
            .Register<FederationQuerySchemaNodeVisitor>(ServiceLifetime.Transient);

        return builder
            .ConfigureSchema((schema, _) =>
            {
                schema.AddFederationDirectives(import);
                // add the @link directive to the schema, referencing the directive specified by the import parameter
                schema.BuildLinkExtension(import);
                // register Federation types
                schema.RegisterType<ServiceGraphType>();
                schema.RegisterType<Utilities.Federation.AnyScalarGraphType>();
                schema.RegisterType<EntityType>();
                // after schema initialization, configure the _Entity union type with the proper types
                schema.RegisterVisitor<FederationEntitiesSchemaNodeVisitor>();
                if (addFields)
                {
                    // add the _service and _entities fields to the query type
                    // this cannot be done here because the schema.Query property will not yet be set
                    schema.RegisterVisitor<FederationQuerySchemaNodeVisitor>();
                }
            });
    }
}
