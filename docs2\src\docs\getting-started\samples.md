---
title: Samples
---

The GraphQL.NET repository ships with several sample projects that demonstrate different features and integration scenarios. All samples can be found in the [`src/Samples`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/Samples) directory (or as top-level projects under `src/`).

## StarWars (Code-First)

**Location:** `src/StarWars`

A classic implementation of the [Star Wars GraphQL API](https://github.com/graphql/swapi-graphql) using **code-first** schema definition.

Topics demonstrated:
- Defining types with `ObjectGraphType<T>` and `InterfaceGraphType<T>`
- Field resolvers
- Query and mutation operations
- Dependency injection with `IServiceProvider`

## StarWars (Schema-First)

**Location:** `src/StarWarsSchemaFirst` *(if present)*

The same Star Wars API using **schema-first** (SDL) schema definition.

Topics demonstrated:
- Defining the schema with a GraphQL SDL string
- Mapping SDL types to .NET classes
- Field resolvers via `resolver.Field(...)`

## GraphQL.Harness

**Location:** `src/GraphQL.Harness`

A more complete ASP.NET Core web application that hosts a GraphQL endpoint.

Topics demonstrated:
- ASP.NET Core middleware setup
- Dependency injection
- Serving the GraphiQL browser UI
- Subscription support over WebSockets

## AOT Samples

**Location:** `src/Samples/AOT`

Two samples demonstrating how to use GraphQL.NET with **.NET Native AOT** (ahead-of-time compilation), where JIT reflection is unavailable.

Topics demonstrated:
- AOT-compatible schema building
- Trimming annotations
- Avoiding reflection-based features

## Custom Validation Rule Sample

**Location:** `src/Samples/CustomValidationRule`

Demonstrates how to write a **custom validation rule** that inspects incoming GraphQL documents and rejects queries that violate application-specific constraints — all *before* execution begins.

The sample implements `NoIntrospectionValidationRule`, which disables GraphQL introspection. Disabling introspection in production is a common security measure to prevent clients from discovering your schema structure.

### What Is a Validation Rule?

Before GraphQL.NET executes a query it runs the parsed document through a chain of **validation rules**. Each rule receives an AST visitor context and can report errors. If any rule reports an error the query is rejected and no resolvers are called.

The built-in rules are available via `DocumentValidator.CoreRules`. You can extend this collection with your own rules.

### Implementing IValidationRule

A validation rule implements `IValidationRule`:

