# Migrating from v4.x to v5.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A5.0+is%3Aissue+is%3Aclosed) and [pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A5.0+is%3Aclosed) done in v5.

## New Features

### 1. DoNotMapClrType attribute can now be placed on the graph type or the CLR type

When using the `.AddClrTypeMappings()` builder extension method, GraphQL.NET scans the
specified assembly for graph types that inherit from `ObjectGraphType<T>` and adds a
mapping for the CLR type represented by `T` with the graph type it matched upon.
It skips adding a mapping for any graph type marked with the `[DoNotMapClrType]` attribute.
In v5, it will also skip adding the mapping if the CLR type is marked with the
`[DoNotMapClrType]` attribute.

### 2. Input Extensions support

`Extensions` deserialized from GraphQL requests can now be set on the `ExecutionOptions.Extensions` property
and passed through to field resolvers via `IResolveFieldContext.InputExtensions`. Note that standard .NET
dictionaries (such as `Dictionary<TKey, TValue>`) are thread-safe for read-only operations. Also you can
access these extensions from validation rules via `ValidationContext.Extensions`.

### 3. Improved GraphQL-Parser

GraphQL.NET v5 uses GraphQL-Parser v8. This release brought numerous changes in the parser object model,
which began to better fit the [latest version](http://spec.graphql.org/October2021/) of the published
official GraphQL specification. GraphQL-Parser v8 has a lot of backward incompatible changes, but you are
unlikely to come across them if you do not use advanced features.

### 4. `IGraphQLSerializer` interface with JSON deserialization support

`IGraphQLSerializer.ReadAsync` is implemented by the `GraphQL.SystemTextJson` and
`GraphQL.NewtonsoftJson` libraries. It supports deserialization of any type, with
special support for the `GraphQLRequest` class. It also supports deserializing to
a `IList<GraphQLRequest>`, which will deserialize multiple requests or
a single request (with or without the JSON array wrapper) into a list.

When calling the `AddSystemTextJson` or `AddNewtonsoftJson` extension method to
the `IGraphQLBuilder` interface, the method will register the `IGraphQLSerializer`
and `IGraphQLTextSerializer` interfaces with the appropriate
serialization engine. These interfaces handle both serialization and deserialization
of objects.

This makes it so that you can write JSON-based transport code independent of the
JSON serialization engine used by your application, simplifying the most common use
case, while still being configurable through your DI framework.

You can also use `IGraphQLTextSerializer.ReadNode` to deserialize a framework-dependent
JSON element node, stored within an `object`, into a specific type. The specific
serialization engine you are using may have additional `Read` members as would be expected
for that library.

Be aware that `System.Text.Json` defaults to case-sensitive deserialization, while
`Newtonsoft.Json` defaults to case-insensitive deserialization. However, the supported
data models (such as `GraphQLRequest` and `OperationMessage`) will always deserialize
with case-sensitive camelCase deserialization. You can write your own data classes
which will behave in the default manner of the serializer's configuration. You can
configure the serializer to use camelCase for all properties by default. You can also
tag properties with serializer-specific attributes to change deserialization behavior,
such as adding a `JsonPropertyName` attribute to a data member to override its
serialized property name.

Specific support is provided for serializing and deserializing to the following data models:

| Class                   | Notes |
|-------------------------|-------|
| `ExecutionResult`       | Only serialization is supported |
| `GraphQLRequest`        | |
| `IList<GraphQLRequest>` | Other common collection variations, such as `IEnumerable<>` or `List<>`, are also supported |
| `OperationMessage`      | `Payload` is an `object` and can be deserialized to `GraphQLRequest` via `ReadNode` |
| `ApolloTrace`           | |
| `Inputs`                | |

Note that when deserializing a `IList<GraphQLRequest>`, and when the JSON data is a
single request rather than a list of requests, the request will be deserialized into
a list or array of a single item. For example, `{"query":"{ hero }"}` deserializes into
`new[] { new GraphQLRequest { Query = "{ hero }" }}`.

### 5. `IGraphQLTextSerializer` interface to support serialization to/from strings.

`IGraphQLTextSerializer.Serialize` and `IGraphQLTextSerializer.Deserialize` support
serializing objects to and from `string` values. For the `GraphQL.SystemTextJson`
and `GraphQL.NewtonsoftJson` libraries, these serialize and deserialize to JSON strings.

### 6. `AutoRegisteringObjectGraphType` and `AutoRegisteringInputObjectGraphType` enhancements

These two classes now provide a range of customizable behavior for data models without the
need for creating individual graph types for each data model. New for v5, fields' names
and graph types can be customized by applying attributes to the respective property, such
as is shown in the below example:

```csharp
// graph type: AutoRegisteringObjectGraphType<Person>

class Person
{
    [Name("Id")]
    [OutputType(typeof(IdGraphType))]
    public int PersonId { get; set; }

    public string Name { get; set; }

    [GraphQLAuthorize("Administrators")]
    public int Age { get; set; }

    [Description("Employee's job position")]
    public string? Title { get; set; }
}
```

#### Nullable reference type attribute interpretation

When the CLR type has nullable reference type annotations, these annotations
are read and interpreted by GraphQL.NET when constructing the graph type.
For instance, this is how the following CLR types are mapped to graph types
after schema initialization:

| CLR type          | Graph type            |
|-------------------|-----------------------|
| `string?`         | `StringGraphType`     |
| `string`          | `NonNullGraphType<StringGraphType>` |
| `List<int>`       | `NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>` |

CLR type mappings registered in the schema are supported as well.

In addition to the above, if the `[Id]` attribute is marked on the property,
it will override the interpreted graph type such as in the following examples:

| CLR type marked with `[Id]` | Graph type  |
|-------------------|-----------------------|
| `string?`         | `IdGraphType`     |
| `string`          | `NonNullGraphType<IdGraphType>` |
| `List<int>`       | `NonNullGraphType<ListGraphType<NonNullGraphType<IdGraphType>>>` |

Custom attributes can also be added to perform the following behavior changes:
- Override detected underlying CLR type
- Override detected nullability or list nullability state
- Override chosen underlying graph type

#### Overridable base functionality

The classes can be overridden, providing the ability to customize behavior of automatically
generated graph types. For instance, to exclude properties marked with a custom attribute
called `[InternalUse]` you could write this:

```csharp
private class CustomAutoObjectType<T> : AutoRegisteringObjectGraphType<T>
{
    protected override IEnumerable<FieldType> ProvideFields()
    {
        var props = GetRegisteredProperties();
        foreach (var prop in props)
        {
            if (prop.IsDefined(typeof(InternalUseAttribute)))
                yield return CreateField(prop);
        }
    }
}
```

Similarly, by overriding `CreateField` you can change the default name, description,
graph type, or other information applied to each generated field.

These `protected` methods can be overridden to provide the following customizations to
automatically-generated graph types:

| Method           | Description                                                          | Typical use      |
|------------------|----------------------------------------------------------------------|------------------|
| (constructor)    | Configures graph properties and adds fields                          | Configuring graph after default initialization is complete |
| ConfigureGraph   | Configures default graph properties prior to applying attributes     | Applying a different default naming convention, such as appending "Input" or "Model" |
| GetRegisteredProperties | Returns the set of properties to be automatically configured  | Filtering internal properties; sorting the property list |
| ProvideFields    | Returns a set of generated fields                                    | Adding additional fields to the generated set |
| CreateField      | Creates a `FieldType` from a `PropertyInfo`                          | Applying custom behavior to field generation |

If you utilize dependency injection within your schema, you can register your custom graph
type to be used instead of the built-in type as follows:

```cs
services.AddSingleton(typeof(AutoRegisteringObjectGraphType<>), typeof(CustomAutoObjectType<>));
```

Then any graph type defined as `AutoRegisteringObjectGraphType<...>` will use your custom
type instead.

#### Graphs and fields recognize attributes to control initialization behavior

Any attribute that derives from `GraphQLAttribute`, such as `GraphQLAuthorizeAttribute`, can be set on a
CLR class or one if its properties and is configured for the graph or field type. New attributes have been
updated or added for convenience as follows:

| Attribute            | Description        |
|----------------------|--------------------|
| `[Name]`             | Specifies a GraphQL name for a CLR class or property |
| `[InputName]`        | Specifies a GraphQL name for an input CLR class or property |
| `[OutputName]`       | Specifies a GraphQL name for an output CLR class or property |
| `[InputType]`        | Specifies a graph type for a field on an input model |
| `[OutputType]`       | Specifies a graph type for a field on an output model |
| `[Ignore]`           | Indicates that a CLR property should not be mapped to a field |
| `[Metadata]`         | Specifies custom metadata to be added to the graph type or field |
| `[GraphQLAuthorize]` | Specifies an authorization policy for the graph type for field |
| `[GraphQLMetadata]`  | Specifies name, description, deprecation reason, or other properties for the graph type or field |

Custom attributes can be easily added to control any other initialization of graphs or fields.

### 7. More strict behavior of FloatGraphType for special values

This is a spec-compliance issue (bug fix), that fixes parsing of Nan and -/+ Infinity values.
The spec says that:

> Non-finite floating-point internal values (NaN and Infinity) cannot be
> coerced to Float and must raise a field error.

### 8. Support for cancellation at validation stage

With new visitors design from GraphQL-Parser v8 it is possible now to cancel GraphQL request
at validation stage before actual execution. `DocumentExecuter` uses the same cancellation token
specified into `ExecutionOptions` to pass into `IDocumentValidator.ValidateAsync`.

## Breaking Changes

### 1. UnhandledExceptionDelegate

`ExecutionOptions.UnhandledExceptionDelegate` and `IExecutionContext.UnhandledExceptionDelegate`
properties type was changed from `Action<UnhandledExceptionContext>` to `Func<UnhandledExceptionContext, Task>`
so now you may use async/await for exception handling. In this regard, some methods in `ExecutionStrategy` were
renamed to have `Async` suffix.

### 2. `IDocumentCache` now has asynchronous methods instead of synchronous methods.

The default get/set property of the interface has been replaced with `GetAsync` and `SetAsync` methods.
Keys cannot be removed by setting a null value as they could before.

### 3. `IResolveFieldContext.Extensions` property renamed to `OutputExtensions` and related changes

To clarify and differ output extensions from input extensions, `IResolveFieldContext.Extensions`
has now been renamed to `OutputExtensions`. The `GetExtension` and `SetExtension` thread-safe
extension methods have also been renamed to `GetOutputExtension` and `SetOutputExtension` respectively.

### 4. `ExecutionOptions.Inputs` and `ValidationContext.Inputs` properties renamed to `Variables`

To better align the execution options and variable context with the specification, the `Inputs`
property containing the execution variables has now been renamed to `Variables`.

### 5. `ConfigureExecution` GraphQL builder method renamed to `ConfigureExecutionOptions`

Also, `IConfigureExecution` renamed to `IConfigureExecutionOptions`.

### 6. `AddGraphQL` now accepts a configuration delegate instead of returning `IGraphQLBuilder`

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

### 7. `GraphQLExtensions.BuildNamedType` was renamed and moved to `SchemaTypes.BuildGraphQLType`

### 8. `GraphQLBuilderBase.Initialize` was renamed to `RegisterDefaultServices`

### 9. `schema`, `variableDefinitions` and `variables` arguments were removed from `ValidationContext.GetVariableValues`

Use `ValidationContext.Schema`, `ValidationContext.Operation.Variables` and `ValidationContext.Variables` properties

### 10. `ValidationContext.OperationName` was changed to `ValidationContext.Operation`

### 11. All arguments from `IDocumentValidator.ValidateAsync` were wrapped into `ValidationOptions` class

### 12. All methods from `IGraphQLBuilder` were moved into `IServiceRegister` interface

Use `IGraphQLBuilder.Services` property if you need to register services into DI container.
If you use provided extension methods upon `IGraphQLBuilder` then your code does not require any changes.

### 13. Changes caused by GraphQL-Parser v8

- The `GraphQL.Language.AST` namespace and all classes from it have been removed in favor of ones
  from `GraphQLParser.AST` namespace in GraphQL-Parser project. Examples of changed usages:
  - `GraphQL.Language.AST.Document` -> `GraphQLParser.AST.GraphQLDocument`
  - `GraphQL.Language.AST.IValue` -> `GraphQLParser.AST.GraphQLValue`
  - `GraphQL.Language.AST.Field` -> `GraphQLParser.AST.GraphQLField`
  - `GraphQL.Language.AST.SelectionSet` -> `GraphQLParser.AST.GraphQLSelectionSet`
  - `GraphQL.Language.AST.IHaveDirectives` -> `GraphQLParser.AST.IHasDirectivesNode`
  - `GraphQL.Language.AST.IType` -> `GraphQLParser.AST.GraphQLType`
- Some APIs utilize `GraphQLParser.ROM` struct instead of `string`:
  - `ExecutionResult.Query`
  - `Metrics.SetOperationName`
  - `IComplexGraphType.GetField`
  - `QueryArguments.Find`
  - `SchemaDirectives.Find`
  - `SchemaDirectives.this[]`
  - `SchemaDirectives.Dictionary`
  - `ValidationContext.GetFragment`
  - All `ValidationError`'s constructors take _originalQuery_ as `ROM`
- `OperationType` and `DirectiveLocation` enums were removed, use enums from `GraphQLParser.AST` namespace
- `SourceLocation` struct was removed, use `GraphQLLocation` from `GraphQLParser.AST` namespace
- `CoreToVanillaConverter` class was removed
- `ErrorLocation` struct was removed, use `Location` from `GraphQLParser` namespace
- `IResolveFieldContext.SubFields` and `IExecutionStrategy.GetSubFields` returns dictionary with
   values of tuple of queried field and its field definition
- All scalars works with `GraphQLParser.AST.GraphQLValue` instead of `GraphQL.Language.AST.IValue`
- `IInputObjectGraphType.ToAST` returns `GraphQLParser.AST.GraphQLObjectValue` instead of `GraphQL.Language.AST.IValue`

### 14. Classes and members marked as obsolete have been removed

The following classes and members that were marked with `[Obsolete]` in v4 have been removed:

| Class or member | Notes |
|-----------------|-------|
| `GraphQL.NewtonsoftJson.StringExtensions.GetValue`     |                             |
| `GraphQL.NewtonsoftJson.StringExtensions.ToDictionary` | Use `Read` or `Deserialize` instead |
| `GraphQL.SystemTextJson.ObjectDictionaryConverter`     | Use `InputsJsonConverter` instead   |
| `GraphQL.SystemTextJson.StringExtensions.ToDictionary` | Use `Read` or `Deserialize` instead |
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

### 15. `IDocumentWriter` has been renamed to `IGraphQLSerializer` and related changes.

As such, the `DocumentWriter` classes have been renamed to `GraphQLSerializer`, and the
`AddDocumentWriter` extension method for `IGraphQLBuilder` has been renamed to `AddSerializer`.
The `WriteAsync` method's functionality has not changed.

### 16. Extension methods for parsing variables (e.g. `ToInputs`) have been removed.

Please use the `Read<Inputs>()` method of an `IGraphQLSerializer` implementation, or the
`Deserialize<Inputs>()` method of an `IGraphQLTextSerializer` implementation. Note that
these methods will return `null` if a null string or the string "null" is passed to them.
The `ExecutionOptions.Variables` property does not require `Inputs.Empty`, but if you have
tests based on the `.ToInputs()` extension method, you may want a direct replacement.
Equivalent code to the previous functionality is as follows:

```cs
using GraphQL;
using GraphQL.SystemTextJson;

public static class StringExtensions
{
    private static readonly GraphQLSerializer _serializer = new();

    public static Inputs ToInputs(string json)
        => json == null ? Inputs.Empty : _serializer.Deserialize<Inputs>(json) ?? Inputs.Empty;

    public static Inputs ToInputs(System.Text.Json.JsonElement element)
        => _serializer.ReadNode<Inputs>(element) ?? Inputs.Empty;

    public static T FromJson<T>(string json)
        => _serializer.Deserialize<T>(json);

    public static System.Threading.Tasks.ValueTask<T> FromJsonAsync<T>(this System.IO.Stream stream, System.Threading.CancellationToken cancellationToken = default)
        => _serializer.ReadAsync<T>(stream, cancellationToken);
}
```

### 17. The `WriteToStringAsync` extension methods have been removed.

Please use the `Serialize()` method of an `IGraphQLTextSerializer` implementation.
The asynchronous text serialization methods have been removed as the underlying serialization
providers execute synchronously when serializing to a string.

The `WriteAsync()` method can be used to asynchronously serialize to a stream. However,
the `Newtonsoft.Json` serializer does not support asynchronous serialization, so synchronous
calls are made to the underlying stream. Only `System.Text.Json` supports asynchronous writing.

### 18. Other changes to the serialization infrastructure

- `InputsConverter` renamed to `InputsJsonConverter`
- `ExecutionResultContractResolver` renamed to `GraphQLContractResolver`

### 19. `GraphQLMetadataAttribute` cannot be applied to graph type classes

The `[GraphQLMetadata]` attribute is designed to be used for schema-first configurations
and has not changed in this regard. For code-first graph definitions, please set the
GraphQL type name within the constructor.

```csharp
//[GraphQLMetadata("Person")] //previously supported
public class HumanType : ObjectGraphType<Human>
{
    public HumanType()
    {
        Name = "Person"; //correct implementation
        ...
    }
}
```

### 20. `AstPrinter` class was removed

`AstPrinter` class was removed in favor of `SDLPrinter` from GraphQL-Parser project.

Code before changes:

```csharp
INode node = ...;
string s = AstPrinter.Print(node);
```

Code after changes:

```csharp
ASTNode node = ...;
var writer = new StringWriter();
var printer = new SDLPrinter();
sdlPrinter.PrintAsync(node, writer).GetAwaiter().GetResult(); // actually is sync
string s = writer.ToString();
```

`SDLPrinter` is a highly optimized visitor for asynchronous non-blocking SDL output
into provided `TextWriter`. In the majority of cases it does not allocate memory in
the managed heap at all.
