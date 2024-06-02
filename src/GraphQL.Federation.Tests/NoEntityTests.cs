using GraphQL.Federation.Attributes;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Federation.Tests;

public class NoEntityTests
{
    [Fact]
    public void DoesNotCreateRepresentationsWhenNoResolvableTypes()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<MyQuery>()
            .AddFederation());
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        schema.Print(new() { IncludeFederationTypes = false }).ShouldBe("""
            schema {
              query: MyQuery
            }

            type MyQuery {
              favoriteProduct: Product!
            }

            type Product @key(fields: "id", resolvable: false) {
              id: ID!
            }
            """, StringCompareShould.IgnoreLineEndings);
    }

    private class MyQuery
    {
        public static Product FavoriteProduct => new Product { Id = 1 };
    }

    [Key("id", Resolvable = false)]
    private class Product
    {
        [Id]
        public int Id { get; set; }
    }
}
