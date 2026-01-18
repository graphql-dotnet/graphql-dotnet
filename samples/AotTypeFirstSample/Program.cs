using AotSample;
using GraphQL;
using GraphQL.StarWars.TypeFirst;
using GraphQL.Types;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddSingleton<StarWarsData>();
builder.Services.AddGraphQLAot(b => b
    .AddSystemTextJsonAot()
    .AddSchema<SampleAotSchema>() // acts like AddSelfRegisteringSchema for any AOT types
    .ValidateServices()
);

var app = builder.Build();
app.Services.GetRequiredService<ISchema>().Initialize();
app.UseGraphQL("/graphql");
app.UseGraphQLGraphiQL("/");

app.Run();
