# Samples

This repository includes several sample projects that demonstrate various features of GraphQL.NET.
All samples are located in the `src/` directory.

## Available Samples

### GraphQL.Harness

A full-featured ASP.NET Core hosted GraphQL server sample demonstrating:
- Dependency injection integration
- Middleware setup
- Schema configuration
- Subscription support via WebSockets

**Location:** `src/GraphQL.Harness`

---

### StarWars (Classic)

A classic StarWars API sample using the standard schema-first approach with GraphQL.NET.

**Location:** `src/StarWars`

---

### StarWars (AOT)

A StarWars API sample built with Native AOT (Ahead-of-Time) compilation support.

**Location:** `src/StarWarsAOT`

---

### AOT Sample

A minimal sample demonstrating Native AOT compilation with GraphQL.NET.

**Location:** `src/GraphQL.AOT.Sample`

---

### Custom Validation Rule

A sample project demonstrating how to write and register **custom validation rules** in GraphQL.NET.

**Location:** `src/CustomValidationRule`

This sample shows:
- How to implement `IValidationRule`
- How to use `INodeVisitor` / `NodeVisitors` to traverse the document AST
- How to report validation errors
- How to register a custom rule via `ExecutionOptions.ValidationRules`

#### Key Files

| File | Description |
|---|---|
| `NoIntrospectionRule.cs` | A custom rule that disallows introspection queries |
| `Program.cs` | Wires everything up and runs example queries |

#### Custom Rule Implementation

The following example shows a rule that prevents clients from executing introspection queries
(i.e. queries that contain `__schema` or `__type` fields).

```csharp
using GraphQL;
using GraphQL.Validation;
using GraphQLParser.AST;

public class NoIntrospectionRule : IValidationRule
{
    public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
    {
        return new ValueTask<INodeVisitor?>(
            new MatchingNodeVisitor<GraphQLField>(
                (field, context) =>
                {
                    if (field.Name.StringValue.StartsWith("__", StringComparison.Ordinal))
                    {
                        context.ReportError(new ValidationError(
                            context.Document.Source,
                            "NoIntrospection",
                            "Introspection queries are not allowed.",
                            field));
                    }
                }));
    }
}
```

#### Registering the Rule

You can register a custom validation rule globally for all requests or per-request.

**Per-request (via `ExecutionOptions`):**

```csharp
var result = await schema.ExecuteAsync(options =>
{
    options.Query = query;
    options.ValidationRules = DocumentValidator.CoreRules
        .Append(new NoIntrospectionRule());
});
```

**Globally (via DI in ASP.NET Core):**

```csharp
services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .AddValidationRule<NoIntrospectionRule>());
```

> **Note:** When adding validation rules via dependency injection using `AddValidationRule<T>()`,
> the rule is automatically prepended or appended to the default rule set on every request.

#### Running the Sample

```bash
cd src/CustomValidationRule
dotnet run
```

You should see output demonstrating that valid queries pass validation while introspection
queries are rejected with an appropriate error message.
