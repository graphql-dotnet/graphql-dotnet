# Migrating from v8.x to v9.x

:warning: For the best upgrade experience, please upgrade to 8.5 :warning:

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A9.0.0+is%3Aissue+is%3Aclosed) and
[pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A9.0.0+is%3Aclosed) done in v9.

## Overview

GraphQL.NET v9 is a major release that includes many new features, including:

- ConcurrentDictionary is now used in ValidationContext 

1. Upgrade to GraphQL.NET v8.5
2. Use the included analyzers to apply automatic code fixes to obsolete code patterns
3. Upgrade to GraphQL.NET v9.0

## New Features

### 1. Use `ConcurrentDictionary` in `ValidationContext`

In previous versions, the `ValidationContext` used a plain `Dictionary` which required coercion in certain scenarios within `DocumentExecuter.BuildExecutionContext`. This has been updated to use `ConcurrentDictionary` to avoid the need for such coercion. See [#4083](https://github.com/graphql-dotnet/graphql-dotnet/issues/4083) for more details.
Additionally, several members now expose `IDictionary<,>` instead of `IReadOnlyDictionary<,>`: 
- `IExecutionContext.ArgumentValues` and `IExecutionContext.DictionaryValues`
- `IValidationResult.ArgumentValues` and `IValidationResult.DirectiveValues`
- `IExecutionContext.ArgumentValues` and `IExecutionContext.DictionaryValues`
This is considered a breaking change and should be addressed if you extend these interfaces or access these properties directly.

