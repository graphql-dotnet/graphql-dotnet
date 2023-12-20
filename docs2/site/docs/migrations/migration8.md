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
    return builder.AuthorizeWithRoles("Administrators");
}
```

Both interfaces extend `IProvideMetadata` with read/write access to the metadata contained within the graph or field type.
Be sure not to write metadata during the execution of a query, as the same graph/field type instance may be used for
multiple queries and you would run into concurrency issues.

### 2. Built-in scalars may be overridden via DI registrations

For GraphQL.NET built-in scalars (such as `IntGraphType` or `GuidGraphType`), a dervied class may be registered
within the DI engine to facilitate replacement of the graph type throughout the schema versus calling `RegisterType`.

```csharp
services.AddSingleton<BooleanGraphType, MyBooleanGraphType>();
```

See https://graphql-dotnet.github.io/docs/getting-started/custom-scalars/#3-register-the-custom-scalar-within-your-schema
for more details.

### 3. Added `ComplexScalarGraphType`

This new scalar can be used to send or receive arbitrary objects or values to or from the server. It is functionally
equivalent to the `AnyGraphType` used for GraphQL Federation, but defaults to the name of `Complex` rather than `_Any`.

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

### 4. Relay types must be registered within the DI provider

Previuosly the Relay graph types were instantiated directly by the `SchemaTypes` class. This has been changed so that
the types are now pulled from the DI container. No changes are required if you are using the provided DI builder methods,
as they automatically register the relay types. Otherwise, you will need to manually register the Relay graph types.

```csharp
// v7 and prior -- builder methods -- NO CHANGES REQUIRED
services.AddGraphQL(b => {
  b.AddSchema<StarWarsSchema>();
});

// v7 and prior -- manual registration
services.AddSingleton<StarWarsSchema>(); // and other types

// v8
services.AddSingleton<PageInfoType>();
services.AddSingleton(typeof(EdgeType<>);
services.AddSingleton(typeof(ConnectionType<>);
services.AddSingleton(typeof(ConnectionType<,>);
```

### 5. Duplicate GraphQL configuration calls with the same `Use` command is ignored

Specifically, this relates to the following methods:

- `UseMemoryCache()`
- `UseAutomaticPersistedQueries()`
- `UseConfiguration<T>()` with the same `T` type

This change was made to prevent duplicate registrations of the same service within the DI container.

### 6. `ObjectExtensions.ToObject` changes (impacts `InputObjectGraphType`)

- `ObjectExtensions.ToObject<T>` was removed; it was only used by internal tests.
- `ObjectExtensions.ToObject` requires input object graph type for conversion.
- Only public constructors are eligible candidates while selecting a constructor.
- Constructor is selected based on the following rules:
  - If only a single public constructor is available, it is used.
  - If a public constructor is marked with `[GraphQLConstructor]`, it is used.
  - Otherwise the public parameterless constructor is used if available.
  - Otherwise an exception is thrown during deserialization.
- Only public properties are eligible candidates when matching a property.
- Any init-only or required properties not provided in the dictionary are set to their default values.
- Only public writable fields are eligible candidates when matching a field.

The changes above allow for matching behavior with source-generated or dynamically-compiled functions.

### 7. `AutoRegisteringInputObjectGraphType` changes

- See above changes to `ObjectExtensions.ToObject` for deserialization notes.
- Registers read-only properties when the property name matches the name of a parameter in the
  chosen constructor. Comparison is case-insensitive, matching `ToObject` behavior.
  Does not register constructor parameters that are not read-only properties.
  Any attributes such as `[Id]` must be applied to the property, not the constructor parameter.

### 8. Default naming of generic graph types has changed

The default graph name of generic types has changed to include the generic type name.
This should reduce naming conflicts when generics are in use. See below examples:

| Graph type class name | Old graph type name | New graph type name |
|------------------|---------------------|---------------------|
| `AutoRegisteringObjectGraphType<SearchResults<Person>>` | `SearchResults` | `PersonSearchResults` |
| `LoggerGraphType<Person, string>` | `Logger` | `PersonStringLogger` |

To revert to the prior behavior, set the following global switch prior to creating your schema classes:

```csharp
using GraphQL;

GlobalSwitches.UseLegacyTypeNaming = true;
```

As usual, you are encouraged to set the name in the constructor of your class, or
immediately after construction, or for auto-registering types, via an attribute.
You can also set global attributes that will be applied to all auto-registering types
if you wish to define your own naming logic.

The `UseLegacyTypeNaming` option is deprecated and will be removed in GraphQL.NET v9.
