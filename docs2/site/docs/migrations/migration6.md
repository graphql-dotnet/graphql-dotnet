# Migrating from v5.x to v6.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A6.0+is%3Aissue+is%3Aclosed) and [pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A6.0+is%3Aclosed) done in v6.

## New Features

### 1. Reduced memory usage for data loader results

Especially noteworthy when a data loader is configured with caching enabled and a singleton lifetime,
memory usage is reduced by freeing unnecessary references after obtaining the results.

### 2. Async support for validation rules

Particularly useful for authentication checks, now validation rules are asynchronous.

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
