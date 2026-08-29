using GraphQL;
using GraphQL.SchemaFirstDemo;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IBookRepository, BookRepository>();

// Resolver classes must be registered in DI so SchemaBuilder can resolve them
// and inject their constructor dependencies.
builder.Services.AddSingleton<QueryResolvers>();
builder.Services.AddSingleton<MutationResolvers>();
builder.Services.AddSingleton<BookResolvers>();

// ---------------------------------------------------------------------------
// GraphQL.NET
// ---------------------------------------------------------------------------
builder.Services.AddGraphQL(b => b
    .AddSchema<BookSchema>()
    .AddSystemTextJson()
    .AddErrorInfoProvider(opt =>
        opt.ExposeExceptionDetails = builder.Environment.IsDevelopment()));

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware
// ---------------------------------------------------------------------------
app.UseGraphQL<BookSchema>("/graphql");
app.UseGraphQLPlayground("/ui/playground");

await app.RunAsync();
