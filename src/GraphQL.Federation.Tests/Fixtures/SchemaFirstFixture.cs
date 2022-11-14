using GraphQL.DataLoader;
using GraphQL.Federation.Enums;
using GraphQL.Federation.Extensions;
using GraphQL.Federation.Tests.Schema.External;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Federation.Tests.Fixtures;

public class SchemaFirstFixture
{
    public readonly IServiceProvider Services;
    public readonly GraphQL.Types.Schema Schema;

    public SchemaFirstFixture()
    {
        var sc = new ServiceCollection();

        sc.AddGraphQL(builder => builder
            .AddSystemTextJson()
            .AddDataLoader()
            .AddFederation(FederationDirectiveEnum.Key
                | FederationDirectiveEnum.Shareable
                | FederationDirectiveEnum.Inaccessible
                | FederationDirectiveEnum.Override
                | FederationDirectiveEnum.External
                | FederationDirectiveEnum.Provides
                | FederationDirectiveEnum.Requires,
                addFields: true,
                schemaPrinterOptions: new() { IncludeDeprecationReasons = true }));

        Services = sc.BuildServiceProvider();

        Schema = FederatedSchemaHelper.For(@"
            type Query {
                _noop: String
            }
            type SchemaFirstExternalResolvableTestDto @key(fields: ""id"") {
                id: Int!
                external: String @external
                extended: String! @requires(fields: ""external"")
            }
            type SchemaFirstExternalTestDto @key(fields: ""id"", resolvable: false) {
                id: Int!
            }
            type SchemaFirstFederatedTestDto @key(fields: ""id"") {
                id: Int!
                name: String
                externalTestId: Int!
                externalResolvableTestId: Int!
                externalTest: SchemaFirstExternalTestDto! @deprecated(reason: ""Test deprecation reason 04."")
                externalResolvableTest: SchemaFirstExternalResolvableTestDto! @provides(fields: ""external"")
            }",
            builder =>
            {
                builder.ServiceProvider = Services;
                builder.Types.Include<SchemaFirstExternalResolvableTestDto>();
                builder.Types.Include<SchemaFirstExternalTestDto>();
                builder.Types.Include<SchemaFirstFederatedTestDto>();

                builder.Types.For(nameof(SchemaFirstExternalResolvableTestDto))
                   .IsTypeOf<SchemaFirstExternalResolvableTestDto>();
                builder.Types.For(nameof(SchemaFirstFederatedTestDto))
                   .IsTypeOf<SchemaFirstFederatedTestDto>();

                builder.Types.For(nameof(SchemaFirstExternalResolvableTestDto))
                    .ResolveReference<SchemaFirstExternalResolvableTestDto>((ctx, rep) => rep);
                builder.Types.For(nameof(SchemaFirstFederatedTestDto))
                    .ResolveReference<SchemaFirstFederatedTestDto>((ctx, rep) =>
                    {
                        var accessor = ctx.RequestServices.GetRequiredService<IDataLoaderContextAccessor>();
                        return accessor.Context.GetOrAddBatchLoader<int, SchemaFirstFederatedTestDto>(
                            "SchemaFirstFederatedTestDto.ResolveReference",
                            (items) =>
                            {
                                return Task.FromResult<IDictionary<int, SchemaFirstFederatedTestDto>>(new Dictionary<int, SchemaFirstFederatedTestDto>
                                {
                                    [1] = new() { Id = 1, Name = "111", ExternalTestId = 4, ExternalResolvableTestId = 7 },
                                    [2] = new() { Id = 2, Name = "222", ExternalTestId = 5, ExternalResolvableTestId = 8 },
                                    [3] = new() { Id = 3, Name = "333", ExternalTestId = 6, ExternalResolvableTestId = 9 },
                                });
                            }).LoadAsync(rep.Id);
                    });
            });
    }
}
