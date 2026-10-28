---
title: Samples
---

The GraphQL.NET repository ships with several sample projects that demonstrate different features and integration scenarios. All samples can be found in the repository under the `src/` directory.

## StarWars (Code-First)

**Location:** [`src/StarWars`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/StarWars)

A classic implementation of the [Star Wars GraphQL API](https://graphql.org/swapi-graphql/) using **code-first** schema definition.

Topics demonstrated:
- Defining types with `ObjectGraphType<T>` and `InterfaceGraphType<T>`
- Field resolvers
- Query and mutation operations
- Dependency injection with `IServiceProvider`

## StarWars (Schema-First)

**Location:** [`src/StarWarsSchemaFirst`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/StarWarsSchemaFirst)

The same Star Wars API using **schema-first** (SDL) schema definition.

Topics demonstrated:
- Defining the schema with a GraphQL SDL string
- Mapping SDL types to .NET classes
- Field resolvers with SDL builder

## GraphQL.Harness

**Location:** [`src/GraphQL.Harness`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/GraphQL.Harness)

A complete ASP.NET Core web application that hosts a GraphQL endpoint.

Topics demonstrated:
- ASP.NET Core middleware setup
- Dependency injection integration
- Serving the GraphiQL browser UI
- WebSocket subscriptions

## AOT Samples

**Location:** [`src/Samples/AOT`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/Samples/AOT)

Two sample projects demonstrating how to use GraphQL.NET with **.NET Native AOT** compilation.

Topics demonstrated:
- AOT-compatible schema building
- Trimming annotations
- Avoiding reflection-based patterns that are incompatible with AOT

## Custom Validation Rule Sample

**Location:** [`src/Samples/CustomValidationRule`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/Samples/CustomValidationRule)

Demonstrates how to write a **custom validation rule** that inspects incoming GraphQL documents and rejects queries that violate application-specific constraints — all *before* execution begins.

The sample implements `NoIntrospectionValidationRule`, which disables GraphQL introspection.
Disabling introspection in production is a common security measure to prevent clients from
discovering your schema structure.

### What Is a Validation Rule?

Before GraphQL.NET executes a query it runs the parsed document through a chain of **validation rules**. Each rule receives an AST visitor context and can report errors. If any rule reports an error the query is rejected and no resolvers are called.

The built-in rules are available via `DocumentValidator.CoreRules`. You can extend this collection with your own rules.

### Implementing IValidationRule

A validation rule implements `IValidationRule`:

