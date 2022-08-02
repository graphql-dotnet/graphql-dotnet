# Migrating from v5.x to v7.x

Note that v6 was skipped to align GraphQL.NET version with versions of packages from [server](https://github.com/graphql-dotnet/server) project. The historically established discrepancy in one major version constantly caused problems among the developers.

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A7.0+is%3Aissue+is%3Aclosed) and [pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A7.0+is%3Aclosed) done in v6.

## New Features

### 1. Reduced memory usage for data loader results

Especially noteworthy when a data loader is configured with caching enabled and a singleton lifetime,
memory usage is reduced by freeing unnecessary references after obtaining the results.

### 2. Async support for validation rules

Particularly useful for authentication checks, now validation rules are asynchronous.

### 3. Add `AddApolloTracing` builder method (added in 5.3.0)

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

### 9. Add `Field<TReturnType>` overload to create field builder with an inferred graph type

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
Field<int>("test")        // by defaut assumes not-null
    .Resolve(_ => 123);

// or

Field<int>("test", true)  // specify true or false to indicate nullability
    .Resolve(_ => 123);
```

This is similar to the expression syntax (`Field(x => x.Name)`) which does not require
the graph type to be specified in order to define a field.

As with the expression syntax or the `AutoRegisteringObjectGraphType`,
CLR type mappings can be tailored via the `schema.RegisterTypeMapping()` methods.

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
// GraphQL 5.x or 6.x
builder.AddComplexityAnalyzer(complexityConfig => {
    // set configuration here
});
```

However, when manually setting `options.ComplexityConfiguration`, you will need to instead add the
`ComplexityValidationRule` validation rule to the validation rules.

```csharp
// GraphQL 5.x
options.ComplexityConfiguration = complexityConfig;

// GraphQL 6.x
options.ValidationRules = GraphQL.Validation.DocumentValidator.CoreRules.Append(new ComplexityValidationRule(complexityConfig));
```

### 4. `IComplexityAnalyzer` has been removed from `DocumentExecuter` constructors

When not using the complexity analyzer, or when using the default complexity analyzer, simply
remove the argument from calls to the constructor and no additional changes are required.

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

/// GraphQL 6.x
public MyCustomDocumentExecuter(
    IDocumentBuilder documentBuilder,
    IDocumentValidator documentValidator,
    IDocumentCache documentCache,
    IEnumerable<IConfigureExecutionOptions> configureExecutionOptions,
    IExecutionStrategySelector executionStrategySelector)
    : base(documentBuilder, documentValidator, documentCache, configureExecutionOptions, executionStrategySelector)
{
}
```

When using a custom complexity analyzer implementation added through the `IGraphQLBuilder.AddComplexityAnalyzer`
methods, no change is required.

```csharp
/// GraphQL 5.x or 6.x
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

// GraphQL 6.x
options.ValidationRules = GraphQL.Validation.DocumentValidator.CoreRules.Append(
    new ComplexityValidationRule(
        complexityConfig,
        options.RequestServices.GetRequiredService<IComplexityAnalyzer>()
    ));
```

Using the `IGraphQLBuilder` interface to configure the GraphQL.NET execution engine is the recommended approach.

### 5. Obsolete members have been removed

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

`AddMetrics` contains functionality not present in `AddApolloTracing` and vice versa.
Please consider the operation of the new `AddApolloTracing` method (see 'New Features' section above)
when replacing `AddMetrics` with `AddApolloTracing`. Remember that `AddApolloTracing` includes
functionality previously within `ApolloTracingDocumentExecuter` and/or `EnrichWithApolloTracing`.

### 6. `GlobalSwitches.MapAllEnumerableTypes` has been removed; only specific types are detected as lists.

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

### 7. Unification of namespaces for DI extension methods

All extension methods to configure GraphQL.NET services within a dependency injection framework
were moved into `GraphQL` namespace. Also class names were changed:

- `GraphQL.DataLoader.GraphQLBuilderExtensions` -> `GraphQL.DataLoaderGraphQLBuilderExtensions`
- `GraphQL.MemoryCache.GraphQLBuilderExtensions` -> `GraphQL.MemoryCacheGraphQLBuilderExtensions`
- `GraphQL.MicrosoftDI.GraphQLBuilderExtensions` -> `GraphQL.MicrosoftDIGraphQLBuilderExtensions`
- `GraphQL.NewtonsoftJson.GraphQLBuilderExtensions` -> `GraphQL.NewtonsoftJsonGraphQLBuilderExtensions`
- `GraphQL.SystemTextJson.GraphQLBuilderExtensions` -> `GraphQL.SystemTextJsonGraphQLBuilderExtensions`

This change was done for better discoverability and usability of extension methods when configuring DI.

### 8. `IResolveFieldContext.User` property added

Custom implementations of `IResolveFieldContext` must implement the new `User` property.

### 9. Errors do not serialize the `Data` property within the response by default.

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
