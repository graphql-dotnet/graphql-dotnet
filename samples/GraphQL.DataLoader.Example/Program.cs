using DataLoaderGql;
using DataLoaderGql.GraphQl;
using GraphQL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<SalespeopleByNameDataLoader>().AddScoped<CarsBySalespersonDataLoader>();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddDbContext<DealershipDbContext>();

builder.Services.AddGraphQL(b => b
        .AddSystemTextJson()
        .AddGraphTypes(typeof(CarsGraphType).Assembly)
        .AddSchema<DealershipSchema>()
);
var app = builder.Build();

app.UseGraphQL();
app.UseGraphQLAltair();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DealershipDbContext>();
    await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
}


await app.RunAsync().ConfigureAwait(false);
