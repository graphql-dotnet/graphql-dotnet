# Migrating from v3.x to v4.x

## New Features

* Extension methods to configure authorization requirements for GraphQL elements: types, fields, schema.
* New property `GraphQL.Introspection.ISchemaComparer ISchema.Comparer { get; set; }`

## Breaking Changes

* `NameConverter` and `SchemaFilter` have been removed from `ExecutionOptions` and are now properties on the `Schema`.
* `GraphQL.Utilities.ServiceProviderExtensions` has been made internal. This affects usages of its extension method `GetRequiredService`. Instead, reference the `Microsoft.Extensions.DependencyInjection.Abstractions` NuGet package and use extension method from `Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions` class.
* When used, Apollo tracing will now convert the starting timestamp to UTC so that `StartTime` and `EndTime` are properly serialized as UTC values.
* `ApolloTracing.ConvertTime` is now private and `ResolverTrace.Path` does not initialize an empty list when created.
* `LightweightCache.First` has been removed.
* `IGraphType.CollectTypes` has been removed.
* `TypeCollectionContext` is now internal, also all methods with this parameter in `GraphTypesLookup` are private.
* `GraphTypesLookup.ApplyTypeReferences` is now private.
* `IHaveDefaultValue.Type` has been moved to `IProvideResolvedType.Type`
* `Connection<TNode, TEdge>.TotalCount` has been changed from an `int` to an `int?`. This allows for returning `null` indicating that the total count is unknown.
* By default fields returned by introspection query are no longer sorted by their names.
