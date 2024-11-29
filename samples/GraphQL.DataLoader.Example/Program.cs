using DataLoaderGql;
using DataLoaderGql.GraphQl;
using GraphQL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<SalespeopleByNameDataLoader>().AddScoped<CarsBySalespersonDataLoader>();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddDbContext<DealershipDbContext>();

builder.Services.AddGraphQL(builder =>
{
    builder
        .AddSystemTextJson()
        .AddGraphTypes(typeof(CarsGraphType).Assembly)
        .AddSchema<DealershipSchema>();
});
var app = builder.Build();

app.UseGraphQL();
app.UseGraphQLAltair();
app.MapGraphQLAltair().AllowAnonymous();
await app
    .Services
    .CreateAsyncScope()
    .ServiceProvider
    .GetRequiredService<DealershipDbContext>()
    .Database
    .MigrateAsync()
    .ConfigureAwait(false);

await app
    .Services
    .CreateAsyncScope()
    .ServiceProvider
    .GetRequiredService<DealershipDbContext>()
    .Database
    //For making sure seed Data gets applied
    .EnsureCreatedAsync()
    .ConfigureAwait(false);
app.Run();
