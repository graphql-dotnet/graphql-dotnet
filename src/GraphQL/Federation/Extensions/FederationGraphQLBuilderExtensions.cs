using GraphQL.DI;
using GraphQL.Federation.Types;
using GraphQL.Federation.Visitors;
using GraphQL.Utilities;

namespace GraphQL.Federation;

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
    /// <param name="printOptions"> <see cref="PrintOptions"/> used to print _services { sdl }. </param>
    public static IGraphQLBuilder AddFederation(
        this IGraphQLBuilder builder,
        FederationDirectiveEnum import = FederationDirectiveEnum.All,
        PrintOptions? printOptions = null)
    {
        builder.Services
            .Register(new ServiceGraphType(printOptions))
            .Register<AnyScalarGraphType>(ServiceLifetime.Singleton)
            .Register<EntityType>(ServiceLifetime.Transient)
            .Register<LinkPurposeGraphType>(ServiceLifetime.Singleton)
            .Register<LinkImportGraphType>(ServiceLifetime.Singleton)
            .Register<FieldSetGraphType>(ServiceLifetime.Singleton)
            .Register<FederationEntitiesSchemaNodeVisitor>(ServiceLifetime.Transient)
            .Register<FederationServiceSchemaNodeVisitor>(ServiceLifetime.Transient);

        return builder
            .ConfigureSchema((schema, _) =>
            {
                schema.AddFederationDirectives(import);
                // add the @link directive to the schema, referencing the directive specified by the import parameter
                schema.ApplyLinkDirective(import);
                // register Federation types
                schema.RegisterType<ServiceGraphType>();
                schema.RegisterType<AnyScalarGraphType>();
                schema.RegisterType<EntityType>();
                // add the _service field to the query type
                // this cannot be done here because the schema.Query property will not yet be set
                schema.RegisterVisitor<FederationServiceSchemaNodeVisitor>();
                // after schema initialization, configure the _Entity union type with the proper types,
                // and add the _entities field to the query type
                // this cannot be done here because the schema.Query property will not yet be set, and the types are not yet known
                schema.RegisterVisitor<FederationEntitiesSchemaNodeVisitor>();
            });
    }
}
