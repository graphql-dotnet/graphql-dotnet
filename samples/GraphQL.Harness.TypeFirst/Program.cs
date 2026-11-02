using GraphQL;
using GraphQL.StarWars.TypeFirst;
using GraphQL.StarWars.TypeFirst.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<StarWarsData>();
builder.Services.AddGraphQL(graphql => graphql
    .AddSystemTextJson()
    .AddAutoSchema<StarWarsQuery>(schema => schema.WithMutation<StarWarsMutation>())
    .ValidateServices());

var app = builder.Build();

app.UseGraphQL();
app.UseGraphQLGraphiQL();

await app.RunAsync().ConfigureAwait(false);

public partial class Program
{
}
