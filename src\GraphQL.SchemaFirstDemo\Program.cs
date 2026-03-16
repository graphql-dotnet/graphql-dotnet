using GraphQL;
using GraphQL.SchemaFirstDemo;
using GraphQL.Types;

var builder = WebApplication.CreateBuilder(args);

// Register application services.
builder.Services.AddSingleton<IBookRepository, BookRepository>();

// Register resolver classes so they can be resolved from DI by SchemaBuilder.
builder.Services.AddSingleton<QueryResolvers>();
builder.Services.AddSingleton<MutationResolvers>();
builder.Services.AddSingleton<BookResolvers>();

// Register GraphQL.NET with Schema-First schema.
builder.Services.AddGraphQL(b => b
    .AddSchema<BookSchema>()
    .AddSystemTextJson()
    .AddErrorInfoProvider(opt => opt.ExposeExceptionDetails = builder.Environment.IsDevelopment())
);

var app = builder.Build();

// Mount the GraphQL endpoint.
app.UseGraphQL<BookSchema>("/graphql");

// Mount the Playground UI for interactive exploration.
app.UseGraphQLPlayground("/ui/playground");

await app.RunAsync();
