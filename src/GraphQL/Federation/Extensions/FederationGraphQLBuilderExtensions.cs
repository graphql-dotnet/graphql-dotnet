using GraphQL.DI;
using GraphQL.Federation;
using GraphQL.Federation.Types;
using GraphQL.Federation.Visitors;

namespace GraphQL;

/// <summary>
/// Federation extensions for <see cref="IGraphQLBuilder"/>.
/// </summary>
public static class FederationGraphQLBuilderExtensions
{
    /// <summary>
    /// Registers Federation types, directives and, optionally, Query fields.
    /// </summary>
    public static IGraphQLBuilder AddFederation(
        this IGraphQLBuilder builder,
        Action<FederationSettings>? configure = null)
    {
        var settings = new FederationSettings();
        configure?.Invoke(settings);

        if (settings.Version.StartsWith("1.") && settings.SdlPrintOptions == null)
            settings.SdlPrintOptions = new() { IncludeFederationTypes = false };

        // todo: ensure all directives are supported by all supported versions
        var directives = settings.ImportDirectives ?? FederationDirectiveEnum.All;

        builder.Services
            .Register(new ServiceGraphType(settings.SdlPrintOptions))
            .Register<AnyScalarGraphType>(ServiceLifetime.Singleton)
            .Register<EntityGraphType>(ServiceLifetime.Transient)
            .Register<LinkPurposeGraphType>(ServiceLifetime.Singleton)
            .Register<LinkImportGraphType>(ServiceLifetime.Singleton)
            .Register<FieldSetGraphType>(ServiceLifetime.Singleton)
            .Register<FederationEntitiesSchemaNodeVisitor>(ServiceLifetime.Transient)
            .Register<FederationServiceSchemaNodeVisitor>(ServiceLifetime.Transient);

        return builder
            .ConfigureSchema((schema, _) =>
            {
                // add the federation directives to the schema
                schema.AddFederationDirectives(settings);

                // add the @link directive to the schema, referencing the directive specified by the import parameter
                schema.ApplyLinkDirective(settings);

                // register Federation types
                schema.RegisterType<ServiceGraphType>();
                schema.RegisterType<AnyScalarGraphType>();
                schema.RegisterType<EntityGraphType>();

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
