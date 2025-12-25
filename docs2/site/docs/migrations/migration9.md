# Migrating from v8.x to v9.x

:warning: For the best upgrade experience, please upgrade to v8.x and resolve all obsolete code warnings
before upgrading to v9.0. Many members that were marked as obsolete in v8.x have been removed in v9.0. :warning:

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A9.0.0+is%3Aissue+is%3Aclosed) and
[pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A9.0.0+is%3Aclosed) done in v9.

## Overview

GraphQL.NET v9 is a major release that focuses on removing obsolete APIs and improving performance. The primary changes include:

- Removal of many members that were marked as obsolete in v8.x
- Improved concurrency support

For the smoothest migration experience, we strongly recommend:

1. Upgrade to the latest v8.x version
2. Resolve all compiler warnings about obsolete members
3. Test your application thoroughly
4. Then upgrade to v9.0

## New Features

### 1. GraphQL Specification September 2025 Features Enabled by Default

Two features from the GraphQL specification dated September 2025 are now enabled by default:

- **Repeatable Directives**: The `isRepeatable` field is now exposed for directives via introspection by default. This allows clients to determine whether a directive can be applied multiple times to the same location.
- **Deprecation of Input Values**: Input values (arguments on fields and input fields on input types) can now be deprecated by default, similar to how output fields and enum values can be deprecated.

These features were previously opt-in via `Schema.Features` but are now part of the official GraphQL specification and enabled by default.

If you need to disable these features for backward compatibility, you can set them to `false`:

```csharp
schema.Features.RepeatableDirectives = false;
schema.Features.DeprecationOfInputValues = false;
```

Note that these properties must be set before schema initialization.

## Breaking Changes

### 1. Removal of Obsolete Members

Many members that were marked as obsolete in v8.x have been removed in v9.0. The following is a summary of the key members that have been removed:

- `GlobalSwitches.UseLegacyTypeNaming`
- `GlobalSwitches.RequireRootQueryType`
- `FieldBuilder.Directive()` method - use `ApplyDirective()` instead
- `SchemaPrinter` class - use `schema.Print()` extension method instead
- `LegacyComplexityValidationRule` - use the newer complexity analyzer instead
- `IFederatedResolver` interface - use `IFederationResolver` instead
- `FederatedSchemaBuilder` - use `SchemaBuilder` with `.AddFederation()` instead
- Related classes within `GraphQL.Utilities.Federation` including `InjectTypenameValidationRule`
- Built-in validation rule constructors - use `.Instance` instead
- Field registration methods which do not include the name explicitly or implicitly - use overload with field name instead
- Field registration methods that include both the bool nullable and Type graphType parameters - use overload with field name and type instead
- Field methods which include arguments and the resolver as parameters - use field builder methods instead
- Field argument registration methods that include default values - use configuration overload instead

> **Note:** The GraphQL.NET v8 analyzers can help automatically update obsolete API calls with code fixes, prior to upgrading to v9.

#### FederationSchemaBuilder migration

To create Federation 1.0 schemas in v9, you will need to dependency injection along with the `AddFederation` extension method as seen below:

```csharp
// v8 using FederationSchemaBuilder
var schema = GenerateSchema();

ISchema GenerateSchema()
{
    string sdl = /* load sdl */;
    var builder = new FederatedSchemaBuilder();
    // configuration here
    return builder.Build(sdl);
}


// v9 using SchemaBuilder with AddFederation
services.AddGraphQL(b => b
    .AddSchema(GenerateSchema)
    .AddFederation("1.0")
);

ISchema GenerateSchema(IServiceProvider serviceProvider)
{
    string sdl = /* load sdl */;
    var builder = new SchemaBuilder
    {
        ServiceProvider = serviceProvider,
    };
    // configuration here
    return builder.Build(sdl);
}
```

#### SchemaPrinter migration

The `SchemaPrinter` has been replaced by the new `SchemaExporter` class and the `Print` extension method on `ISchema`.
The default print options have changed when comparing `SchemaPrinter` to its replacement, but even so, there may be differences in the output.
Please review [this documentation](migration7/#13-add-code-classlanguage-textschemaexportercode-to-export-schema-to-sdl-with-new-code-classlanguage-textschematoastcode-and-code-classlanguage-textschemaprintcode-methods)
for details on the new features available. A simple migration example is shown below:

```csharp
// v8
var printer = new SchemaPrinter(schema);
var sdl = printer.Print();

// v9 configured to have similar behavior as previous defaults
var sdl = schema.Print(new() {
    IncludeDescriptions = false,
    IncludeDeprecationReasons = false,
    IncludeFederationTypes = false,
    StringComparison = StringComparison.OrdinalIgnoreCase,
});
```

### 2. `ConcurrentDictionary` in `ValidationContext`

Previously, `ValidationContext` used `Dictionary`-typed members, which prevented the use of `ConcurrentDictionary` even when concurrent access would have been beneficial. This limitation became apparent in two key issues:

- **[#4083](https://github.com/graphql-dotnet/graphql-dotnet/issues/4083):** Merged selection sets required writable access to the argument dictionary. However, since the relevant interfaces exposed `IReadOnlyDictionary`, code had to cast to `Dictionary` in order to write values, leading to brittle and potentially unsafe patterns.
- **[#4060](https://github.com/graphql-dotnet/graphql-dotnet/issues/4060):** Highlighted cases where user code wrote directly to the argument dictionary. Without concurrency guarantees, this introduced the risk of race conditions.

To address both concerns:

- `ValidationContext` now uses `ConcurrentDictionary` for `ArgumentValues` and `DirectiveValues` to support safe concurrent writes.
- Several interfaces and classes now expose `IDictionary<,>` instead of `IReadOnlyDictionary<,>`, allowing controlled write access:

  - `IExecutionContext.ArgumentValues` and `IExecutionContext.DirectiveValues`
  - `ExecutionContext.ArgumentValues` and `ExecutionContext.DirectiveValues`
  - `IValidationResult.ArgumentValues` and `IValidationResult.DirectiveValues`
  - `ValidationResult.ArgumentValues` and `ValidationResult.DirectiveValues`

### 2. New methods added to `IAbstractGraphType`

The `IAbstractGraphType` interface has been extended with new methods that were previously only available on `InterfaceGraphType` and `UnionGraphType`. Since both interface and union graph types require the same methods for the same purpose, these methods have been moved to the common interface:

- `Type(Type)` - Adds the specified graph type to the list of possible graph types
- `Type<TType>()` - Generic version to add a graph type to the list of possible graph types
- `Types` - Property to get or set the collection of possible types

If you have custom implementations of `IAbstractGraphType`, you will need to implement these new methods and property. Most users who inherit from `InterfaceGraphType` or `UnionGraphType` will not be affected as these base classes already provide the implementations.

### 3. `ParseLinkVisitor.Run` method removed

The `ParseLinkVisitor.Run` method has been removed. However, no changes should be required in your code since an equivalent extension method `Run` already exists for all `ISchemaNodeVisitor` instances, including `ParseLinkVisitor`.

### 4. Async suffix removal for type-first field names

In type-first GraphQL schemas, field names ending with "Async" are now automatically removed for methods returning `ValueTask<T>` and `IAsyncEnumerable<T>`, consistent with the existing behavior for `Task<T>`.

For example, previously only `Task<string> GetDataAsync()` would become field `"GetData"`, while `ValueTask<string> GetDataAsync()` and `IAsyncEnumerable<int> GetItemsAsync()` would keep their "Async" suffix. Now all three async return types have consistent field naming.

If you have type-first schemas with `ValueTask<T>` or `IAsyncEnumerable<T>` methods ending in "Async", update your GraphQL queries to use the new field names without the "Async" suffix, or use the `[Name]` attribute to explicitly specify the desired field name.

### 5. `ISchema.ResolveFieldContextAccessor` property added

The `ResolveFieldContextAccessor` property has been added to the `ISchema` interface. This property was previously only available on the `Schema` class. If you have custom implementations of `ISchema`, you will need to implement this property. Most users who inherit from `Schema` will not be affected as the base class already provides the implementation.

### 6. `ApplyMiddleware` methods moved from `SchemaTypes` to extension methods

The `ApplyMiddleware` methods have been moved from the `SchemaTypes` class to extension methods in the `SchemaTypesExtensions` class. This change improves code organization and follows .NET best practices for extending types. These methods are typically only called by `Schema` and as such should not require any changes to your code.

### 7. `SchemaTypes` is now an abstract base class

The `SchemaTypes` class has been refactored into an abstract base class with a new `LegacySchemaTypes` derived class that contains the concrete implementation. This change enables future extensibility while maintaining backward compatibility.

Typically no changes are required unless you override `Schema.CreateSchemaTypes()` and/or use your own `SchemaTypes` implementation. In those cases, you should update your code to use or derive from `LegacySchemaTypes` instead of `SchemaTypes`.
