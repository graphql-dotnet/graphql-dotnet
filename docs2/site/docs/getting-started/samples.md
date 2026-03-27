# Samples

The GraphQL.NET repository includes a number of sample projects that demonstrate various features and usage patterns.
All samples can be found in the [`samples/`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples) directory.

## Schema-First Sample

[`samples/GraphQL.SchemaFirst.Sample`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.SchemaFirst.Sample)

Demonstrates the **Schema-First** (SDL-First) approach to building a GraphQL API with GraphQL.NET.
In the Schema-First approach, you write the schema in the [GraphQL Schema Definition Language (SDL)](https://graphql.org/learn/schema/#type-language)
and map it to C# resolver classes using `SchemaBuilder`.

Key features demonstrated:
- Defining the schema in a `.gql` SDL file embedded as a resource
- Using `SchemaBuilder` to map SDL types to C# classes
- Injecting services into resolvers via `[FromServices]`
- Setting up a minimal ASP.NET Core web application with GraphiQL

## GraphQL.Harness

[`samples/GraphQL.Harness`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Harness)

A full-featured sample ASP.NET Core web application that demonstrates many GraphQL.NET features.
It supports multiple UI clients including [GraphiQL](https://github.com/graphql/graphiql), [Altair](https://altairgraphql.dev/),
[Voyager](https://github.com/graphql-kit/graphql-voyager), and [Firecamp](https://firecamp.dev/).

Key features demonstrated:
- Code-First schema definition using `ObjectGraphType<T>`
- ASP.NET Core integration
- Subscriptions via WebSockets
- Multiple serialization options
- DataLoader usage

## AOT Compilation Samples

### Code-First AOT Sample

[`samples/GraphQL.AotCompilationSample.CodeFirst`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.AotCompilationSample.CodeFirst)

Demonstrates running GraphQL.NET with **Ahead-of-Time (AOT) compilation** using the **Code-First** approach.

Key features demonstrated:
- Publishing with `PublishAot=true`
- Manual service registration required for AOT
- Limitations and workarounds for AOT scenarios

### Type-First AOT Sample

[`samples/GraphQL.AotCompilationSample.TypeFirst`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.AotCompilationSample.TypeFirst)

Demonstrates running GraphQL.NET with **AOT compilation** using the **Type-First** (CLR-First) approach,
where the schema is inferred from plain C# classes via attributes.

## DataLoader Samples

### DataLoader with Default Configuration

[`samples/GraphQL.DataLoader.Sample.Default`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.DataLoader.Sample.Default)

Demonstrates using **DataLoader** to batch and cache data-fetching operations.

### DataLoader with Dependency Injection

[`samples/GraphQL.DataLoader.Sample.DI`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.DataLoader.Sample.DI)

Demonstrates using **DataLoader** together with ASP.NET Core **Dependency Injection** and Entity Framework Core.

Key features demonstrated:
- `IDataLoaderContextAccessor` usage
- Scoped DataLoader registration with DI

## Federation Samples

These samples demonstrate [Apollo Federation](https://www.apollographql.com/docs/federation/) support in GraphQL.NET.

### Federation Schema-First Sample 1

[`samples/GraphQL.Federation.SchemaFirst.Sample1`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Federation.SchemaFirst.Sample1)

Federation subgraph using the **Schema-First** approach with Federation v1.

### Federation Schema-First Sample 2

[`samples/GraphQL.Federation.SchemaFirst.Sample2`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Federation.SchemaFirst.Sample2)

Federation subgraph using the **Schema-First** approach with Federation v2.

### Federation Code-First Sample 3

[`samples/GraphQL.Federation.CodeFirst.Sample3`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Federation.CodeFirst.Sample3)

Federation subgraph using the **Code-First** approach.

### Federation Type-First Sample 4

[`samples/GraphQL.Federation.TypeFirst.Sample4`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Federation.TypeFirst.Sample4)

Federation subgraph using the **Type-First** approach.

## StarWars Projects

The [`src/`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src) directory also contains StarWars-themed
reference implementations used as the basis for many tests:

- [`src/GraphQL.StarWars`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/GraphQL.StarWars) — Code-First StarWars schema
- [`src/GraphQL.StarWars.TypeFirst`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/GraphQL.StarWars.TypeFirst) — Type-First StarWars schema
