# Custom Validation Rule Sample

This sample demonstrates how to implement a **custom GraphQL validation rule** using GraphQL.NET.

## What it shows

The sample implements `NoIntrospectionValidationRule`, which prevents clients from executing
[introspection queries](https://graphql.org/learn/introspection/) — queries that inspect the
schema itself (fields beginning with `__` such as `__schema`, `__type`, `__typename`).

Disabling introspection is a common security measure in production environments to prevent
clients from discovering your schema structure.

## Running the sample

