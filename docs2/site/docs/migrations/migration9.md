# Migrating from v8.x to v9.x

:warning: For the best upgrade experience, please upgrade to v8.x and resolve all obsolete code warnings
before upgrading to v9.0. Many members that were marked as obsolete in v8.x have been removed in v9.0. :warning:

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A9.0.0+is%3Aissue+is%3Aclosed) and
[pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A9.0.0+is%3Aclosed) done in v9.

## Overview

GraphQL.NET v9 is a major release that focuses on removing obsolete APIs and improving performance. The primary changes include:

- Removal of many members that were marked as obsolete in v8.x
- Improved concurrency support

For the smoothest migration experience, we strongly recommend:

1. Upgrade to the latest v8.x version
2. Resolve all compiler warnings about obsolete members
3. Test your application thoroughly
4. Then upgrade to v9.0

## New Features

## Breaking Changes

### 1. Removal of Obsolete Members

Many members that were marked as obsolete in v8.x have been removed in v9.0. The following is a summary of the key members that have been removed:

- `GlobalSwitches.UseLegacyTypeNaming`
- `GlobalSwitches.RequireRootQueryType`
- `FieldBuilder.Directive()` method - use `ApplyDirective()` instead
- `SchemaPrinter` class - use `schema.Print()` extension method instead
- `LegacyComplexityValidationRule` - use the newer complexity analyzer instead
- `IFederatedResolver` interface - use `IFederationResolver` instead
- `FederatedSchemaBuilder` - use `SchemaBuilder` with `.AddFederation()` instead
- Related classes within `GraphQL.Utilities.Federation` including `InjectTypenameValidationRule`
- Built-in validation rule constructors - use `.Instance` instead
- Field registration methods which do not include the name explicitly or implicitly - use overload with field name instead
- Field registration methods that include both the bool nullable and Type graphType parameters - use overload with field name and type instead
- Field methods which include arguments and the resolver as parameters - use field builder methods instead
- Field argument registration methods that include default values - use configuration overload instead

> **Note:** The GraphQL.NET v8 analyzers can help automatically update obsolete API calls with code fixes, prior to upgrading to v9.

### 2. `ConcurrentDictionary` in `ValidationContext`

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
