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

    private class Query
    {
        public static Post GetPost() => new Post();
    }

    private class Post
    {
        public string? Title { get; set; }
    }
}
