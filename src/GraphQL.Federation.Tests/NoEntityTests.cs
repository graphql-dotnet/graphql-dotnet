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
            .AddFederation("2.3"));
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        var sdl = schema.Print(new() { IncludeImportedDefinitions = false });
        sdl.ShouldMatchApproved(c => c.NoDiff());
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
