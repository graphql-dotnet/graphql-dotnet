using GraphQL;
using GraphQL.DataLoader.Di.Sample;
using GraphQL.DataLoader.Di.Sample.GraphQl;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DealershipDbContext>();

builder.Services.AddGraphQL(b => b
    .AddSystemTextJson()
    .AddGraphTypes(typeof(CarsGraphType).Assembly)
    .AddSchema<DealershipSchema>()
    .AddDataLoader()
);

var app = builder.Build();

//This GraphQl Middleware is a polyfill to Operate without Dependencies on the Seperate Server Repository
// For Setting up a real GraphQl WebApi, be sure to have a look at: https://github.com/graphql-dotnet/server
app.UseGraphQL();
app.UseGraphQLAltair();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DealershipDbContext>();
    await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);
