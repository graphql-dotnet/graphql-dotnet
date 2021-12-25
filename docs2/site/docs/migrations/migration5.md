# Migrating from v4.x to v5.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A5.0+is%3Aissue+is%3Aclosed) and [pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A5.0+is%3Aclosed) done in v5.

## New Features

### DoNotMapClrType attribute can now be placed on the graph type or the CLR type

When using the `.AddClrTypeMappings()` builder extension method, GraphQL.NET scans the
specified assembly for graph types that inherit from `ObjectGraphType<T>` and adds a
mapping for the CLR type represented by `T` with the graph type it matched upon.
It skips adding a mapping for any graph type marked with the `[DoNotMapClrType]` attribute.
In v5, it will also skip adding the mapping if the CLR type is marked with the
`[DoNotMapClrType]` attribute.

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

### `IDocumentCache` now has asynchronous methods instead of synchronous methods.

The default get/set property of the interface has been replaced with `GetAsync` and `SetAsync` methods.
Keys cannot be removed by setting a null value as they could before.

### `IResolveFieldContext.Extensions` property renamed to `OutputExtensions` and related changes

To clarify and differ output extensions from input extensions, `IResolveFieldContext.Extensions`
has now been renamed to `OutputExtensions`. The `GetExtension` and `SetExtension` thread-safe
extension methods have also been renamed to `GetOutputExtension` and `SetOutputExtension` respectively.

### `ExecutionOptions.Inputs` and `ValidationContext.Inputs` properties renamed to `Variables`

To better align the execution options and variable context with the specification, the `Inputs`
property containing the execution variables has now been renamed to `Variables`.

### `ConfigureExecution` GraphQL builder method renamed to `ConfigureExecutionOptions`

Also, `IConfigureExecution` renamed to `IConfigureExecutionOptions`.
