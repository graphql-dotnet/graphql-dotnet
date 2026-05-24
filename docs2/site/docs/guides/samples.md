# Samples

The GraphQL.NET repository includes several sample projects that demonstrate various features and patterns.
These samples are located in the [`samples`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples)
folder of the repository.

## GraphQL.Harness

A basic ASP.NET Core project that demonstrates how to set up GraphQL.NET with the StarWars schema.
It includes GraphiQL, Altair, and Voyager UI integrations for interactive query testing.

**Key concepts demonstrated:**
- Basic GraphQL.NET setup with ASP.NET Core
- Schema registration via dependency injection
- Multiple GraphQL UI tools
- Field middleware (`CountFieldMiddleware`, `InstrumentFieldsMiddleware`)

## GraphQL.CustomValidationRules.Sample

A sample project that demonstrates how to create and register custom validation rules.
This is useful when you want to enforce additional constraints on incoming GraphQL queries
beyond the standard specification validation.

**Key concepts demonstrated:**

### Extending `ValidationRuleBase`

All custom validation rules should extend `ValidationRuleBase` and override one or more
of the visitor methods:

```csharp
public class MaxDepthValidationRule : ValidationRuleBase
{
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(new MatchingNodeVisitor<GraphQLOperationDefinition>(
            enter: (op, ctx) =>
            {
                // Validate the operation here
            }));
}
```

### Using `MatchingNodeVisitor<TNode>`

`MatchingNodeVisitor<TNode>` allows you to define callbacks that fire only when the
validation visitor encounters a specific AST node type. You can provide `enter` (before
children are visited) and `leave` (after children are visited) delegates:

```csharp
new MatchingNodeVisitor<GraphQLField>(
    enter: (field, context) => { /* called when entering a field node */ },
    leave: (field, context) => { /* called when leaving a field node */ });
```

### Registering custom rules

Custom validation rules can be registered via the DI builder:

```csharp
services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .AddSystemTextJson()
    .AddValidationRule<MaxDepthValidationRule>()
    .AddValidationRule<MaxFieldsValidationRule>());
```

Alternatively, you can set them directly on `ExecutionOptions.ValidationRules` when
calling `IDocumentExecuter.ExecuteAsync`.

### Reporting validation errors

Use `context.ReportError()` with a `ValidationError` to report issues:

```csharp
context.ReportError(new ValidationError(
    context.Document.Source,
    "CUSTOM_CODE",
    "Human-readable error message.",
    node));
```

### Sample rules included

| Rule | Description |
|------|-------------|
| `MaxDepthValidationRule` | Rejects queries that nest fields deeper than a configured limit (default: 5 levels). |
| `MaxFieldsValidationRule` | Rejects selection sets that contain more than a configured number of fields (default: 20). |

## AOT Compilation Samples

Two samples demonstrate ahead-of-time (AOT) compilation support:

- **GraphQL.AotCompilationSample.CodeFirst** — Code-first approach with AOT
- **GraphQL.AotCompilationSample.TypeFirst** — Type-first approach with AOT

## DataLoader Samples

- **GraphQL.DataLoader.Sample.Default** — Basic DataLoader usage
- **GraphQL.DataLoader.Sample.DI** — DataLoader with dependency injection

## Federation Samples

Four samples demonstrate Apollo Federation support:

- **GraphQL.Federation.SchemaFirst.Sample1** — Schema-first federation with `@key` directive
- **GraphQL.Federation.SchemaFirst.Sample2** — Schema-first federation with products/categories
- **GraphQL.Federation.CodeFirst.Sample3** — Code-first federation approach
- **GraphQL.Federation.TypeFirst.Sample4** — Type-first federation approach

## Running the Samples

Each sample can be run independently using:

```bash
cd samples/GraphQL.Harness
dotnet run
```

Then navigate to the displayed URL (typically `http://localhost:5000`) to access the
GraphiQL interface.
