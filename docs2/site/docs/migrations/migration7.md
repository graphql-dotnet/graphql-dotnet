# Migrating from v5.x to v7.x

Note that v6 was skipped to align GraphQL.NET version with versions of packages from [server](https://github.com/graphql-dotnet/server)
project. The historically established discrepancy in one major version constantly caused problems among the developers.

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A7.0+is%3Aissue+is%3Aclosed) and
[pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A7.0+is%3Aclosed) done in v7.

## New Features

### 1. Reduced memory usage for data loader results

Especially noteworthy when a data loader is configured with caching enabled and a singleton lifetime,
memory usage is reduced by freeing unnecessary references after obtaining the results.

### 2. Async support for validation rules

Particularly useful for authentication checks, now validation rules are asynchronous.

### 3. Add `UseApolloTracing` builder method (added in 5.3.0 as `AddApolloTracing`)

This method adds the `InstrumentFieldsMiddleware` to the schema, and conditionally enables metrics
during execution via `ExecutionOptions.EnableMetrics`. It also appends the Apollo Tracing results
to the execution result if metrics is enabled during execution.

### 4. Add `ConfigureExecution` builder method (added in 5.3.0)

`ConfigureExecution` allows a delegate to both alter the execution options and the execution result.
For example, to add total execution time to the results, you could write:

```csharp
services.AddGraphQL(b => b
    // other builder methods here
    .ConfigureExecution(async (options, next) => {
        var timer = Stopwatch.StartNew();
        var result = await next(options);
        result.Extensions ??= new Dictionary<string, object?>();
        result.Extensions["elapsedMs"] = timer.ElapsedMilliseconds;
        return result;
    }));
```

You can also use the method to add logging of any execution errors; not just unhandled errors.

Note: you can access `options.RequestServices` for access to the scoped DI service provider
for the request.

### 5. Complexity analyzer allows configuration of each field's impact towards the total complexity factor

With this change the complexity analyzer could be configured to operate in terms of 'database calls'
or similar means which more closely represent the complexity of the request.

To set the impact on a field, call `.WithComplexityImpact(value)` on the field type, such as:

```csharp
Field<IntGraphType>("id").WithComplexityImpact(123);
```

For more details, please review the PR here: https://github.com/graphql-dotnet/graphql-dotnet/pull/3159

### 6. `AutoRegisteringObjectGraphType` recognizes inherited methods

Inherited methods are now recognized by `AutoRegisteringObjectGraphType` and fields are built for them.

### 7. GraphQL attributes can be applied globally

GraphQL attributes (`GraphQLAttribute`) can now be applied at the module or assembly level, which
will apply to all applicable CLR types within the module or assembly.

This allow global changes to how the schema builder or auto-registering graph type builds graph types,
field types or field arguments.

For an example use case, users could add a global attribute which converts query arguments of type
`DbContext` to pull from services, like this:

```csharp
[AttributeUsage(AttributeTargets.Assembly)]
public class DbContextFromServicesAttribute : GraphQLAttribute
{
    public override void Modify<TParameterType>(ArgumentInformation argumentInformation)
    {
        if (typeof(TParameterType) == typeof(DbContext))
            argumentInformation.SetDelegate(context => (context.RequestServices ?? throw new MissingRequestServicesException())
                .GetRequiredService<TParameterType>());
    }
}

// in AssemblyInfo.cs or whereever in your code at assembly level
[assembly: DbContextFromServices]
```

Similar code could be used to pull your user context class into a method argument.

If it is necessary for a custom global GraphQL attribute to execute prior to or after other attributes,
adjust the return value of the `Priority` property of the attribute.

Note that global attributes may also be added to the `GlobalSwitches.GlobalAttributes` collection.

### 8. `ExecutionOptions.User` property added and available to validation rules and field resolvers

You may pass a `ClaimsPrincipal` instance into `ExecutionOptions` and it will be fed through to
`ValidationContext.User`, `IExecutionContext.User` and `IResolveFieldContext.User` so the value
is accessible by validation rules, document listeners, field middleware and field resolvers.

This property is similar in nature to the ASP.NET Core `HttpContext.User` property, not being
used by the GraphQL.NET engine internally but merely being a convenience property similar to
`RequestServices` and `UserContext` for use by separate authentication packages.

### 9. `Field<TReturnType>` and `Argument<TArgumentClrType> overloads to create field and argument builders with inferred graph types

To define a field with a field builder, previously the graph type was always required, like this:

```csharp
Field<IntGraphType>("test")
    .Resolve(_ => 123);

// or

Field<IntGraphType, int>("test")
    .Resolve(_ => 123);
```

Now you can simply specify the return type, and the graph type will be inferred:

```csharp
Field<int>("test")        // by default assumes not-null
    .Resolve(_ => 123);

// or

Field<int>("test", true)  // specify true or false to indicate nullability
    .Resolve(_ => 123);
```

This is similar to the expression syntax (`Field(x => x.Name)`) which does not require
the graph type to be specified in order to define a field.

Similarly, you can now define an argument by specifying the CLR type:

```csharp
Field<int>("test")
    .Argument<string>("name")              // required
    .Argument<string>("description", true) // optional
    .Resolve(ctx => {
        var name = ctx.GetArgument<string>("name");
        var desc = ctx.GetArgument<string>("description");
        return 123;
    });
```

As with the expression syntax or the `AutoRegisteringObjectGraphType`,
CLR type mappings can be tailored via the `schema.RegisterTypeMapping()` methods.

### 10. Interface graph types can be automatically built from CLR types

Similar to how input and output types can be inferred from their CLR counterparts,
now interface graph types can also be inferred from CLR types. This is possible
with the new class `AutoRegisteringInterfaceGraphType<TSourceType>` which functions
identically to `AutoRegisteringObjectGraphType<TSourceType>` except it creates an
interface type rather than an object graph type. When using automatic CLR type
mapping provided by `AddAutoClrMappings()` or `AddAutoSchema()`, any CLR interface
type is automatically mapped to a interface graph type rather than an object graph
type.

Note that auto-mapped CLR types do not automatically register or link
to any GraphQL interfaces; such mapping needs to be specified via the new
`ImplementsAttribute`. Similarly, CLR types not referenced directly in the schema
need to be added to the schema manually or else no graph type will be generated for them.

Below is a typical example of how the new functionality can be used:

```csharp
services.AddGraphQL(b => b
    .AddAutoSchema<SampleQuery>()
    .AddSystemTextJson());

public class SampleQuery
{
    public static IAnimal Find(AnimalType type) => type switch
    {
        AnimalType.Cat => Cat(),
        AnimalType.Dog => Dog(),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static Cat Cat() => new Cat() { Name = "Fluffy", Lives = 9 };
    public static Dog Dog() => new Dog() { Name = "Shadow", IsLarge = true };
}

public interface IObject
{
    [Id] int Id { get; }
}

public interface IAnimal : IObject
{
    AnimalType Type { get; }
    string Name { get; }
}

public enum AnimalType { Cat, Dog }

[Implements(typeof(IAnimal))]
public class Cat : IAnimal
{
    [Id] public int Id => 10;
    public AnimalType Type => AnimalType.Cat;
    public string Name { get; set; } = null!;
    public int Lives { get; set; }
}

[Implements(typeof(IAnimal))]
public class Dog : IAnimal
{
    [Id] public int Id => 20;
    public AnimalType Type => AnimalType.Dog;
    public string Name { get; set; } = null!;
    public bool IsLarge { get; set; }
}
```

It is important to ensure that any GraphQL attributes applied to members of the CLR types
are applied to both the interface and the classes alike, as the GraphQL.NET engine will build
distinct graph types for the interface and classes which implement those interfaces. Regardless,
fields will execute against the source object as expected.

When supported by the language in use, default interface methods are fully supported, including static
methods defined on an interface. This functionality is available in C# 8.0 and later.

When building a graph type from an interface, methods are built for all inherited methods
as well as the specified interface's methods. This is by design.

### 11. Add `ErrorInfoProviderOptions.ExposeExceptionDetailsMode` property

In v7 we introduced a new `ErrorInfoProviderOptions.ExposeExceptionDetailsMode` property
that allows you to control location of exception details. By default in v7 exception details
are located within "extensions.details" separately from exception message itself. Before v7
exception details were located along with exception message. To use old behavior set
`ExposeExceptionDetailsMode` to `Message` or just do nothing if you have already set
`ExposeExceptionStackTrace` property to `true`. To better reflect the meaning of these changes
we have added a new `ErrorInfoProviderOptions.ExposeExceptionDetails` property and marked
`ErrorInfoProviderOptions.ExposeExceptionStackTrace` property as obsolete.

## Breaking Changes

### 1. `DataLoaderPair<TKey, T>.Loader` property removed

This property was not used internally and should not be necessary by user code or custom implementations.
Removal was necessary as the value is released after the result is set.

### 2. `INodeVisitor` and `IVariableVisitor` members' signatures are asynchronous and end in `Async`.

Note that `MatchingNodeVisitor` has not changed, so many validation rules will not require
any source code changes.

### 3. `ExecutionOptions.ComplexityConfiguration` has been removed

Complexity analysis is now a validation rule and has been removed from execution options.
There is no change when using the `IGraphQLBuilder.AddComplexityAnalyzer` methods as shown below:

```csharp
// GraphQL 5.x or 7.x
builder.AddComplexityAnalyzer(complexityConfig => {
    // set configuration here
});
```

However, when manually setting `options.ComplexityConfiguration`, you will need to instead add the
`ComplexityValidationRule` validation rule to the validation rules.

```csharp
// GraphQL 5.x
options.ComplexityConfiguration = complexityConfig;

// GraphQL 7.x
options.ValidationRules = GraphQL.Validation.DocumentValidator.CoreRules.Append(new ComplexityValidationRule(complexityConfig));
```

### 4. `IComplexityAnalyzer` and `IDocumentCache` have been removed from `DocumentExecuter` constructors

When not using the complexity analyzer, or when using the default complexity analyzer, simply
remove the argument from calls to the constructor. The `IDocumentCache` argument was
removed as well; see the next section for details.

```csharp
/// GraphQL 5.x
public MyCustomDocumentExecuter(
    IDocumentBuilder documentBuilder,
    IDocumentValidator documentValidator,
    IComplexityAnalyzer complexityAnalyzer,
    IDocumentCache documentCache,
    IEnumerable<IConfigureExecutionOptions> configureExecutionOptions,
    IExecutionStrategySelector executionStrategySelector)
    : base(documentBuilder, documentValidator, complexityAnalyzer, documentCache, configureExecutionOptions, executionStrategySelector)
{
}

/// GraphQL 7.x
public MyCustomDocumentExecuter(
    IDocumentBuilder documentBuilder,
    IDocumentValidator documentValidator,
    IExecutionStrategySelector executionStrategySelector,
    IEnumerable<IConfigureExecution> configurations)
    : base(documentBuilder, documentValidator, documentCache, executionStrategySelector, configurations)
{
}
```

When using a custom complexity analyzer implementation added through the `IGraphQLBuilder.AddComplexityAnalyzer`
methods, no change is required.

```csharp
/// GraphQL 5.x or 7.x
builder.AddComplexityAnalyzer<MyComplexityAnalyzer>(complexityConfig => {
    // set configuration here
});
```

When using a custom complexity analyzer implementation configured through DI, and need to
add the `ComplexityValidationRule` validation rule to the validation rules, pass the implementation
from DI through to `ComplexityValidationRule`.

```csharp
// GraphQL 5.x
options.ComplexityConfiguration = complexityConfig;

// GraphQL 7.x
options.ValidationRules = GraphQL.Validation.DocumentValidator.CoreRules.Append(
    new ComplexityValidationRule(
        complexityConfig,
        options.RequestServices.GetRequiredService<IComplexityAnalyzer>()
    ));
```

Using the `IGraphQLBuilder` interface to configure the GraphQL.NET execution engine is the recommended approach.

Note that the `IComplexityAnalyzer` has been deprecated and will be removed in v8.
Please convert your custom complexity analyzer to a validation rule.

### 5. Changes in document caching

To make work with document cache more flexible and allow some advanced use-cases this component
was moved out of the GraphQL.NET execution engine. There is no more `IDocumentCache` interface
to implement and no more `AddDocumentCache` extension methods defined on `IGraphQLBuilder`.
The recommended way to setup caching layer is to inherit from `IConfigureExecution` interface
and register your class as its implementation. No change is required if you used `AddMemoryCache`
extension methods before though `AddMemoryCache` method itself was marked as obsolete and you may
want to switch to its replacement `UseMemoryCache`. 

Other changes in `MemoryDocumentCache` that may affect you - `GetMemoryCacheEntryOptions`,
`GetAsync` and `SetAsync` methods take `ExecutionOptions options` argument instead of `string query`.

### 6. Obsolete members have been removed

| Member | Replaced by |
|--------|-------------|
| `AuthorizationExtensions.RequiresAuthorization` | `IsAuthorizationRequired` |
| `AuthorizationExtensions.AuthorizeWith` | `AuthorizeWithPolicy` |
| `GraphQLAuthorizeAttribute` | `AuthorizeAttribute` |
| `IConfigureExecutionOptions` | `IConfigureExecution` |
| `GraphQLBuilderExtensions.AddMetrics` | `AddApolloTracing` |
| `ApolloTracingDocumentExecuter` | `AddApolloTracing` |

A few of the `DocumentExecuter` constructors have been removed that include `IConfigureExecutionOptions`.
No changes to `ConfigureExecutionOptions` builder methods are required.

`AddMetrics` contains functionality not present in `UseApolloTracing` and vice versa.
Please consider the operation of the new `UseApolloTracing` method (see 'New Features' section above)
when replacing `AddMetrics` with `UseApolloTracing`. Remember that `UseApolloTracing` includes
functionality previously within `ApolloTracingDocumentExecuter` and/or `EnrichWithApolloTracing`.

### 7. `GlobalSwitches.MapAllEnumerableTypes` has been removed; only specific types are detected as lists.

When auto detecting graph types from CLR types (usually within `AutoRegisteringObjectGraphType` or the
expression syntax of `Field(x => x.Member)`), previously any type except `string` that implemented
`IEnumerable` was considered a list type. This would includes types such as dictionary types, making
it impossible to register a CLR type that derives from a dictionary for automatic mapping.

Now only the following types or generic types are considered list types:

- Any array type
- `IEnumerable`
- `IEnumerable<T>`
- `IList<T>`
- `List<T>`
- `ICollection<T>`
- `IReadOnlyCollection<T>`
- `IReadOnlyList<T>`
- `HashSet<T>`
- `ISet<T>`

There is no change as compared to when `GlobalSwitches.MapAllEnumerableTypes` was set to `false`.

### 8. Unification of namespaces for DI extension methods

All extension methods to configure GraphQL.NET services within a dependency injection framework
were moved into `GraphQL` namespace. Also class names were changed:

- `GraphQL.DataLoader.GraphQLBuilderExtensions` -> `GraphQL.DataLoaderGraphQLBuilderExtensions`
- `GraphQL.MemoryCache.GraphQLBuilderExtensions` -> `GraphQL.MemoryCacheGraphQLBuilderExtensions`
- `GraphQL.MicrosoftDI.GraphQLBuilderExtensions` -> `GraphQL.MicrosoftDIGraphQLBuilderExtensions`
- `GraphQL.NewtonsoftJson.GraphQLBuilderExtensions` -> `GraphQL.NewtonsoftJsonGraphQLBuilderExtensions`
- `GraphQL.SystemTextJson.GraphQLBuilderExtensions` -> `GraphQL.SystemTextJsonGraphQLBuilderExtensions`

This change was done for better discoverability and usability of extension methods when configuring DI.

### 9. `IResolveFieldContext.User` property added

Custom implementations of `IResolveFieldContext` must implement the new `User` property.

### 10. Errors do not serialize the `Data` property within the response by default.

This change was made because various .NET services add data to the `Exception` instance
which may be unintentionally returned to the caller.

To revert to prior behavior, register a custom `ErrorInfoProvider` instance configured
to return the data to the caller.

```csharp
//
services.AddGraphQL(b => b
    // add schema, serializer, etc
    .AddErrorInfoProvider(o => o.ExposeData = true));
```

### 11. A bunch of FieldXXX APIs were deprecated

After upgrading to v7 you will likely notice many compiler warnings with a message similar to the following:
> Please use one of the Field() methods returning FieldBuilder and the methods defined on it or just use
> AddField() method directly. This method may be removed in a future release. For now you can continue to
> use this API but we do not encourage this.

The goal of this [change](https://github.com/graphql-dotnet/graphql-dotnet/pull/3237) was to simplify
APIs and guide developers with well-discovered APIs.

You will need to change a way of setting fields on your graph types. Instead of many `FieldXXX`
overloads, start configuring your field with one of the `Field` methods defined on `ComplexGraphType`.
All such methods define a new field and return an instance of `FieldBuilder<T,U>`. Then continue to
configure the field with rich APIs provided by the returned builder. 

```csharp
// GraphQL 5.x
Field<NonNullGraphType<StringGraphType>>(
  "name",
  "Argument name",
  resolve: context => context.Source!.Name);

// GraphQL 7.x
Field<NonNullGraphType<StringGraphType>>("name")
  .Description("Argument name")
  .Resolve(context => context.Source!.Name);



// GraphQL 5.x
FieldAsync<CharacterInterface>("hero", resolve: async context => await data.GetDroidByIdAsync("3").ConfigureAwait(false));

// GraphQL 7.x
Field<CharacterInterface>("hero").ResolveAsync(async context => await data.GetDroidByIdAsync("3").ConfigureAwait(false));



// GraphQL 5.x
FieldAsync<HumanType>(
  "human",
  arguments: new QueryArguments(
      new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the human" }
  ),
  resolve: async context => await data.GetHumanByIdAsync(context.GetArgument<string>("id")).ConfigureAwait(false)
);

// GraphQL 7.x
Field<HumanType>("human")
  .Argument<NonNullGraphType<StringGraphType>>("id", "id of the human")
  .ResolveAsync(async context => await data.GetHumanByIdAsync(context.GetArgument<string>("id")).ConfigureAwait(false));



// GraphQL 5.x
Func<IResolveFieldContext<object>, Task<string?>> resolver = context => Task.FromResult("abc");
FieldAsync<StringGraphType, string>("name", resolve: resolver);

// GraphQL 7.x
Func<IResolveFieldContext<object>, Task<string?>> resolver = context => Task.FromResult("abc");
Field<StringGraphType, string>("name").ResolveAsync(resolver);



// GraphQL 5.x
Func<IResolveFieldContext, string, Task<Droid>> func = (context, id) => data.GetDroidByIdAsync(id);

FieldDelegate<DroidType>(
  "droid",
  arguments: new QueryArguments(
    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the droid" }
  ),
  resolve: func
);



// GraphQL 7.x
Func<IResolveFieldContext, string, Task<Droid>> func = (context, id) => data.GetDroidByIdAsync(id);

Field<DroidType, Droid>("droid")
  .Argument<NonNullGraphType<StringGraphType>>("id", "id of the droid")
  .ResolveDelegate(func);



// GraphQL 5.x
IObservable<object> observable = ...;
FieldSubscribe<MessageGraphType>("messages", subscribe: context => observable);

// GraphQL 7.x
IObservable<object> observable = ...;
Field<MessageGraphType>("messages").ResolveStream(context => observable);



// GraphQL 5.x
Task<IObservable<object>> observable = null!;
FieldSubscribeAsync<MessageGraphType>("messages", subscribeAsync: context => observable);



// GraphQL 7.x
Task<IObservable<object>> observable = null!;
Field<MessageGraphType>("messages").ResolveStreamAsync(context => observable);
```

Also `ComplexGraphType.Field<IntGraphType>("name")` now returns `FieldBuilder` instead of `FieldType`.

### 12. `SortOrder` property added to `IConfigureExecution`

If you have classes that implement `IConfigureExecution`, you will now need to also implement the
added `SortOrder` property. The sort order determines the order that the `IConfigureExecution`
instances are run, with the lowest value being run first.

The default sort order of configurations are as follows:

- 100: Option configurations -- `Add` calls such as `AddValidationRule`, and `ConfigureExecutionOptions` calls
- 200: Execution configurations -- `Use` calls such as `UseApolloTracing`, and `ConfigureExecution` calls

### 13. Interfaces mapped by the `AutoRegisteringGraphTypeMappingProvider` now generate interface graph types rather than object graph types.

If you use interfaces to contain your GraphQL attributes for your data models, or for any other reason
rely on the generation of object graph types for interface CLR types, you may wish to revert this design
choice. Simply reconfigure the mapping provider as follows and interfaces will be generated as object graph
types as before:

```csharp
services.AddGraphQL(b => b
    .AddGraphTypeMappingProvider(new AutoRegisteringGraphTypeMappingProvider(true, true, false))
    // other calls
);
```

### 14. Graph types cannot be used as data models

From version 7.1 on, graph types cannot be used as data models. This is because the graph types are
designed to be used as schema definitions, and not as data models. The following classes
will now throw an exception if a graph type is used as a data model:

- `ObjectGraphType<TSourceType>`
- `InputObjectGraphType<TSourceType>`
- `AutoRegisteringObjectGraphType<TSourceType>`
- `AutoRegisteringInputObjectGraphType<TSourceType>`
- `AutoRegisteringInterfaceGraphType<TSourceType>`
- 
If it is necessary to do so, you can derive from the `ObjectGraphType` or `InputObjectGraphType` classes
instead of the generic version.

### 15. Different instances of the same graph type cannot be referenced in the same schema

From version 7.1.1 on, different instances of the same graph type cannot be referenced in the same schema.
This prevents the situation where some graph types are not initialized and throw errors when used.
If this is causing a problem (perhaps with graph types that are dynamically generated, for instance),
create and pull from a dictionary of instantiated types, or use `GraphQLNameReference` to reference
the graph type by name.
