# Samples

The GraphQL.NET repository includes several sample projects that demonstrate various features and usage patterns.
All samples are located in the `samples/` directory of the repository.

## DataLoader Samples

### GraphQL.DataLoader.Sample.DI

**Location:** `samples/GraphQL.DataLoader.Sample.DI`

Demonstrates how to use DataLoader with dependency injection in an ASP.NET Core application. This sample
shows how to batch and cache database queries using the `IDataLoaderContextAccessor` and custom DataLoader
implementations, using a simulated dealership database.

**Key features:**
- DataLoader with dependency injection
- Batched database queries
- ASP.NET Core integration

### GraphQL.DataLoader.Sample.Default

**Location:** `samples/GraphQL.DataLoader.Sample.Default`

Demonstrates how to use DataLoader without dependency injection, using the default DataLoader context.

**Key features:**
- DataLoader without DI
- Default DataLoader context

## AOT Compilation Samples

These samples demonstrate how to use GraphQL.NET with .NET's Ahead-of-Time (AOT) compilation feature.

### GraphQL.AotCompilationSample.CodeFirst

**Location:** `samples/GraphQL.AotCompilationSample.CodeFirst`

Demonstrates building a GraphQL schema using the code-first approach with AOT compilation enabled.

**Key features:**
- Code-first schema definition
- AOT compilation compatibility
- Native AOT publishing

### GraphQL.AotCompilationSample.TypeFirst

**Location:** `samples/GraphQL.AotCompilationSample.TypeFirst`

Demonstrates building a GraphQL schema using the type-first approach with AOT compilation enabled.

**Key features:**
- Type-first schema definition
- AOT compilation compatibility
- `AutoRegisteringObjectGraphType` usage

## Federation Samples

These samples demonstrate GraphQL Federation support, which allows combining multiple GraphQL services
into a unified graph.

### GraphQL.Federation.SchemaFirst.Sample1

**Location:** `samples/GraphQL.Federation.SchemaFirst.Sample1`

Demonstrates Federation using schema-first (SDL) approach.

### GraphQL.Federation.SchemaFirst.Sample2

**Location:** `samples/GraphQL.Federation.SchemaFirst.Sample2`

A second schema-first Federation sample demonstrating additional Federation features.

### GraphQL.Federation.CodeFirst.Sample3

**Location:** `samples/GraphQL.Federation.CodeFirst.Sample3`

Demonstrates Federation using the code-first approach.

### GraphQL.Federation.TypeFirst.Sample4

**Location:** `samples/GraphQL.Federation.TypeFirst.Sample4`

Demonstrates Federation using the type-first approach with `AutoRegisteringObjectGraphType`.

## Harness Sample

### GraphQL.Harness

**Location:** `samples/GraphQL.Harness`

A general-purpose ASP.NET Core host for running GraphQL queries. This sample provides a foundation for
hosting a GraphQL API, including support for field middleware, user context, and dependency injection.

**Key features:**
- ASP.NET Core integration
- Field middleware demonstration (`CountFieldMiddleware`)
- User context setup
- JSON serialization configuration

## Custom Validation Rules

While not a standalone sample project, the documentation page on
[Query Validation](../getting-started/query-validation.md) contains comprehensive inline code samples
demonstrating how to write custom validation rules using the v8 API, including:

- Disabling introspection requests
- Limiting connection page sizes
- Adding validation via schema node visitors
- Using `IVariableVisitor` for variable validation

These samples are paired with tests in `src/GraphQL.Tests/Validation/CustomValidationRuleTests.cs`
to demonstrate that each example is functional.
