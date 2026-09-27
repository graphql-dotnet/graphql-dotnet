# Samples

The repository includes runnable samples that demonstrate common GraphQL.NET configurations. They are useful starting points when you want to compare schema styles, dependency injection setup, serialization, or server integration without creating a new project from scratch.

## Schema-first

The `samples/GraphQL.SchemaFirst.Sample` project demonstrates a non-federation schema-first application. It keeps the GraphQL schema in `Schema.gql`, builds an `ISchema` with `SchemaBuilder`, registers CLR resolver methods with `schemaBuilder.Types.Include<Query>()`, and executes a small query from a console app.

Run it with:

```bash
dotnet run --project samples/GraphQL.SchemaFirst.Sample/GraphQL.SchemaFirst.Sample.csproj
```

Schema-first works well when the SDL is the source of truth, when a team wants to design the contract before implementing resolvers, or when an existing schema needs to be hosted by GraphQL.NET. Resolver methods still live in CLR types, so you can continue to use dependency injection with attributes such as `[FromServices]`.

Code-first or type-first can be easier when most of the schema should follow the shape of existing CLR models, when you want stronger compile-time feedback for schema changes, or when you rely heavily on GraphQL.NET graph type classes.

## Other sample projects

- `samples/GraphQL.AotCompilationSample.CodeFirst` and `samples/GraphQL.AotCompilationSample.TypeFirst` show AOT-friendly schema configuration.
- `samples/GraphQL.DataLoader.Sample.Default` and `samples/GraphQL.DataLoader.Sample.DI` show DataLoader usage with and without dependency injection.
- `samples/GraphQL.Federation.SchemaFirst.Sample1` and `samples/GraphQL.Federation.SchemaFirst.Sample2` show schema-first federation scenarios.
- `samples/GraphQL.Federation.CodeFirst.Sample3` and `samples/GraphQL.Federation.TypeFirst.Sample4` show federation with code-first and type-first schemas.
- `samples/GraphQL.Harness` is a runnable ASP.NET Core harness for experimenting with requests.
- `src/GraphQL.StarWars` and `src/GraphQL.StarWars.TypeFirst` provide the Star Wars example schemas used by tests and samples.
