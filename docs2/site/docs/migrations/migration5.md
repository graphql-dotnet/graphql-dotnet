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
| `string?`         | `IdGraphType`         |
| `string`          | `NonNullGraphType<IdGraphType>` |
| `List<int>`       | `NonNullGraphType<ListGraphType<NonNullGraphType<IdGraphType>>>` |

Custom attributes can also be added to perform the following behavior changes:
- Override detected underlying CLR type
- Override detected nullability or list nullability state
- Override chosen underlying graph type

#### Method and argument support for `AutoRegisteringObjectGraphType<T>` instances

Methods will be detected and added to fields on the graph. Asynchronous methods
and data loader methods are supported such as shown in the below example:

| CLR type                     | Graph type                          |
|------------------------------|-------------------------------------|
| `Task<string>`               | `NonNullGraphType<StringGraphType>` |
| `IDataLoaderResult<Person?>` | `PersonGraphType`                   |

The above example assumes that the `Person` CLR type was mapped to `PersonGraphType`
in the schema CLR type mappings.

Arguments are also added as query arguments to the field, and recognize default values
and other attributes set on the parameter. Certain argument types are recognized and
treated special; special fields do not add a query argument to the field:

| Argument type                     | Value passed to the method   |
|-----------------------------------|------------------------------|
| `IResolveFieldContext`            | The field resolver's context; useful in advanced scenarios |
| `CancellationToken`               | The cancellation token from the resolve context |
| Any, tagged with `[FromServices]` | Pulls the service of the argument type from the `RequestServices` property of the resolve context |

Also note:
- Asynchronous methods that end in `Async` have the "Async" suffix removed from the default field name.
- Methods tagged with `[Scoped]` (when using `GraphQL.MicrosoftDI`) create a service scope for the field resolver's execution.

This allows for code such as the following:

```csharp
[Name("Person")]
public class Human
{
    [Id]
    public int Id { get; set; }

    public string Name { get; set; }

    [Name("Orders")]
    [Scoped]
    public async Task<IEnumerable<Order>> GetOrdersAsync(
        [FromServices] MyDbContext db,
        CancellationToken token,
        [Name("Sort")] SortOrder sortOrder = SortOrder.Date)
    {
        var query = db.Orders.Where(x => x.HumanId == Id);

        if (sortOrder == SortOrder.Date)
            query = query.OrderByDesc(x => x.OrderDate);

        return query.ToListAsync(token);
    }
}

public enum SortOrder
{
    Date
}
```

The above code would generate a GraphQL schema like this:

```graphql
type Person {
  id: ID!
  name: String!
  orders(sort: SortOrder! = DATE): [Order!]!
}
```

#### CLR field support

CLR fields are not automatically added to graph types, but can be added by overriding
the `GetRegisteredMembers` method of a `AutoRegisteringObjectGraphType<T>` or
`AutoRegisteringInputObjectGraphType<T>` instance.

#### Overridable base functionality

The classes can be overridden, providing the ability to customize behavior of automatically
generated graph types. For instance, to exclude properties of a certain type, you could write this:

```csharp
private class CustomAutoObjectType<T> : AutoRegisteringObjectGraphType<T>
{
    protected override IEnumerable<FieldType> ProvideFields()
    {
        var props = GetRegisteredProperties();
        foreach (var prop in props)
        {
            if (prop.PropertyType != typeof(MyType))
                yield return CreateField(prop);
        }
    }
}
```

Similarly, by overriding `CreateField` you can change the default name, description,
graph type, or other information applied to each generated field.

Most of these changes can be performed declaratively by attributes, but by creating a derived class you can
change default behavior imperatively without needing to add attributes to all of your data models.

These `protected` methods can be overridden to provide the following customizations to
automatically-generated graph types:

| Method           | Description                                                          | Typical use      |
|------------------|----------------------------------------------------------------------|------------------|
| (constructor)    | Configures graph properties and adds fields                          | Configuring graph after default initialization is complete |
| ConfigureGraph   | Configures default graph properties prior to applying attributes     | Applying a different default naming convention, such as appending "Input" or "Model" |
| GetRegisteredMembers | Returns the set of properties, methods and fields to be automatically configured | Filtering internal properties; sorting the property list; including fields; excluding methods |
| ProvideFields    | Returns a set of generated fields                                    | Adding additional fields to the generated set |
| CreateField      | Creates a `FieldType` from a `MemberInfo`                            | Applying custom behavior to field generation |
| GetTypeInformation | Parses a CLR type and NRT annotations to return a graph type       | Use a specific graph type for a certain CLR type |
| GetArgumentInformation | Parses a method argument to return a query argument or expression | Return an expression for specific types, such as a user context |

Note that if you override `GetRegisteredMembers` to include private properties or fields for
an input graph, you may also need to override `ParseDictionary` as well.

If you utilize dependency injection within your schema, you can register your custom graph
type to be used instead of the built-in type as follows:

```cs
services.AddSingleton(typeof(AutoRegisteringObjectGraphType<>), typeof(CustomAutoObjectType<>));
```

Then any graph type defined as `AutoRegisteringObjectGraphType<...>` will use your custom
type instead.

#### Graphs, fields and arguments recognize attributes to control initialization behavior

Any attribute that derives from `GraphQLAttribute`, such as `GraphQLAuthorizeAttribute`, can be set on a
CLR class or one if its properties, fields, methods or method arguments and is configured for the graph,
field type or query argument. New attributes have been updated or added for convenience as follows:

| Attribute            | Description        |
|----------------------|--------------------|
| `[Name]`             | Specifies a GraphQL name for a CLR class, member or method parameter |
| `[InputName]`        | Specifies a GraphQL name for an input CLR class, member or method parameter |
| `[OutputName]`       | Specifies a GraphQL name for an output CLR class or member |
| `[InputType]`        | Specifies a graph type for a field on an input model, or for a query argument |
| `[OutputType]`       | Specifies a graph type for a field on an output model |
| `[Ignore]`           | Indicates that a CLR member should not be mapped to a field |
| `[Metadata]`         | Specifies custom metadata to be added to the graph type, field or query argument |
| `[Scoped]`           | For methods, specifies to create a DI service scope during resolver execution |
| `[FromServices]`     | For method parameters, specifies that the argument value should be pulled from DI |
| `[FromSource]`       | For method parameters, specifies that the argument value should be the context 'Source' |
| `[FromUserContext]`  | For method parameters, specifies that the argument value should be the user context |
| `[GraphQLAuthorize]` | Specifies an authorization policy for the graph type for field |
| `[GraphQLMetadata]`  | Specifies name, description, deprecation reason, or other properties for the graph type or field |

Note: `[Scoped]` is provided through the GraphQL.MicrosoftDI NuGet package.

Custom attributes can be easily added to control any other initialization of graphs, fields or query arguments.

### 7. More strict behavior of FloatGraphType for special values

This is a spec-compliance issue (bug fix), that fixes parsing of Nan and -/+ Infinity values.
The spec says that:

> Non-finite floating-point internal values (NaN and Infinity) cannot be
> coerced to Float and must raise a field error.

### 8. Support for cancellation at validation stage

With new visitors design from GraphQL-Parser v8 it is possible now to cancel GraphQL request
at validation stage before actual execution. `DocumentExecuter` uses the same cancellation token
specified into `ExecutionOptions` to pass into `IDocumentValidator.ValidateAsync`.

### 9. `InputObjectGraphType` supports `ToAST`/`IsValidDefault`

`ToAST` is supported for `InputObjectGraphType` and enables printing a code-first schema that uses
`InputObjectGraphType` (`ToAST` threw `NotImplementedException` before), i.e. schemas with default
input objects can be printed out of the box now. `InputObjectGraphType.IsValidDefault` now checks
all fields on the provided input object value. To revert `IsValidDefault` to v4 behavior use that snippet:

```csharp
public override bool IsValidDefault(object value) => value is TSourceType;
```

### 10. `EnumerationGraphType<T>` enhancements

The following new attributes are detected on auto-generated enum graph types:

| Attribute     | Description                                        |
|---------------|----------------------------------------------------|
| `[Name]`      | Specifies the name of the enum type or value       |
| `[Ignore]`    | Does not add the enum value to the enum type       |
| `[Metadata]`  | Adds the specified metadata to enum type or value  |

As before, you can still use the `[Description]` or `[Obsolete]` attributes to add
descriptions or deprecation reasons to enum graph types or enum values.

You can also derive from `GraphQLAttribute` to create your own attributes to modify
enum graph types or enum values as they are being built by `EnumerationGraphType<T>`.

### 11. Ability to get directives and their arguments values

Now you may get directives along with their arguments that have been provided in the GraphQL query request.
New APIs are similar to ones used for field arguments:

```csharp
Field<StringGraphType>("myField", resolve: context =>
{
    var dir = ctx.GetDirective("myDirective");
    var arg = dir.GetArgument<string>("arg");
    ...
});
```

### 12. The `ExecutionStrategy` selected for an operation can be configured through `IGraphQLBuilder`

Previously, in order to change the execution strategy for a specific operation -- for instance,
using a serial execution strategy for 'query' operation types -- required creating a custom
document executer and overriding the `SelectExecutionStrategy` method.

Now, for DI configurations, you can call the `.AddExecutionStrategy<T>(OperationType)` method to
provide this configuration without overriding the method. See below for an example.

```csharp
// === GraphQL.NET v4 ===
public class SerialDocumentExecuter : DocumentExecuter
{
    public SerialDocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer, IDocumentCache documentCache, IEnumerable<IConfigureExecutionOptions> configurations)
        : base(documentBuilder, documentValidator, complexityAnalyzer, documentCache, configurations)
    {
    }

    protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
    {
        return context.Operation.Operation switch
        {
            OperationType.Query => SerialExecutionStrategy.Instance,
            _ => base.SelectExecutionStrategy(context)
        };
    }
}

// within Startup.cs
services.AddGraphQL()
    .AddSystemTextJson()
    .AddSchema<StarWarsSchema>()
    .AddDocumentExecuter<SerialDocumentExecuter>();


// === GraphQL.NET v5 ===

// within Startup.cs
services.AddGraphQL(builder => builder
    .AddSystemTextJson()
    .AddSchema<StarWarsSchema>()
    .AddExecutionStrategy<SerialExecutionStrategy>(OperationType.Query));
```

You can also register your own implementation of `IExecutionStrategySelector` which can inspect the
`ExecutionContext` to make additional decisions before selecting an execution strategy.

```csharp
public class MyExecutionStrategySelector : IExecutionStrategySelector
{
    public virtual IExecutionStrategy Select(ExecutionContext context)
    {
        return context.Operation.Operation switch
        {
            OperationType.Query => ParallelExecutionStrategy.Instance,
            OperationType.Mutation => SerialExecutionStrategy.Instance,
            OperationType.Subscription => throw new NotSupportedException(),
            _ => throw new InvalidOperationException()
        };
    }
}

// within Startup.cs
servcies.AddGraphQL(builder => builder
    // other configuration here
    .AddExecutionStrategySelector<MyExecutionStrategySelector>());
```

The `DocumentExecuter.SelectExecutionStrategy` method is still available to be overridden for
backwards compatibility but may be removed in the next major version.

### 13. Schema builder and `FieldDelegate` improvements for reflected methods

When configuring a CLR method for a field, the method arguments now allow the use of all of
the new attributes available to `AutoRegisteringObjectGraphType`, such as `[FromServices]`.
Field resolvers are now precompiled, resulting in faster performance.

As always, when the CLR type is not the source type, the CLR type is pulled from DI.
Now the CLR type will be pulled from `context.RequestServices` to allow for scoped instances.
If `RequestServices` is `null`, the root DI provider will be used as it was before.

Note that existing methods will require the use of `[FromSource]` and `[FromUserContext]` for
applicable method arguments.

```csharp
// v4
[GraphQLMetadata("Droid")]
class DroidType
{
    // DI-injected services are always pulled from the root DI provider, so scoped services are not supported
    private readonly Repository _repo;
    public DroidType(Repository repo)
    {
        _repo = repo;
    }

    public int Id(Droid source) => source.Id;

    public IEnumerable<Droid> Friends(Droid source) => _repo.FriendsOf(source.Id);
}

// v5
[GraphQLMetadata("Droid")]
class DroidType
{
    // scoped services are supported, so long as ExecutionOptions.RequestServices is set
    private readonly Repository _repo;
    public DroidType(Repository repo)
    {
        _repo = repo;
    }

    // requires use of [FromSource]
    public int Id([FromSource] Droid source) => source.Id;

    public IEnumerable<Droid> Friends([FromSource] Droid source) => _repo.FriendsOf(source.Id);
}

// v5 alternate
[GraphQLMetadata("Droid")]
class DroidType
{
    public int Id([FromSource] Droid source) => source.Id;

    // only inject Repository where needed
    public IEnumerable<Droid> Friends([FromSource] Droid source, [FromServices] Repository repo) => repo.FriendsOf(source.Id);
}
```

Similar changes may be necessary when using `FieldDelegate` to assign a field resolver.

### 14. ValueTask support

The execution pipeline has been changed to use `ValueTask` throughout. To support this change, the following
interfaces have been slightly changed to have methods with `ValueTask` signatures:

- `IFieldResolver`
- `IEventStreamResolver` (`IAsyncEventStreamResolver` has been removed)
- `IFieldMiddleware`

This will result in a substantial speed increase for schemas that use field middleware.

In addition, `ValueTask<T>` return types are supported for fields built on CLR methods via the schema builder,
fields built on CLR methods via `AutoRegisteringObjectGraphType`, and fields built on CLR methods via `FieldDelegate`.

When manually instantiating a field or subscription resolver, you may use a delegate that return a `ValueTask` by
using new constructors available on the `FuncFieldResolver` or `EventStreamResolver` classes.

### 15. `NameFieldResolver` enhanced method support

When adding a field by name only, such as `Field<StringGraphType>("Name");`, and the field matches a method
rather than a property on the source object, the method parameters are parsed similarly to `FieldDelegate`
as noted above with support for query arguments, `IResolveFieldContext`, `[FromServices]` and so on.

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

### 9. `ExecutionHelper.GetArgumentValues` was renamed to `GetArguments`

### 10. `DirectiveGraphType` was renamed to `Directive`

### 11. `schema`, `variableDefinitions` and `variables` arguments were removed from `ValidationContext.GetVariableValues`

Use `ValidationContext.Schema`, `ValidationContext.Operation.Variables` and `ValidationContext.Variables` properties

### 12. `ValidationContext.OperationName` was changed to `ValidationContext.Operation`

### 13. All arguments from `IDocumentValidator.ValidateAsync` were wrapped into `ValidationOptions` struct

### 14. All methods from `IGraphQLBuilder` were moved into `IServiceRegister` interface

Use `IGraphQLBuilder.Services` property if you need to register services into DI container.
If you use provided extension methods upon `IGraphQLBuilder` then your code does not require any changes.

### 15. Changes caused by GraphQL-Parser v8

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
- `ValidationContext.GetFragment` method was removed, use `ValidationContext.Document.FindFragmentDefinition`
- `IResolveFieldContext.SubFields` and `IExecutionStrategy.GetSubFields` returns dictionary with
   values of tuple of queried field and its field definition
- All scalars works with `GraphQLParser.AST.GraphQLValue` instead of `GraphQL.Language.AST.IValue`
- `IInputObjectGraphType.ToAST` returns `GraphQLParser.AST.GraphQLObjectValue` instead of `GraphQL.Language.AST.IValue`

### 16. Classes and members marked as obsolete have been removed

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

### 17. `IDocumentWriter` has been renamed to `IGraphQLSerializer` and related changes.

As such, the `DocumentWriter` classes have been renamed to `GraphQLSerializer`, and the
`AddDocumentWriter` extension method for `IGraphQLBuilder` has been renamed to `AddSerializer`.
The `WriteAsync` method's functionality has not changed.

### 18. Extension methods for parsing variables (e.g. `ToInputs`) have been removed.

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

The new `Read` and `Deserialize` methods of the `Newtonsoft.Json` implementation
will default to reading dates as strings unless configured otherwise in the settings.

### 19. The `WriteToStringAsync` extension methods have been removed.

Please use the `Serialize()` method of an `IGraphQLTextSerializer` implementation.
The asynchronous text serialization methods have been removed as the underlying serialization
providers execute synchronously when serializing to a string.

The `WriteAsync()` method can be used to asynchronously serialize to a stream. However,
the `Newtonsoft.Json` serializer does not support asynchronous serialization, so synchronous
calls are made to the underlying stream. Only `System.Text.Json` supports asynchronous writing.

### 20. Other changes to the serialization infrastructure

- `InputsConverter` renamed to `InputsJsonConverter`
- `ExecutionResultContractResolver` renamed to `GraphQLContractResolver`

### 21. `GraphQLMetadataAttribute` cannot be applied to graph type classes

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

### 22. `AstPrinter` class was removed

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

### 23. Possible breaking changes in `InputObjectGraphType<TSourceType>`

`InputObjectGraphType<TSourceType>.ToAST` and `InputObjectGraphType<TSourceType>.IsValidDefault`
methods were changed in such a way that now you may be required to also override `ToAST` if you override
`ParseDictionary`. Changes in those methods are made for earlier error detection and schema printing.

### 24. `AutoRegisteringObjectGraphType` changes

The protected method `GetRegisteredProperties` has been renamed to `GetRegisteredMembers`
and now supports properties, methods and fields, although fields are not included
with the default implementation. Override the method in a derived class to include fields.

New for v5, methods are included by default. To revert to v4 behavior, which does not
include methods, create a derived class as follows:

```csharp
public class AutoRegisteringObjectGraphTypeWithoutMethods<T> : AutoRegisteringObjectGraphType<T>
{
    public AutoRegisteringObjectGraphTypeWithoutMethods() : base() { }
    public AutoRegisteringObjectGraphTypeWithoutMethods(params Expression<Func<T, object?>>[]? excludedProperties) : base(excludedProperties) { }
    protected override IEnumerable<MemberInfo> GetRegisteredMembers() => base.GetRegisteredMembers().Where(x => x is PropertyInfo);
}
```

Register this class within your DI engine like this:

```cs
services.AddTransient(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphTypeWithoutMethods<>));
```

### 25. `AutoRegisteringInputObjectGraphType` changes

The protected method `GetRegisteredProperties` has been renamed to `GetRegisteredMembers`
and now supports returning both properties and fields, although fields are not included
with the default implementation. Override the method in a derived class to include fields.

### 26. `EnumerationGraphType` parses exact names

Consider GraphQL `enum Color { RED GREEN BLUE }` and corresponding `EnumerationGraphType`.
In v4 `ParseValue("rED")` yields internal value for `RED` name. In v5 this behavior was changed
and `ParseValue("rED")` throws error `Unable to convert 'rED' to the scalar type 'Color'`.

### 27. `EnumerationGraphType.AddValue` changes

`description` argument from `EnumerationGraphType.AddValue` method was marked as optional
and moved after `value` argument. If you use this method and set descriptions, you will need
to change the order of arguments. Since changing the order of arguments in some cases can remain
invisible to the caller and lead to hardly detected bugs, the method name has been changed from
`AddValue` to `Add`.

### 28. The settings class provided to `GraphQL.NewtonsoftJson.GraphQLSerializer` has changed.

Previously the settings class used was `Newtonsoft.Json.JsonSerializerSettings`. Now the class
is `GraphQL.NewtonsoftJson.JsonSerializerSettings`. The class inherits from the former class,
but sets the default date parsing behavior set to 'none'.

### 29. `SubscriptionDocumentExecuter` and `.AddSubscriptionDocumentExecuter()` have been deprecated.

While these can continue to be used for the lifetime of v5, it is now suggested to use the
`.AddSubscriptionExecutionStrategy()` builder method instead:

```csharp
// v4
services.AddGraphQL()
   .AddSchema<StarWarsSchema>()
   .AddSubscriptionDocumentExecuter();

// v5
services.AddGraphQL(builder => builder
   .AddSchema<StarWarsSchema>()
   .AddSubscriptionExecutionStrategy());
```

For more details on the new approach to execution strategy selection, please review the new feature above titled:

> The `ExecutionStrategy` selected for an operation can be configured through `IGraphQLBuilder`

### 30. Schema builder CLR types' method arguments require `[FromSource]` and `[FromUserContext]` where applicable

See New Features: 'Schema builder and `FieldDelegate` improvements for reflected methods' above.

### 31. FieldDelegate method arguments require `[FromSource]` and `[FromUserContext]` where applicable

See New Features: 'Schema builder and `FieldDelegate` improvements for reflected methods' above.

### 32. Code removed to support prior implementation of FieldDelegate and schema builder

The following classes and methods have been removed:

- The `EventStreamResolver` implementation which accepted an `IAccessor` as a construtor parameter.
- The `AsyncEventStreamResolver` implementation which accepted an `IAccessor` as a construtor parameter.
- The `DelegateFieldModelBinderResolver` class.
- The `ReflectionHelper.BuildArguments` method.

You may use the following classes and methods as replacements:

- The `MemberResolver` class is an `IFieldResolver` implementation for a property, method or field. Expressions are passed
  to the constructor for the instance (and if applicable, method arguments), which is immediately compiled.
- The `EventStreamMethodResolver` class is an `IEventStreamResolver` implementation for a method that returns an `IObservable<T>`
  or `Task<IObservable<T>>`. It also provides a basic `IFieldResolver` implementation for subscription fields.
- The `AutoRegisteringHelper.BuildFieldResolver` method builds a field resolver around a specifed property, method or field.
- The `AutoRegisteringHelper.BuildEventStreamResolver` method builds an event stream resolver around a specified method.

### 33. ValueTask execution pipeline support changes

The following interfaces have been modified to support a `ValueTask` pipeline:

- `IFieldResolver`
- `IEventStreamResolver`
- `IFieldMiddleware` and `FieldMiddlewareDelegate`

The following interfaces have been removed:

- `IAsyncEventStreamResolver`

All classes which implemented the above interfaces have been modified as necessary:

- `AsyncEventStreamResolver`
- `AsyncFieldResolver`
- `EventStreamResolver`
- `ExpressionFieldResolver`
- `FuncFieldResolver`
- `NameFieldResolver`
- `InstrumentFieldsMiddleware`

These properties have been removed:

- `EventStreamFieldType.AsyncSubscriber` (note: the `EventStreamFieldType` class was removed and the `Subscriber` property moved to the `FieldType` class)
- `FieldConfig.AsyncSubscriber`

Any direct implementation of these interfaces or classes derived from the above list will need to be modified to fit the new design.

In addition, it is required that any asynchronous fields must use an appropriate asynchronous field builder method or
asynchronous field resolver, and inferred methods (built by the schema builder, `FieldDelegate`, or `AutoRegisteringObjectGraphType`)
must be strongly typed.

```csharp
// works in v4, not in v5 (throws in runtime)
Field<CharacterInterface>("hero", resolve: context => data.GetDroidByIdAsync("3"));

// works in v4 or v5
FieldAsync<CharacterInterface>("hero", resolve: async context => await data.GetDroidByIdAsync("3"));


// works in v4, not in v5
AddField(new FieldType
{
    Name = "hero",
    Resolver = new FuncFieldResolver<Task<Droid>>(context => data.GetDroidByIdAsync("3")),
});

// works in v4 or v5
AddField(new FieldType
{
    Name = "hero",
    Resolver = new AsyncFieldResolver<Droid>(context => data.GetDroidByIdAsync("3")),
});


// works in v4, not in v5
Func<IResolveFieldContext, string, object> func = (context, id) => data.GetDroidByIdAsync(id);
FieldDelegate<DroidType>(
    "droid",
    arguments: new QueryArguments(
        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the droid" }
    ),
    resolve: func
);

// works in v4 or v5
Func<IResolveFieldContext, string, Task<Droid>> func = (context, id) => data.GetDroidByIdAsync(id);
FieldDelegate<DroidType>(
    "droid",
    arguments: new QueryArguments(
        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the droid" }
    ),
    resolve: func
);
```

### 34. `IResolveEventStreamContext` interface and `ResolveEventStreamContext` class removed

Please use the `IResolveFieldContext` interface and the `ResolveFieldContext` class instead. No other changes are required.

### 34. `EventStreamFieldType` class removed

Please use `FieldType` instead. The `Subscriber` property has been moved to the `FieldType` class so no other changes should be required.
The `AsyncSubscriber` property has been removed as described above.

### 35. `IEventStreamResolver<T>` interface removed

For custom resolver implementations, please implement `IEventStreamResolver` instead.

### 36. Asynchronous field resolver classes have been removed

These classes have been removed:

- `ScopedAsyncFieldResolver`
- `AsyncFieldResolver`
- `AsyncEventStreamResolver`

Please use the new `ValueTask`-based constructors on `ScopedFieldResolver`, `FuncFieldResolver` and `EventStreamResolver` instead.

```csharp
// v4
var resolver = new AsyncFieldResolver<string>(async context => await GetSomeString());

// v5
var resolver = new FuncFieldResolver<string>(async context => await GetSomeString());


// v4
Func<IResolveFieldContext, Task<string>> func = async context => await GetSomeString();
var resolver = new AsyncFieldResolver(func);

// v5 option 1
Func<IResolveFieldContext, ValueTask<string>> func = async context => await GetSomeString();
var resolver = new FuncFieldResolver(func);

// v5 option 2
Func<IResolveFieldContext, Task<string>> func = async context => await GetSomeString();
var resolver = new FuncFieldResolver(context => new ValueTask<string>(func(context)));

// v5 option 3
Func<IResolveFieldContext, Task<string>> func = async context => await GetSomeString();
var resolver = new FuncFieldResolver(async context => await func(context));
```

Field builder methods have not changed and still require a `Task<T>` return value for asynchronous field resolver delegates.

### 37. `NameFieldResolver` implementation supports methods with arguments; may cause `AmbigiousMatchException`

The `NameFieldResolver`, used when adding a field by name (e.g. `Field<StringGraphType>("Name");`),
now supports methods with arguments. During resolver execution, it first looks for a matching property
with the specified name, and if none is found, looks for a method with the matching name. Since it
now supports methods with arguments as well as methods without arguments, an `AmbigiousMatchException`
can occur if the name refers to a public method with multiple overloads. Either specify a field
resolver explicitly, or reduce the number of public methods with the same name to one.
