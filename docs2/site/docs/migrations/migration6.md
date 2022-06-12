# Migrating from v5.x to v6.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A6.0+is%3Aissue+is%3Aclosed) and [pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A6.0+is%3Aclosed) done in v6.

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

```cs
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

## Breaking Changes

### 1. `DataLoaderPair<TKey, T>.Loader` property removed

This property was not used internally and should not be necessary by user code or custom implementations.
Removal was necessary as the value is released after the result is set.

### 2. `INodeVisitor` and `IVariableVisitor` members' signatures are asynchronous and end in `Async`.

Note that `MatchingNodeVisitor` has not changed, so many validation rules will not require
any source code changes.

### 3. Obsolete members have been removed

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

### 3. `GlobalSwitches.MapAllEnumerableTypes` has been removed; only specific types are detected as lists.

When auto detecting graph types from CLR types (usually within `AutoRegisteringObjectGraphType` or the
expression syntax of `Field(x => x.Member)`), previously any type except `string` that implemented
`IEnumerable` was considered a list type. This would includes types such as dictionary types, making
it impossible to register a CLR type that dervies from a dictionary for automatic mapping.

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
