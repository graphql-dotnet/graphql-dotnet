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

### More strict `InputObjectGraphType<TSourceType>.IsValidDefault`

This method began to perform more checks of the specified value. Current implementation
can cause problems in some situations. Changes are made for earlier error detection - at
the schema initialization stage. If you encountered that your code stopped working,
then feel free to post an issue.

### `AddGraphQL` now accepts a configuration delegate instead of returning `IGraphQLBuilder`

In order to prevent default implementations from ever being registered in the DI engine,
the `AddGraphQL` method now accepts a configuration delegate where you can configure the
GraphQL.NET DI components. To support this change, the `GraphQLBuilder` constructor now
requires a configuration delegate parameter and will execute the delegate before calling
`GraphQLBuilderBase.RegisterDefaultServices`.

This requires a change similar to the following:

```csharp
// v4
services.AddGraphQL()
    .AddSystemTextJson()
    .AddSchema<StarWarsSchema>();

// v5
services.AddGraphQL(builder => builder
    .AddSystemTextJson()
    .AddSchema<StarWarsSchema>());
```

### `GraphQLExtensions.BuildNamedType` renamed and moved to `SchemaTypes.BuildGraphQLType`

### `GraphQLBuilderBase.Initialize` renamed to `RegisterDefaultServices`

### All methods from `IGraphQLBuilder` were moved into `IServiceRegister` interface

Use `IGraphQLBuilder.Services` property if you need to register services into DI container.
If you use provided extension methods upon `IGraphQLBuilder` then your code does not require any changes.

### Classes and members marked as obsolete have been removed

The following classes and members that were marked with `[Obsolete]` in v4 have been removed:

| Class or member | Notes |
|-----------------|-------|
| `GraphQL.NewtonsoftJson.StringExtensions.GetValue`     |                             |
| `GraphQL.NewtonsoftJson.StringExtensions.ToDictionary` | Use `ToInputs` instead        |
| `GraphQL.SystemTextJson.ObjectDictionaryConverter`     | Use `InputsConverter` instead |
| `GraphQL.SystemTextJson.StringExtensions.ToDictionary` | Use `ToInputs` instead        |
| `GraphQL.TypeExtensions.GetEnumerableElementType`      |                             |
| `GraphQL.TypeExtensions.IsNullable`                    |                             |
| `GraphQL.Builders.ConnectionBuilder.Unidirectional`    | `Unidirectional` is default and does not need to be called |
| `GraphQL.IDocumentExecutionListener.BeforeExecutionAwaitedAsync`     | Use `IDataLoaderResult` interface instead |
| `GraphQL.IDocumentExecutionListener.BeforeExecutionStepAwaitedAsync` | Use `IDataLoaderResult` interface instead |
| `GraphQL.Utilities.DeprecatedDirectiveVisitor`         |                             |

Various classes' properties in the `GraphQL.Language.AST` namespace are now
read-only instead of read-write, such as `Field.Alias`.

Various classes' constructors in the `GraphQL.Language.AST` namespace have been
removed in favor of other constructors.
