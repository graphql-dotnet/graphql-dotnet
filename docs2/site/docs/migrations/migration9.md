# Migrating from v8.x to v9.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A9.0.0+is%3Aissue+is%3Aclosed) and
[pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A9.0.0+is%3Aclosed) done in v9.

## Overview

## New Features

## Breaking Changes

### 1. `ConcurrentDictionary` in `ValidationContext`

Previously, `ValidationContext` used `Dictionary`-typed members, which prevented the use of `ConcurrentDictionary` even when concurrent access would have been beneficial. This limitation became apparent in two key issues:

- **[#4083](https://github.com/graphql-dotnet/graphql-dotnet/issues/4083):** Merged selection sets required writable access to the argument dictionary. However, since the relevant interfaces exposed `IReadOnlyDictionary`, code had to cast to `Dictionary` in order to write values, leading to brittle and potentially unsafe patterns.
- **[#4060](https://github.com/graphql-dotnet/graphql-dotnet/issues/4060):** Highlighted cases where user code wrote directly to the argument dictionary. Without concurrency guarantees, this introduced the risk of race conditions.

To address both concerns:

- `ValidationContext` now uses `ConcurrentDictionary` for `ArgumentValues` and `DirectiveValues` to support safe concurrent writes.
- Several interfaces and classes now expose `IDictionary<,>` instead of `IReadOnlyDictionary<,>`, allowing controlled write access:

  - `IExecutionContext.ArgumentValues` and `IExecutionContext.DirectiveValues`
  - `ExecutionContext.ArgumentValues` and `ExecutionContext.DirectiveValues`
  - `IValidationResult.ArgumentValues` and `IValidationResult.DirectiveValues`
  - `ValidationResult.ArgumentValues` and `ValidationResult.DirectiveValues`

### 2. Async suffix removal for type-first field names

In type-first GraphQL schemas, field names ending with "Async" are now automatically removed for methods returning `ValueTask<T>` and `IAsyncEnumerable<T>`, consistent with the existing behavior for `Task<T>`.

For example, previously only `Task<string> GetDataAsync()` would become field `"GetData"`, while `ValueTask<string> GetDataAsync()` and `IAsyncEnumerable<int> GetItemsAsync()` would keep their "Async" suffix. Now all three async return types have consistent field naming.

If you have type-first schemas with `ValueTask<T>` or `IAsyncEnumerable<T>` methods ending in "Async", update your GraphQL queries to use the new field names without the "Async" suffix, or use the `[Name]` attribute to explicitly specify the desired field name.
