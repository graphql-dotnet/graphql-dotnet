---
title: Samples
---

# Samples

The GraphQL.NET repository includes several sample projects that demonstrate different features and use cases. These samples can be found in the `src/Samples` directory of the repository.

## Available Samples

### StarWars (Classic)

Located in `src/StarWars`, this sample demonstrates a classic implementation of the Star Wars GraphQL API using code-first schema definition.

**Features demonstrated:**
- Code-first schema definition using `ObjectGraphType<T>`
- Field resolvers
- Interfaces and unions
- Query and mutation operations
- Dependency injection

### StarWars (Schema-First)

Located in `src/StarWarsSchemaFirst`, this sample demonstrates the Star Wars API using schema-first (SDL) approach.

**Features demonstrated:**
- Schema-first (SDL) schema definition
- Type resolvers
- Field resolvers with SDL

### GraphQL.Harness

Located in `src/GraphQL.Harness`, this sample demonstrates a more complete ASP.NET Core web application hosting a GraphQL API.

**Features demonstrated:**
- ASP.NET Core integration
- Dependency injection
- Middleware configuration
- GraphiQL UI

### AOT Sample

Located in `src/Samples/AOT`, these samples demonstrate how to use GraphQL.NET in ahead-of-time (AOT) compilation scenarios (e.g., Native AOT with .NET).

**Features demonstrated:**
- AOT compatibility
- Trimming-safe usage patterns

### Custom Validation Rule Sample

Located in `src/Samples/CustomValidationRule`, this sample demonstrates how to create a **custom validation rule** — a mechanism to enforce your own constraints on incoming GraphQL queries before execution begins.

**Features demonstrated:**
- Implementing `IValidationRule`
- Implementing `INodeVisitor`
- Reporting validation errors with `ValidationError`
- Combining custom rules with built-in `DocumentValidator.CoreRules`

#### What is a Validation Rule?

When GraphQL.NET receives a query, it runs the document through a set of **validation rules** before executing it. Each rule inspects the AST (abstract syntax tree) of the query and can report errors if the query violates a constraint.

The built-in rules (e.g., checking that fields exist, arguments have correct types, etc.) are available via `DocumentValidator.CoreRules`. You can extend this collection with your own rules.

#### The `NoIntrospectionValidationRule`

The sample implements a `NoIntrospectionValidationRule` that rejects any query containing introspection fields (fields whose names start with `__`, such as `__schema`, `__type`, and `__typename`).

Disabling introspection in production is a common security practice to prevent clients from discovering your schema structure.

