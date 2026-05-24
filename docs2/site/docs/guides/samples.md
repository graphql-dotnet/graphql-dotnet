# Sample Projects

The GraphQL.NET repository includes several sample projects that demonstrate different
approaches and features. These samples serve as working examples you can run locally to
explore the library's capabilities.

## Schema-First Sample

**Project:** [`GraphQL.SchemaFirst.Sample`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.SchemaFirst.Sample)

Demonstrates the Schema-First approach using `SchemaBuilder` to define a book catalog API
from SDL. Key features shown:

- Defining types (`Book`, `Author`) and operations (`Query`, `Mutation`) in SDL
- Building the schema with `SchemaBuilder` and `Types.Include<T>()`
- Configuring custom field resolvers with `FieldFor().Resolver`
- Using `[FromServices]` and `[FromSource]` attributes for DI

## DataLoader Samples

### Default DataLoader

**Project:** [`GraphQL.DataLoader.Sample.Default`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.DataLoader.Sample.Default)

Shows the default DataLoader pattern for batching database queries to avoid N+1 problems.
Uses ASP.NET Core with dependency injection.

### DI-based DataLoader

**Project:** [`GraphQL.DataLoader.Sample.DI`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.DataLoader.Sample.DI)

An alternative DataLoader setup using explicit dependency injection registration for
DataLoader providers.

## AOT Compilation Samples

### Code-First AOT

**Project:** [`GraphQL.AotCompilationSample.CodeFirst`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.AotCompilationSample.CodeFirst)

Demonstrates ahead-of-time (AOT) compilation compatibility with the Code-First approach.
Targets .NET 10 with native AOT publishing.

### Type-First AOT

**Project:** [`GraphQL.AotCompilationSample.TypeFirst`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.AotCompilationSample.TypeFirst)

Demonstrates AOT compilation compatibility with the Type-First approach. Also targets
.NET 10 with native AOT publishing.

## Federation Samples

The [`Federation`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/Federation)
directory contains multiple samples demonstrating Apollo Federation support:

| Sample | Approach | Description |
|--------|----------|-------------|
| `GraphQL.Federation.SchemaFirst.Sample1` | Schema-First | Federation with SDL-defined schema |
| `GraphQL.Federation.SchemaFirst.Sample2` | Schema-First | Another federation schema-first example |
| `GraphQL.Federation.CodeFirst.Sample3` | Code-First | Federation with code-defined schema |
| `GraphQL.Federation.TypeFirst.Sample4` | Type-First | Federation with type-first approach |
| `GraphQL.Federation.Tests` | Tests | Integration tests for federation samples |

## GraphQL.Harness

**Project:** [`GraphQL.Harness`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Harness)

A full ASP.NET Core application demonstrating GraphQL.NET setup with middleware, GraphiQL
UI, and the Star Wars schema. Supports multiple target frameworks (net6.0, net8.0, net10.0).

Includes test coverage via `GraphQL.Harness.Tests`.

## Server Polyfill

**Project:** [`GraphQL.Server.Polyfill`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Server.Polyfill)

A server-side polyfill sample for compatibility scenarios.

## Running the Samples

To run any sample:

```bash
cd samples/<sample-name>
dotnet run
```

Most web-based samples will start an ASP.NET Core server. Open the displayed URL in a
browser to access the GraphiQL interface and explore the API.
