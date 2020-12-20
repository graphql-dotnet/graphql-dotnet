# Migrating from v3.x to v4.x

## New Features

* Extension methods to configure authorization requirements for GraphQL elements: types, fields, schema.

## Breaking Changes

* `NameConverter` and `SchemaFilter` have been removed from `ExecutionOptions` and are now properties on the `Schema`.
* `GraphQL.Utilities.ServiceProviderExtensions` has been made internal. This affects usages of it's extension method `GetRequiredService`. Instead reference the `Microsoft.Extensions.DependencyInjection.Abstractions` NuGet package and use extension method from `Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions` class.
