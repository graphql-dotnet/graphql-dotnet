using GraphQL.DI;
using GraphQL.Federation;
using GraphQL.Federation.Types;
using GraphQL.Federation.Visitors;
using GraphQL.Utilities;

namespace GraphQL;

/// <summary>
/// Federation extensions for <see cref="IGraphQLBuilder"/>.
/// </summary>
public static class FederationGraphQLBuilderExtensions
{
    /// <summary>
    /// Registers Federation types and directives for a specified federation version, and modifies
    /// the Query operation type definition to include the required `_entities` and `_service` fields.
    /// </summary>
    /// <param name="builder">The GraphQL builder.</param>
    /// <param name="version">The version of the federation specification to use. Specify in major.minor format, such as "1.0" or "2.3".</param>
    /// <param name="configureLinkDirective">Optional configuration for the @link directive.</param>
    public static IGraphQLBuilder AddFederation(
        this IGraphQLBuilder builder,
        string version,
        Action<LinkConfiguration>? configureLinkDirective = null)
    {
        if (!FederationHelper.TryParseVersion(version, out var parsedVersion))
            throw new ArgumentOutOfRangeException(nameof(version), version, "Invalid federation version.");
        if (parsedVersion.Major == 1 && configureLinkDirective != null)
            throw new ArgumentException("The @link directive is not supported in federation version 1.x.", nameof(configureLinkDirective));

        builder.Services
            .Configure<FederationPrintOptions>(o => o.IncludeFederationTypes = !version.StartsWith("1."))
            .Register<ServiceGraphType>(ServiceLifetime.Transient)
            .Register<AnyScalarGraphType>(ServiceLifetime.Singleton)
            .Register<EntityGraphType>(ServiceLifetime.Transient)
            .Register<FederationEntitiesSchemaNodeVisitor>(ServiceLifetime.Transient)
            .Register<FederationServiceSchemaNodeVisitor>(ServiceLifetime.Transient);

        return builder
            .ConfigureSchema((schema, _) =>
            {
                if (parsedVersion.Major != 1)
                {
                    // add the @link directive to the schema, referencing the directive specified by the import parameter
                    schema.AddFederationLink(version, configureLinkDirective);
                }

                // add the federation directives to the schema
                // (examines the @link directive to ensure the directives are added with the correct aliases)
                schema.AddFederationTypesAndDirectives(version);

                // register Federation types so they are available for use by the schema node visitors later on
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
