# Migrating from v0.17.x to v2

## New Features

* This documentation!
* Subscriptions - [details](https://graphql-dotnet.github.io/docs/getting-started/subscriptions)
* DataLoader - helps solve N+1 requests - [details](https://graphql-dotnet.github.io/docs/guides/dataloader)
* New `SchemaBuilder` that supports GraphQL schema language. [details](https://graphql-dotnet.github.io/docs/getting-started/introduction#schema-first-approach)
* Unique Directive Per Location Validation Rule -  [details](https://github.com/graphql-dotnet/graphql-dotnet/issues/231)
* Apollo Tracing - [details](https://graphql-dotnet.github.io/docs/getting-started/metrics)
* Parser support for the `null` keyword
* Addition of `IDependencyResolver` for dependency injection - [details](https://graphql-dotnet.github.io/docs/getting-started/dependency-injection)
* Add `ThrowOnUnhandledException` to `ExecutionOptions`. [details](https://github.com/graphql-dotnet/graphql-dotnet/pull/776)
* Add the ability to return a `GraphQLTypeReference` from `ResolveType` [details](https://github.com/graphql-dotnet/graphql-dotnet/pull/775)
* General updates to conform to the June 2018 Specification - [details](https://github.com/facebook/graphql/releases/tag/June2018)

## Breaking Changes

### Dependency Injection

The func that was previously used for dependency injection has been replaced by the `IDependencyResolver` interface.  Use `FuncDependencyResolver` to help integrate with containers.  See the [Dependency Injection documentation](https://graphql-dotnet.github.io/docs/getting-started/dependency-injection) for more details.

```csharp
[Obsolete]
public Schema(Func<Type, IGraphType> resolveType)
    : this(new FuncDependencyResolver(resolveType))
{
}

public Schema(IDependencyResolver dependencyResolver)
{
  ...
}
```

### DocumentWriter

The `JsonSerializerSettings` now use all default values.  This was altered to support the changes to dates.

### Dates

The `DateGraphType` has been split into multiple types.  [See the GitHub issue](https://github.com/graphql-dotnet/graphql-dotnet/issues/662) for more details.

- `DateGraphType` - A date with no time.
  - Scalar Name: `Date`
  - Format: `2018-05-17` (ISO8601 compliant).
  - Maps to .NET type - `System.DateTime`
  - Added to `GraphTypeRegistry` as the default representation of `System.DateTime`.
- `DateTimeGraphType` - A date and time.
  - Scalar Name: `DateTime`
  - Format: `2018-05-17T12:11:06.3684072Z` (ISO8601 compliant).
  - Maps to .NET type - `System.DateTime`
- `DateTimeOffsetGraphType`  - A date and time with an offset.
  - Scalar Name: `DateTimeOffset`
  - Format: : `2018-05-17T13:11:06.368408+01:00` (ISO8601 compliant).
  - Maps to .NET type `System.DateTimeOffset`
  - Added to `GraphTypeRegistry` as the default representation of `System.DateTimeOffset`.
- `TimeSpanSecondsGraphType` - A period of time as seconds.
  - Scalar Name: `Seconds`
  - Format: `10`
  - Maps to .NET type - `System.TimeSpan`
  - Added to `GraphTypeRegistry` as the default representation of `System.TimeSpan`.
- `TimeSpanMillisecondsGraphType` - A period of time as milliseconds.
  - Scalar Name: `Milliseconds`
  - Format: `100`
  - Maps to .NET type - `System.TimeSpan`

### Names

Fields, enumerations, and arguments all now have their names validated according to the GraphQL spec, which is `/[_A-Za-z][_0-9A-Za-z]*/`.

`QueryArgument` names are now run through the `IFieldNameConverter` set on the `Schema`.

### SchemaPrinter

`SchemaPrinter` now only ignores core GraphQL scalars by default.  Those are `String`, `Boolean`, `Int`, `Float`, and `ID`.  [See the GitHub issue](https://github.com/graphql-dotnet/graphql-dotnet/issues/378) for more details.
