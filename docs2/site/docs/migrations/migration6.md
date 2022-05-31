# Migrating from v5.x to v6.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A6.0+is%3Aissue+is%3Aclosed) and [pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A6.0+is%3Aclosed) done in v6.

## New Features

### 1. Reduced memory usage for data loader results

Especially noteworthy when a data loader is configured with caching enabled and a singleton lifetime,
memory usage is reduced by freeing unnecessary references after obtaining the results.

## Breaking Changes

### 1. `DataLoaderPair<TKey, T>.Loader` property removed

This property was not used internally and should not be necessary by user code or custom implementations.
Removal was necessary as the value is released after the result is set.
