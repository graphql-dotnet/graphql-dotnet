# Migrating from v7.x to v8.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A8.0+is%3Aissue+is%3Aclosed) and
[pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A8.0+is%3Aclosed) done in v8.

## New Features

## Breaking Changes

### 1. Query type is required

Pursuant to the GraphQL specification, a query type is required for any schema.
This is enforced during schema validation but may be bypassed as follows:

```csharp
GlobalSwitches.RequireRootQueryType = false;
```

Future versions of GraphQL.NET will not contain this property and each schema will always be required to have a root Query type to comply with the GraphQL specification.

