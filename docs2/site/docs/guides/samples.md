# Samples

The repository includes runnable samples that demonstrate common GraphQL.NET application styles. Each sample is under the `samples` directory and references the local source projects, so you can run them while developing changes in this repository.

## Schema-first

`samples/GraphQL.SchemaFirst.Sample` demonstrates the schema-first approach requested by issue #1025. It embeds the GraphQL schema language file as `Schema.gql`, registers CLR resolver classes with `Schema.For`, and executes both a query and a mutation from a console application.

Run it with:

```bash
dotnet run --project samples/GraphQL.SchemaFirst.Sample/GraphQL.SchemaFirst.Sample.csproj
```

Use this sample when you want to see how schema fields map to resolver methods with `GraphQLMetadata`, how GraphQL input objects map to CLR classes, and how a schema-first app can be smoke-tested without hosting ASP.NET Core.

## AOT compilation samples

`samples/GraphQL.AotCompilationSample.CodeFirst` and `samples/GraphQL.AotCompilationSample.TypeFirst` show how to prepare code-first and type-first schemas for ahead-of-time compilation. These samples are useful when trimming and native publishing are part of the deployment target.

## DataLoader samples

`samples/GraphQL.DataLoader.Sample.Default` and `samples/GraphQL.DataLoader.Sample.DI` show two DataLoader integration styles. Use them when you need batched data access and want to compare default registration with dependency-injection-driven graph types.

## Federation samples

The federation samples cover code-first, schema-first, and type-first configurations. `samples/GraphQL.Federation.SchemaFirst.Sample1` and `samples/GraphQL.Federation.SchemaFirst.Sample2` are useful references when you need schema-first SDL plus Apollo Federation directives and resolvers.

## Harness sample

`samples/GraphQL.Harness` is a broader ASP.NET Core sample that exercises middleware, serialization, and transport behavior. It is a useful manual testing target when changing request execution behavior.
