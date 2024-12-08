using GraphQL;
using GraphQL.DataLoader.Sample.Default;
using GraphQL.DataLoader.Sample.Default.GraphQL;
using GraphQL.Execution;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DealershipDbContext>();

builder.Services.AddGraphQL(b => b
    .AddSystemTextJson()
    .AddGraphTypes()
    .AddClrTypeMappings()
    .AddSchema<DealershipSchema>()
    .AddDataLoader()
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
