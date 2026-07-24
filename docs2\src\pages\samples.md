---
title: Samples
order: 10
---

# Samples

The GraphQL.NET repository includes several sample projects that demonstrate different approaches
to building GraphQL APIs with this library. Each sample is located in the `src/` directory of
the repository.

---

## Schema-First Sample

**Project:** `src/GraphQL.SchemaFirstDemo`

Demonstrates the **Schema-First** (also called _SDL-First_) approach, where you define your
entire API surface using the [GraphQL Schema Definition Language](https://graphql.org/learn/schema/)
(SDL) and then wire up .NET resolver classes to provide the runtime behaviour.

### When to use Schema-First

| ✅ Good fit | ❌ Consider other approaches |
|---|---|
| You want to keep the schema as a single source of truth | You need advanced type-system features (custom scalars with complex serialization, etc.) |
| Multiple teams share one `.graphql` contract file | You prefer a strongly-typed, code-first API surface |
| You are porting an existing schema from another language | You rely heavily on code generation from C# types |

### How it works

1. **Define the schema in SDL** – a plain string (or `.graphql` file) describing types, queries,
   mutations and subscriptions.
2. **Create resolver classes** – plain C# classes annotated with `[GraphQLMetadata]` that contain
   methods for each field.
3. **Register with DI** – call `schema.For(sdl, cfg => { cfg.Types.Include<MyResolverClass>(); })`
   inside your `Schema` constructor.

