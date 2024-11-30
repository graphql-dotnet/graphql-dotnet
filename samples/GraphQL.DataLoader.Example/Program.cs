using DataLoaderGql;
using DataLoaderGql.GraphQl;
using Example;
using GraphQL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<SalespeopleByNameDataLoader>().AddScoped<CarsBySalespersonDataLoader>();
builder.Services.AddDbContext<DealershipDbContext>();
builder.Services.Configure<GraphQlSettings>(builder.Configuration);
builder.Services.Configure<GraphQlSettings>(settings
    => settings.BuildUserContext = ctx
        => new GraphQlUserContext { User = ctx.User });

builder.Services.AddGraphQL(b => b
        .AddSystemTextJson()
        .AddGraphTypes(typeof(CarsGraphType).Assembly)
        .AddSchema<DealershipSchema>()
);
builder.Services.AddSingleton<GraphQlMiddleware>();

var app = builder.Build();

//This GraphQl Middleware is a polyfill to Operate without Dependencies on the Seperate Server Repository
// For Setting up a real GraphQl WebApi, be sure to have a look at: https://github.com/graphql-dotnet/server
app.UseMiddleware<GraphQlMiddleware>();
app.UseGraphQLAltair();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DealershipDbContext>();
    await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
}


await app.RunAsync().ConfigureAwait(false);
