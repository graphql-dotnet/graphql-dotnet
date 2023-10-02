using GraphQL.DI;
using GraphQL.Utilities.Federation.Enums;
using GraphQL.Utilities.Federation.Types;
using GraphQL.Utilities.Federation.Visitors;
using GraphQL.Utilities;

namespace GraphQL.Utilities.Federation.Extensions
{
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
        /// <param name="federationVersion">The version of federation to use to build the schema</param>
        /// <param name="addFields"> Pass false to skip adding Federation fields to Query (true by default). </param>
        public static IGraphQLBuilder AddFederation(
            this IGraphQLBuilder builder,
            FederationDirectiveEnum import,
            string federationVersion = "2.2",
            bool addFields = true)
        {
            builder.Services
                .Register(new ServiceGraphType())
                .Register<AnyScalarGraphType>(ServiceLifetime.Singleton)
                .Register<EntityType>(ServiceLifetime.Singleton)
                .Register<NeverType>(ServiceLifetime.Singleton)
                .Register<FederationEntitiesSchemaNodeVisitor>(ServiceLifetime.Singleton)
                .Register<FederationQuerySchemaNodeVisitor>(ServiceLifetime.Singleton);
            return builder
                .ConfigureSchema((schema, services) =>
                {
                    schema.BuildLinkExtension(import, federationVersion);
                    schema.RegisterType<ServiceGraphType>();
                    schema.RegisterType<AnyScalarGraphType>();
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
}
