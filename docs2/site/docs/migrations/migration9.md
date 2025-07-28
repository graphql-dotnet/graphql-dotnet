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

### 2. New methods added to `IAbstractGraphType`

The `IAbstractGraphType` interface has been extended with new methods that were previously only available on `InterfaceGraphType` and `UnionGraphType`. Since both interface and union graph types require the same methods for the same purpose, these methods have been moved to the common interface:

- `Type(Type)` - Adds the specified graph type to the list of possible graph types
- `Type<TType>()` - Generic version to add a graph type to the list of possible graph types
- `Types` - Property to get or set the collection of possible types

If you have custom implementations of `IAbstractGraphType`, you will need to implement these new methods and property. Most users who inherit from `InterfaceGraphType` or `UnionGraphType` will not be affected as these base classes already provide the implementations.

### 3. `ParseLinkVisitor.Run` method removed

The `ParseLinkVisitor.Run` method has been removed. However, no changes should be required in your code since an equivalent extension method `Run` already exists for all `ISchemaNodeVisitor` instances, including `ParseLinkVisitor`.
