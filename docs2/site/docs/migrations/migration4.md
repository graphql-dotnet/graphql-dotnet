# Migrating from v3.x to v4.x

## New Features

### Improved Performance

GraphQL.NET 4.0 has been highly optimized, typically executing queries at least 50% faster while also providing
a 75% memory reduction. Small queries have been measured to run twice as fast as they previously ran. A cached
query executor is also provided, which can reduce execution time another 20% once the query has been parsed
(disabled by default). Variable parsing is also improved to run about 50% faster, and schema build time is
now about 20x faster than previously and requires 1/25th the amount of memory.

See the [Document Caching](https://graphql-dotnet.github.io/docs/guides/document-caching) guide to enable
document caching.

To facilitate the performance changes, many changes were made to the API that may affect you if you have
built custom execution strategies, scalars, parser, or similar core components. Please see the complete list
of breaking changes below.

### Input Object Custom Deserializers (aka resolver)

You can now add code to `InputObjectGraphType` descendants to build an object from the collected argument
fields. The new `ParseDictionary` method is called when variables are being parsed or `GetArgument` is called,
depending on if the argument is stored within variables or as a literal. The method is passed a dictionary
containing the input object's fields and deserialized values.

By default, for `InputObjectGraphType<TSourceType>` implementations, the dictionary is passed to
`ObjectExtensions.ToObject` in order to convert the dictionary to an object of `TSourceType`.
You can override the method to have it return an instance of any appropriate type.

Below is a sample which sets a default value for an unsupplied field (this could be done with a default
value set on the field, of course) and converts the name to uppercase:

```csharp
public class HumanInputType : InputObjectGraphType
{
    public HumanInputType()
    {
        Name = "HumanInput";
        Field<NonNullGraphType<StringGraphType>>("name");
        Field<StringGraphType>("homePlanet");
    }

    public override object ParseDictionary(IDictionary<string, object> value)
    {
        return new Human
        {
            Name = ((string)value["name"]).ToUpper(),
            HomePlanet = value.TryGetValue("homePlanet", out var homePlanet) ? (string)homePlanet : "Unknown",
            Id = null,
        };
    }
}
```

Note that pursuant to GraphQL specifications, if a field is optional, not supplied, and has no default,
it will not be in the dictionary.

For untyped `InputObjectGraphType` classes, like shown above, the default behavior of `ParseDictionary`
will be to return the dictionary. `GetArgument<T>` will still attempt to convert a dictionary to the
requested type via `ObjectExtensions.ToObject` as it did before.

### Experimental Features / Applied Directives

In v4 we added ability to apply directives to the schema elements and expose user-defined meta-information
via introspection. This was one of the most requested features not only in GraphQL.NET, but in the entire
GraphQL ecosystem as a whole. See the [Directives](https://graphql-dotnet.github.io/docs/getting-started/directives)
documentation page which describes the new features in detail.

### Microsoft-specific Dependency Injection Extensions

If you are using the `Microsoft.Extensions.DependencyInjection` package, extension methods are provided within
the [GraphQL.MicrosoftDI NuGet package](https://www.nuget.org/packages/GraphQL.MicrosoftDI) for creating a service
provider scope during a field resolver's execution. This is useful when accessing a scoped service with a parallel
execution strategy, as typically scoped services are not multi-threaded compatible. The library also provides a
builder to assist constructing a field resolver that relies on scoped services. Below is a sample of a field resolver
that relies on a scoped service and can run concurrently with other field resolvers:

```csharp
public class MyGraphType : ObjectGraphType<Category>
{
    public MyGraphType()
    {
        Field("Name", context => context.Source.Name);
        Field<ListGraphType<ProductGraphType>>().Name("Products")
            .Resolve()
            .WithScope()
            .WithService<MyDbContext>()
            .ResolveAsync((context, db) => db.Products.Where(x => x.CategoryId == context.Source.Id).ToListAsync());
    }
}
```

See [Dependency Injection](https://graphql-dotnet.github.io/docs/getting-started/dependency-injection) for more details.

### Ability to Sort Introspection Results

Introspection results are now sorted based on a configured 'comparer' for a schema. You can configure the comparer
by setting `ISchema.Comparer` to an implementation of `ISchemaComparer`. By default, introspection results are
returned in the order they were defined.

See [Default Sort Order of Introspection Query Results](#Default-Sort-Order-of-Introspection-Query-Results) below for a sample
of how this can be used to return introspection results that are sorted alphabetically.

### Array Pooling

When returning lists of information from field resolvers, you can choose to rent an array from `IResolveFieldContext.ArrayPool`,
populating it with your results and returning the array. The array will be released after the execution completes. This has
limited uses, since the rented array is not guaranteed to be exactly the requested length, so the array would need to be
wrapped in order to only return the correct number of entries, triggering a memory allocation (albeit a smaller one):

```csharp
 resolve: context =>
{
    var ints = context.ArrayPool.Rent<int>(1000); // ints.Length >= 1000
    for (int i=0; i<1000; ++i)
        ints[i] = i;
    return ints.Constrained(1000); // extension method to return an array or array-like object of a given length
});
```

It is not recommended to use this feature for interim calculations, as it is better to work directly with `System.Buffers.ArrayPool<T>`.

### Global Switches

`GraphQL.GlobalSwitches` is a new static class with properties that affect the schema build process:

- `EnableReadDefaultValueFromAttributes` enables or disables setting default values for 'defaultValue' from `DefaultValueAttribute`. Enabled by default.
- `EnableReadDeprecationReasonFromAttributes` enables or disables setting default values for 'deprecationReason' from `ObsoleteAttribute`. Enabled by default.
- `EnableReadDescriptionFromAttributes` enables or disables setting default values for 'description' from `DescriptionAttribute`. Enabled by default.
- `EnableReadDescriptionFromXmlDocumentation` enables or disables setting default values for 'description' from XML documentation. Disabled by default.
- `Validation` configures the validator used when setting the `Name` property on types, arguments, etc. Can be used to disable validation
  when the configured `INameConverter` fixes up invalid names. See `ISchema.NameConverter`.
- `ValidationOnSchemaInitialize` configures the validator used to verify the schema after the `INameConverter` has processed all the names.
  Disabling this validator is unlikely to be of any use, since the parser will not be able to parse a document that contains invalid characters in a name.

It is recommended to configure these options once when your application starts, such as within your `void Main()` method, a static
constructor of your schema, or a similar location.

### Authorization Extension Methods

Historically, there are two repositories in [graphql-dotnet](https://github.com/graphql-dotnet) org that provide APIs for configuring
authorization requirements.

| Name | Package | Description |
|------|---------|-------------| 
| [server](https://github.com/graphql-dotnet/server) | [GraphQL.Server.Authorization.AspNetCore](https://www.nuget.org/packages/GraphQL.Server.Authorization.AspNetCore) | Integration of GraphQL.NET validation subsystem into ASP.NET Core |
| [authorization](https://github.com/graphql-dotnet/authorization) | [GraphQL.Authorization](https://www.nuget.org/packages/GraphQL.Authorization) | A toolset for authorizing access to graph types for GraphQL.NET |

Authorization itself is not a specific part of the GraphQL.NET repository, so it was quite natural to keep this functionality
in separate repositories. However, this resulted in some code duplication between repositories. In addition, there was constant
confusion about which of the two projects to use. In v4, we began the process of converging the two projects to a common denominator.
Extension methods (see `AuthorizationExtensions`) to configure authorization requirements for GraphQL elements (types, fields, schema)
were moved to GraphQL.NET repository. These methods will be removed from their respective projects after v4 release.

GraphQL.NET will not receive new dependencies, since all methods just read or write meta information. Calling code changes not required.

### Other Features

* New method `IParentExecutionNode.ApplyToChildren`
* New property `IResolveFieldContext.Parent`
* `IExecutionStrategy.ShouldIncludeNode` and ability to control the set of fields that the strategy executes
* Schema validation upon initialization and better support for schema traversal via `ISchemaNodeVisitor`

## Breaking Changes

### Schema Configuration Options Moved

`NameConverter`, `SchemaFilter` and `FieldMiddleware` have been removed from `ExecutionOptions` and are now properties on `Schema`.
These properties can be set in the constructor of the `Schema` instance, or within your DI composition root, or at any time before
any query is executed. Once a query has been executed, changes to these fields is not allowed, and adding middleware via the field middleware
builder has no effect.

### Middleware Builders

- The signature of `IFieldMiddlewareBuilder.Use` has been changed to remove the schema from delegate. Since the schema is now known, there is no
  need for it to be passed to the middleware builder.
- The middleware `Use<T>` extension method has been removed. Please use the `Use` method with a middleware instance instead.

See [Field Middleware](https://graphql-dotnet.github.io/docs/getting-started/field-middleware) for more information.

### Dependency Injection / GetRequiredService

`GraphQL.Utilities.ServiceProviderExtensions` has been made internal. This affects usages of its extension method `GetRequiredService`.
Instead, reference the `Microsoft.Extensions.DependencyInjection.Abstractions` NuGet package and use the extension method from the
`Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions` class.

### Default Sort Order of Introspection Query Results

By default fields returned by introspection query are no longer sorted by their names. `LegacyV3SchemaComparer` can be used to switch to the old behavior.

```csharp
/// <summary>
/// Default schema comparer for GraphQL.NET v3.x.x.
/// By default only fields are ordered by their names within enclosing type.
/// </summary>
public sealed class LegacyV3SchemaComparer : DefaultSchemaComparer
{
    private static readonly FieldByNameComparer _instance = new FieldByNameComparer();

    private sealed class FieldByNameComparer : IComparer<IFieldType>
    {
        public int Compare(IFieldType x, IFieldType y) => x.Name.CompareTo(y.Name);
    }
    /// <inheritdoc/>
    public override IComparer<IFieldType> FieldComparer(IGraphType parent) => _instance;
}

schema.Comparer = new LegacyV3SchemaComparer();
```

### `IResolveFieldContext` Re-use

The `IResolveFieldContext` instance passed to field resolvers is re-used at the completion of the resolver. Be sure not to
use this instance once the resolver finishes executing. To preserve a copy of the context, call `.Copy()` on the context
to create a copy that is not re-used. Note that it is safe to use the field context within asynchronous field resolvers and
data loaders. Once the asynchronous field resolver or data loader returns its final result, the context will be cleared and may be re-used.
Also, any calls to the configured `UnhandledExceptionDelegate` will receive a field context copy that will not be re-used,
so it is safe to preserve these instances without calling `.Copy()`.

### Subscriptions Moved to Separate Project

The implementation for subscriptions, contained within `SubscriptionExecutionStrategy`, has been moved into the
[`GraphQL.SystemReactive`](https://www.nuget.org/packages/GraphQL.SystemReactive/) NuGet package. The default document executer
will now throw a `NotSupportedException` when attempting to execute a subscription. Please import the NuGet package and use the
`SubscriptionDocumentExecuter` instead. If you have a custom document executer, you can override `SelectExecutionStrategy` in
order to select the `SubscriptionExecutionStrategy` instance for subscriptions.

```csharp
protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
{
    return context.Operation.OperationType switch
    {
        OperationType.Subscription => SubscriptionExecutionStrategy.Instance,
        _ => base.SelectExecutionStrategy(context)
    };
}
```

### `ExecutionOptions.EnableMetrics` is disabled by default

To enable metrics, please set the option to `true` before executing the query.

```csharp
var result = await schema.ExecuteAsync(options =>
{
    options.Query = "{ hero { id name } }";
    options.EnableMetrics = true;
});
```

### GraphQL Member Descriptions

To improve performance, by default GraphQL.NET 4.0 does not pull descriptions for types/fields/etc from XML comments as it
did in 3.x. To re-enable that functionality, see [Global Switches](#Global-Switches) above.

### Changes to `IResolveFieldContext.Arguments`

`IResolveFieldContext.Arguments` now returns an `IDictionary<string, ArgumentValue>` instead of `IDictionary<string, object>` so that it
can be determined if the value returned is a default value or if it is a specified literal or variable.

`IResolveFieldContext.HasArgument` now returns `false` when `GetArgument` returns a field default value. Note that if a variable is specified,
and the variable resolves to its default value, then `HasArgument` returns `true` (since the field argument has successfully resolved to a variable
specified by the query).

### Metadata is Not Thread Safe

`IProvideMetadata.Metadata` is now a `Dictionary<string, object>` instead of `ConcurrentDictionary<string, object>`, and is not thread safe anymore.
If you need to write metadata during execution of field resolvers, lock on the graph type before accessing the dictionary. Do not lock on the
`Metadata` property because there can be concurrency issues accessing the field.

```csharp
lock (field)
{
    int value;
    if (field.Metadata.TryGetValue("counter", out var valueObject)) value = (int)valueObject;
    field.Metadata["counter"] = value + 1;
}
```

### Ability to map CLR types to GraphTypes

Strictly speaking, this feature was available before via `GraphTypeTypeRegistry`, but it had a significant
drawbacks, since the mapping was static and did not allow registering the same CLR type both as input and output.
In v4 `GraphTypeTypeRegistry` was completely removed and the `ISchema.RegisterTypeMapping(Type, Type)`
method was added instead (also there are several extension methods).

Consider the following example:

```csharp
public class Money
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}

public class Account
{
    public Money Saldo { get; set; }
}

public class MoneyType : ObjectGraphType<Money>
{
    public MoneyType()
    {
        Field(x => x.Amount);
        Field(x => x.Currency);
    }
}

public class AccountType : ObjectGraphType<Account>
{
    public MoneyType()
    {
        Field(x => x.Saldo);
    }
}
```

On the `Field(x => x.Saldo)` line when parsing an expression GraphQL.NET should somehow infer
that the `Money` CLR type corresponds to the `MoneyType` GraphType. In fact, this cannot be done
without specifying additional information from the caller. GraphQL.NET can only infer some primitive
CLR types (`int`, `string`, `DateTime`, `Guid`, etc.) that match built-in scalars.

Type registration is used for the hint:

```csharp
GraphTypeTypeRegistry.Register<Money, MoneyType>(); // static API before v4
schema.RegisterTypeMapping<Money, MoneyType>();     // instance method on `ISchema` after v4
```

Note that since v4 it's possible to register both input and output GraphType for the same CLR type.
In this case, GraphQL.NET will choose the desired GraphType depending on the context.

```csharp
public class Money
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}

public class MoneyType : ObjectGraphType<Money>
{
    public MoneyType()
    {
        Field(x => x.Amount);
        Field(x => x.Currency);
    }
}

public class MoneyInputType : InputObjectGraphType<Money>
{
    public MoneyInputType()
    {
        Field(x => x.Amount).Description("Total amount").DefaultValue(100m);
        Field(x => x.Currency).DefaultValue("USD");
    }
}

schema.RegisterTypeMapping<Money, MoneyType>();
schema.RegisterTypeMapping<Money, MoneyInputType>();
```

An alternative way to define the mapping is to use the new properties in the `GraphQLMetadata` attribute.
Consider the following example:

```c#
[GraphQLMetadata(InputType = typeof(FilterInputGraphType))]
public class Filter
{
    public string Key { get; set; }
    public int Value { get; set; }
}

public class ContainerRequest
{
    public IList<Filter> Filters { get; set; }
    public int ClientId { get; set; }
    public int AppId { get; set; }
}

public class FilterInputGraphType : InputObjectGraphType<Filter>
{
    public FilterInputGraphType()
    {
        Name = "FilterInput";
        Field(x => x.Key);
        Field(x => x.Value);
    }
}

public class MyInputType : InputObjectGraphType<ContainerRequest>
{
    public MyInputType()
    {
        Name = "Input";
        Field(x => x.Filters); // when building this field, its type is implicitly inferred to list of FilterInputGraphType
        Field(x => x.ClientId);
        Field(x => x.AppId, nullable: true);
    }
}
```

In this case, a call to the registration method is not required, since the schema
will use information from the provided attribute.

> Keep in mind that you can register type mappings even for built-in/primitive types if you want to change their behavior:
> <br/><br/>
> schema.RegisterTypeMapping<int, MyIntGraphType>()
> <br/>
> schema.RegisterTypeMapping<string, MySpecialFormattedStringGraphType>()

### API Cleanup

* `GraphQL.Instrumentation.StatsReport` and its associated classes have been removed. Please copy the source code into
  your project if you require these classes.
* `LightweightCache.First` method has been removed.
* `IGraphType.CollectTypes` method has been removed.
* `ExecutionHelper.SubFieldsFor` method has been removed.
* `NodeExtensions`, `AstNodeExtensions` classes have been removed.
* `CoreToVanillaConverter` class became `static` and most of its members have been removed.
* `GraphQL.Language.AST.Field.MergeSelectionSet` method has been removed.
* `CoreToVanillaConverter.Convert` method now requires only one `GraphQLDocument` argument.
* `GraphTypesLookup` has been renamed to `SchemaTypes` with a significant decrease in public APIs 
* `TypeCollectionContext` class is now internal, also all methods with this parameter in `GraphTypesLookup` (now `SchemaTypes`) are private.
* `GraphQLTypeReference` class is now internal, also `GraphTypesLookup.ApplyTypeReferences` is now private.
* `IHaveDefaultValue.Type` has been moved to `IProvideResolvedType.Type`
* `ErrorLocation` struct became `readonly`.
* `DebugNodeVisitor` class has been removed.
* Most methods and classes within `OverlappingFieldsCanBeMerged` are now private.
* `EnumerableExtensions.Apply` for dictionaries has been removed.
* `ISubscriptionExecuter` has been removed.
* `EnterLeaveListener` has been removed and the signatures of `INodeVisitor.Enter` and `INodeVisitor.Leave` have
  changed. `NodeVisitors` class has been added in its place.
* `TypeInfo.GetAncestors()` has been changed to `TypeInfo.GetAncestor(int index)`
* Various methods within `StringUtils` have been removed; please use extension methods within `StringExtensions` instead.
* `ExecutionHelper.GetVariableValue` has been removed, and the signature for `ExecutionHelper.CoerceValue` has changed.
* Removed `TypeExtensions.As`
* `ExecutionHelper.CollectFields` method was moved into `Fields` class and renamed to `CollectFrom`
* `ISchema.FindDirective`, `ISchema.RegisterDirective`, `ISchema.RegisterDirectives` methods were moved into `SchemaDirectives` class
* `ISchema.FindType` method was moved into `SchemaTypes[string typeName]` indexer
* Some of the `ISchemaNodeVisitor` methods have been changes to better support schema traversal
* `SourceLocation`, `NameNode` and `BasicVisitor` have been changed to a `readonly struct`.
* `ObjectExtensions.GetInterface` has been removed along with two overloads of `GetPropertyValue`.
* `void INode.Visit<TState>(System.Action<INode, TState> action, TState state)` method has been added.
* Various `IEnumerable<T>` properties on schema and graph types have been changed to custom collections:
  `SchemaDirectives`, `SchemaTypes`, `TypeFields`, `PossibleTypes`, `Interfaces` and `ResolvedInterfaces`
* `INode.IsEqualTo` and related methods have been removed.
* `ApolloTracing.ConvertTime` is now private and `ResolverTrace.Path` does not initialize an empty list when created.
* `SchemaBuilder.RegisterType` and `SchemaBuilder.RegisterTypes` methods have been removed, use `ISchema.RegisterType` on the builded schema instead.
* `SchemaBuilder.Directives` and `SchemaBuilder.RegisterDirectiveVisitor` have been removed, use `ISchema.RegisterVisitor` on the builded schema instead.

### Other Breaking Changes (including but not limited to)

* GraphQL.NET now uses GraphQL-Parser v7 with new memory model taking advantage of `System.Memory` APIs.
* When used, Apollo tracing will now convert the starting timestamp to UTC so that `StartTime` and `EndTime` are properly serialized as UTC values.
* `Connection<TNode, TEdge>.TotalCount` has been changed from an `int` to an `int?`. This allows for returning `null` indicating that the total count is unknown.
* `InputObjectGraphType.ParseDictionary` has been added so that customized deserialization behavior can be specified for input objects (aka input resolvers).
  If `InputObjectGraphType<T>` is used, and `GetArgument<T>` is called with the same type, no behavior changes will occur by default.
  If `InputObjectGraphType<T>` is used, but `GetArgument<T>` is called with a different type, coercion may fail. Override `ParseDictionary`
  to force resolving the input object to the correct type. See [Input Object Custom Deserializers](#Input-Object-Custom-Deserializers-aka-resolver) above.
* `ExecutionResult.Data` format breaking changes.
  Both `GraphQL.NewtonsoftJson` and `GraphQL.SystemTextJson` serializers received the necessary changes to produce the same JSON as before.
  However, consumers using `ExecutionResult` instances directly most likely will not work correctly.
* Most `ExecutionStrategy` methods are now `protected`
* `ObjectExecutionNode.SubFields` property type was changed from `Dictionary<string, ExecutionNode>` to `ExecutionNode[]`
* `ExecutionNode.IsResultSet` has been removed
* `ExecutionNode.Source` is read-only; additional derived classes have been added for subscriptions
* `NameValidator.ValidateName` and `NameValidator.ValidateNameOnSchemaInitialize` accept an enum instead of a string for their second argument
* `ExecutionNode.PropagateNull` must be called before `ExecutionNode.ToValue`; see reference implementation
* `IDocumentValidator.ValidateAsync` does not take `originalQuery` parameter; use `Document.OriginalQuery` instead
* `IDocumentValidator.ValidateAsync` now returns `(IValidationResult validationResult, Variables variables)` tuple instead of single `IValidationResult` before
* `IResolveFieldContext.FieldName` and `IResolveFieldContext.ReturnType` properties have been removed, use `IResolveFieldContext.FieldAst.Name` and `IResolveFieldContext.FieldDefinition.ResolvedType` instead
* `GraphQLMetadataAttribute.Type` property was renamed to `ResolverType`
