# Migrating from v7.x to v8.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A8.0+is%3Aissue+is%3Aclosed) and
[pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A8.0+is%3Aclosed) done in v8.

## New Features

### 1. `IMetadataReader` and `IMetadataWriter` interfaces added

This makes it convenient to add extension methods to graph types or fields that can be used to read or write metadata
such as authentication information. Methods for `IMetadataWriter` types will appear on both field builders and graph/field
types, while methods for `IMetadataReader` types will only appear on graph and field types. You can also access the
`IMetadataReader` reader from the `IMetadataWriter.MetadataReader` property. Here is an example:

```csharp
public static TMetadataBuilder RequireAdmin<TMetadataBuilder>(this TMetadataBuilder builder)
    where TMetadataBuilder : IMetadataWriter
{
    if (builder.MetadataReader.GetRoles?.Contains("Guests"))
        throw new InvalidOperationException("Cannot require admin and guest access at the same time.");
    builder.AuthorizeWithRoles("Administrators");
    return builder;
}
```

Both interfaces extend `IProvideMetadata` with read/write access to the metadata contained within the graph or field type.
Be sure not to write metadata during the execution of a query, as the same graph/field type instance may be used for
multiple queries and you would run into concurrency issues.

## Breaking Changes

### 1. Query type is required

Pursuant to the GraphQL specification, a query type is required for any schema.
This is enforced during schema validation but may be bypassed as follows:

```csharp
GlobalSwitches.RequireRootQueryType = false;
```

Future versions of GraphQL.NET will not contain this property and each schema will always be required to have a root Query type to comply with the GraphQL specification.

### 2. Use `ApplyDirective` instead of `Directive` on field builders

The `Directive` method on field builders has been renamed to `ApplyDirective` to better fit with
other field builder extension methods.

### 3. Use `WithComplexityImpact` instead of `ComplexityImpact` on field builders

The `ComplexityImpact` method on field builders has been renamed to `WithComplexityImpact` to better fit with
other field builder extension methods.
