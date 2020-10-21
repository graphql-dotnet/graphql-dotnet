# Migrating from v3.x to v4.x

## New Features

## Breaking Changes

* `NameConverter` and `SchemaFilter` have been removed from `ExecutionOptions` and are now properties on the `Schema`.
* `GraphQL.Utilities.ServiceProviderExtensions` has been made internal. This effects usages of it's extensions method `GetRequiredService`. Instead reference the `Microsoft.Extensions.DependencyInjection.Abstractions` NuGet and use `Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions`.
