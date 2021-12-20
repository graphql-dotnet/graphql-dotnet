# Migrating from v4.x to v5.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A5.0+is%3Aissue+is%3Aclosed) and [pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A5.0+is%3Aclosed) done in v5.

## New Features

###

### Input Extensions support

`Extensions` deserialized from GraphQL requests can now be set on the `ExecutionOptions.Extensions` property
and passed through to field resolvers via `IResolveFieldContext.InputExtensions`. Note that standard .NET
dictionaries (such as `Dictionary<TKey, TValue>`) are thread-safe for read-only operations.

## Breaking Changes

### UnhandledExceptionDelegate

`ExecutionOptions.UnhandledExceptionDelegate` and `IExecutionContext.UnhandledExceptionDelegate`
properties type was changed from `Action<UnhandledExceptionContext>` to `Func<UnhandledExceptionContext, Task>`
so now you may use async/await for exception handling. In this regard, some methods in `ExecutionStrategy` were
renamed to have `Async` suffix.

### Redesign of [IDocumentCache](https://github.com/graphql-dotnet/graphql-dotnet/blob/develop/src/GraphQL/Caching/IDocumentCache.cs).

1. Use async methods to get or set a cache.
2. Cache items cannot be removed anymore.

### `IResolveFieldContext.Extensions` property renamed to `OutputExtensions`

To clarify and differ output extensions from input extensions, `IResolveFieldContext.Extensions`
has now been renamed to `OutputExtensions`. There has not been any change to the `GetExtension` and
`SetExtension` thread-safe extension methods.

### `ExecutionOptions.Inputs` property renamed to `Variables`

To better align the execution options with the specification, the `Inputs` property containing
the execution variables has now been renamed to `Variables`.
