using GraphQL;
using GraphQL.StarWars.SchemaFirst;
using GraphQL.StarWars.SchemaFirst.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<StarWarsData>();
builder.Services.AddSingleton<StarWarsQuery>();
builder.Services.AddSingleton<StarWarsMutation>();
builder.Services.AddGraphQL(graphql => graphql
    .AddSystemTextJson()
    .AddSchema(StarWarsSchema.Create)
    .ValidateServices());

var app = builder.Build();

app.UseGraphQL();
app.UseGraphQLGraphiQL();

await app.RunAsync().ConfigureAwait(false);

public partial class Program
{
}
