# Migrating from v4.x to v5.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A5.0+is%3Aissue+is%3Aclosed) and [pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A5.0+is%3Aclosed) done in v5.

## New Features

###

###

## Breaking Changes

### Redesign of [IDocumentCache](https://github.com/graphql-dotnet/graphql-dotnet/blob/develop/src/GraphQL/Caching/IDocumentCache.cs).

1. Use async methods to get or set a cache.
2. Cache items cannot be removed anymore.
