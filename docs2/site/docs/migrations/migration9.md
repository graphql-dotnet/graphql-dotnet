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

### 2. Automatic Nullable Value Type Detection in Field Arguments

The `Argument<T>` method on field builders now supports automatic nullable value type detection. When the `nullable` parameter is not specified (defaults to `null`), nullable value types like `int?`, `DateTime?`, etc. will automatically be treated as nullable fields in the GraphQL schema.

```csharp
// Before v9 - explicit nullable parameter required
Field<StringGraphType>("myField")
    .Argument<int?>("nullableArg", nullable: true)  // Had to explicitly set nullable: true
    .Argument<int>("requiredArg", nullable: false); // Explicitly non-null

// v9 - automatic detection when nullable parameter is omitted
Field<StringGraphType>("myField")
    .Argument<int?>("nullableArg")     // Automatically nullable (nullable value type)
    .Argument<int>("requiredArg");     // Automatically non-null (non-nullable value type)
```

This feature makes it easier to work with nullable value types without having to explicitly specify the `nullable` parameter. Note that reference types like `string` still default to non-null and require explicitly setting `nullable: true` to make them optional. You can always explicitly set `nullable: true` or `nullable: false` to override the automatic behavior if needed.

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

### 3. New methods added to `IAbstractGraphType`

The `IAbstractGraphType` interface has been extended with new methods that were previously only available on `InterfaceGraphType` and `UnionGraphType`. Since both interface and union graph types require the same methods for the same purpose, these methods have been moved to the common interface:

- `Type(Type)` - Adds the specified graph type to the list of possible graph types
- `Type<TType>()` - Generic version to add a graph type to the list of possible graph types
- `Types` - Property to get or set the collection of possible types

If you have custom implementations of `IAbstractGraphType`, you will need to implement these new methods and property. Most users who inherit from `InterfaceGraphType` or `UnionGraphType` will not be affected as these base classes already provide the implementations.

### 4. `ParseLinkVisitor.Run` method removed

The `ParseLinkVisitor.Run` method has been removed. However, no changes should be required in your code since an equivalent extension method `Run` already exists for all `ISchemaNodeVisitor` instances, including `ParseLinkVisitor`.

### 5. Async suffix removal for type-first field names

In type-first GraphQL schemas, field names ending with "Async" are now automatically removed for methods returning `ValueTask<T>` and `IAsyncEnumerable<T>`, consistent with the existing behavior for `Task<T>`.

For example, previously only `Task<string> GetDataAsync()` would become field `"GetData"`, while `ValueTask<string> GetDataAsync()` and `IAsyncEnumerable<int> GetItemsAsync()` would keep their "Async" suffix. Now all three async return types have consistent field naming.

If you have type-first schemas with `ValueTask<T>` or `IAsyncEnumerable<T>` methods ending in "Async", update your GraphQL queries to use the new field names without the "Async" suffix, or use the `[Name]` attribute to explicitly specify the desired field name.

### 6. `ISchema.ResolveFieldContextAccessor` property added

The `ResolveFieldContextAccessor` property has been added to the `ISchema` interface. This property was previously only available on the `Schema` class. If you have custom implementations of `ISchema`, you will need to implement this property. Most users who inherit from `Schema` will not be affected as the base class already provides the implementation.

### 7. `SchemaTypes.ApplyMiddleware` moved to `SchemaTypesExtensions`

The `ApplyMiddleware` method has been moved from the `SchemaTypes` class to the `SchemaTypesExtensions` static class.

### 8. Schema initialization logic rewritten

The schema initialization logic has been completely rewritten to improve type reference handling. The updated `SchemaTypes` class provides stricter duplicate type prevention and improved exception messages. The previous implementation is available as `LegacySchemaTypes` for backwards compatibility.

Most users require no changes. Note that the order in which types are added to the schema has changed, which may affect introspection query results if you rely on a specific type ordering. Additionally, `GlobalSwitches.TrackGraphTypeInitialization` has been removed, but exception messages have been improved to provide better diagnostics.

If you have a custom `SchemaTypes` implementation or need the legacy behavior, override `CreateSchemaTypes()` in your schema:

```csharp
protected override SchemaTypesBase CreateSchemaTypes()
{
    var graphTypeMappingProviders = this.GetService<IEnumerable<IGraphTypeMappingProvider>>();
    return new LegacySchemaTypes(this, this, graphTypeMappingProviders, OnBeforeInitializeType);
}
```

### 9. `ISchema.AllTypes` and `Schema.AllTypes` return type changed

The `AllTypes` property on both `ISchema` and `Schema` now returns `SchemaTypesBase` instead of `SchemaTypes`. `SchemaTypesBase` is now the base class, and a new `SchemaTypes` class now inherits from `SchemaTypesBase`.

This change allows for better extensibility and provides a clearer separation between the base functionality and the concrete implementation. The `SchemaTypesBase` class exposes the same public API as `SchemaTypes` did before, so most code should continue to work without changes.

### 10. `FieldBuilder.Argument<T>` nullable parameter changed to `bool?`

The `nullable` parameter in the `Argument<T>` method has changed from `bool` (default `false`) to `bool?` (default `null`) to support automatic nullable value type detection. This change is generally source-compatible and should not require changes to user code. See the New Features section above for more details.

### 11. `SchemaExporter` now honors `Schema.Comparer` for sorting

The `SchemaExporter` class (used by `schema.ToAST()` and `schema.Print()`) now respects the `Schema.Comparer` property when exporting the schema to an AST. This means that if you have set a custom comparer on your schema (such as `AlphabeticalSchemaComparer`), the exported schema will be sorted according to that comparer.

Previously, the schema elements (types, fields, arguments, enum values, directives) were exported in their natural order regardless of the `Schema.Comparer` setting. Now they will be sorted if a comparer is configured.

This change also affects the default Federation SDL request, which uses `schema.Print()` internally. If you have set a custom comparer on your federated schema, the SDL response will now be sorted according to that comparer.

### 12. `ISchemaComparer.DirectiveArgumentComparer` method added

The `ISchemaComparer` interface has been extended with a new `DirectiveArgumentComparer(Directive)` method for sorting directive arguments during schema export and introspection. This is similar to the existing `ArgumentComparer(IFieldType)` method for field arguments.

If you have a custom implementation of `ISchemaComparer`, you will need to implement this new method. Most users who use the built-in `DefaultSchemaComparer` or `AlphabeticalSchemaComparer` will not be affected.

Note that `AlphabeticalSchemaComparer` will now sort directive arguments alphabetically for introspection queries and when using `ToAST()` or `Print()`. To revert this behavior and keep directive arguments in their natural order while maintaining alphabetical sorting for other schema elements, derive from `AlphabeticalSchemaComparer` and override `DirectiveArgumentComparer`, returning `null`:

```csharp
public class CustomSchemaComparer : AlphabeticalSchemaComparer
{
    public override IComparer<QueryArgument>? DirectiveArgumentComparer(Directive directive) => null;
}
```
