using GraphQL;
using GraphQL.SchemaFirstDemo;
using GraphQL.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IBookRepository, BookRepository>();

builder.Services.AddGraphQL(b => b
    .AddSchema<BookSchema>()
    .AddSystemTextJson()
    .AddErrorInfoProvider(opt => opt.ExposeExceptionDetails = builder.Environment.IsDevelopment())
);

var app = builder.Build();

app.UseGraphQL<BookSchema>("/graphql");
app.UseGraphQLPlayground("/ui/playground");

await app.RunAsync();
