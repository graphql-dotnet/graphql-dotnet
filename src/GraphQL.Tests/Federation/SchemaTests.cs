using GraphQL.Federation;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Federation;

public class SchemaTests
{
    [Theory]
    [InlineData("1.0")]
    [InlineData("2.0")]
    public void FederationSchemaFirst(string version)
    {
        var sdlInput = """
            type Query

            type Post @key(fields: "id") {
              title: String
            }
            """;
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGraphQL(x => x
            .AddSchema(provider => Schema.For(sdlInput, c => c.ServiceProvider = provider))
            .AddFederation(version));
        var schema = serviceCollection.BuildServiceProvider().GetRequiredService<ISchema>();
        schema.Initialize();
        var sdl = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase });
        sdl.ShouldMatchApproved(c => c.NoDiff().WithDiscriminator(version));
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("2.0")]
    [InlineData("2.1")]
    [InlineData("2.2")]
    [InlineData("2.3")]
    [InlineData("2.4")]
    [InlineData("2.5")]
    [InlineData("2.6")]
    [InlineData("2.7")]
    [InlineData("2.8")]
    public void FederationCodeFirst(string version)
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        var postType = new ObjectGraphType { Name = "Post" };
        postType.Field<StringGraphType>("title");
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGraphQL(x => x
            .AddSchema(provider => new Schema(provider) { Query = queryType })
            .AddFederation(version)
            .ConfigureSchema(s => s.RegisterType(postType)));
        var schema = serviceCollection.BuildServiceProvider().GetRequiredService<ISchema>();
        schema.Initialize();
        var sdl = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase });
        sdl.ShouldMatchApproved(c => c.NoDiff().WithDiscriminator(version));
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("2.0")]
    public void FederationTypeFirst(string version)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGraphQL(x => x
            .AddAutoSchema<Query>()
            .AddFederation(version));
        var schema = serviceCollection.BuildServiceProvider().GetRequiredService<ISchema>();
        schema.Initialize();
        var sdl = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase });
        sdl.ShouldMatchApproved(c => c.NoDiff().WithDiscriminator(version));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ResolvableEntitiesIdentifiesAliasedKey(bool includeImportedTypes)
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        var postType = new ObjectGraphType { Name = "Post" };
        postType.Field<StringGraphType>("id");
        postType.Key("id");
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGraphQL(x => x
            .AddSchema(provider => new Schema(provider) { Query = queryType })
            .AddFederation("2.0", c => c.Imports.Remove("@key"))
            .ConfigureSchema(s => s.RegisterType(postType)));
        var schema = serviceCollection.BuildServiceProvider().GetRequiredService<ISchema>();
        schema.Initialize();
        var sdl = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase, IncludeImportedDefinitions = includeImportedTypes });
        sdl.ShouldMatchApproved(c => c.NoDiff().WithDiscriminator(includeImportedTypes ? "WithImported" : "NoImported"));
    }

    [Theory]
    [InlineData("2.0", false)]
    [InlineData("2.3", true)]
    public void SchemaFailsValidationWithUnsupportedDirectives(string version, bool shouldSucceed)
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        var postType = new ObjectGraphType { Name = "Post" };
        postType.Field<StringGraphType>("id");
        postType.ApplyDirective("federation__interfaceObject");
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGraphQL(x => x
            .AddSchema(provider => new Schema(provider) { Query = queryType })
            .AddFederation(version)
            .ConfigureSchema(s => s.RegisterType(postType)));
        var schema = serviceCollection.BuildServiceProvider().GetRequiredService<ISchema>();
        if (shouldSucceed)
        {
            schema.Initialize();
        }
        else
        {
            Should.Throw<InvalidOperationException>(schema.Initialize)
                .Message.ShouldBe("Unknown directive 'federation__interfaceObject' applied to object 'Post'.");
        }
    }

    [Fact]
    public void DoesNotCreateRepresentationsWhenNoResolvableTypes()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<Query2>()
            .AddFederation("2.3"));
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        var sdl = schema.Print(new() { IncludeImportedDefinitions = false });
        sdl.ShouldMatchApproved(c => c.NoDiff());
    }

    private class Query
    {
        public static Post GetPost() => new Post();
    }

    private class Post
    {
        public string? Title { get; set; }
    }

    [Name("MyQuery")]
    private class Query2
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

