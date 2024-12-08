using GraphQL;
using GraphQL.DataLoader.Sample.DI;
using GraphQL.DataLoader.Sample.DI.GraphQL;
using GraphQL.Execution;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<SalespeopleByNameDataLoader>().AddScoped<CarsBySalespersonDataLoader>();
builder.Services.AddDbContext<DealershipDbContext>();

builder.Services.AddGraphQL(b => b
    .AddSystemTextJson()
    .AddGraphTypes(typeof(CarsGraphType).Assembly)
    .AddSchema<DealershipSchema>()
    .AddExecutionStrategy<SerialExecutionStrategy>(GraphQLParser.AST.OperationType.Query)
);

var app = builder.Build();

//This GraphQl Middleware is a polyfill to Operate without Dependencies on the Seperate Server Repository
// For Setting up a real GraphQl WebApi, be sure to have a look at: https://github.com/graphql-dotnet/server
app.UseGraphQL();
app.UseGraphQLAltair();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DealershipDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

await app.RunAsync();
