# Samples

GraphQL.NET provides several sample projects to help you get started and understand various features of the library.

## Available Samples

### GraphQL.Validation.Sample

This sample demonstrates how to implement custom validation rules in GraphQL.NET. It includes three different validation rules:

1. **NoIntrospectionValidationRule** - Prevents introspection queries from being executed. This is useful for production environments where you want to disable introspection for security reasons.

2. **InputFieldsOfCorrectLengthValidationRule** - Limits the length of string input values to a maximum of 500 characters. Demonstrates how to validate input arguments after they have been parsed.

3. **MaxQueryDepthValidationRule** - Limits the maximum depth of a GraphQL query to prevent overly complex queries that could impact server performance.

To run the sample:

```bash
cd samples/GraphQL.Validation.Sample
dotnet run
```

Then open your browser to:
- GraphiQL: http://localhost:5000/graphiql
- Altair: http://localhost:5000/altair
- Voyager: http://localhost:5000/voyager

### GraphQL.Harness

This sample demonstrates the basic setup of GraphQL.NET with ASP.NET Core, including:
- Schema configuration
- Query and mutation types
- Integration with GraphQL IDEs (GraphiQL, Altair, Voyager)

### GraphQL.DataLoader.Sample.Default / GraphQL.DataLoader.Sample.DI

These samples demonstrate how to use the DataLoader pattern to batch and cache database queries.

### GraphQL.AotCompilationSample.CodeFirst / GraphQL.AotCompilationSample.TypeFirst

These samples demonstrate how to use GraphQL.NET with AOT (Ahead-of-Time) compilation.

### GraphQL.Federation Samples

The Federation samples demonstrate how to implement Apollo Federation with GraphQL.NET:
- GraphQL.Federation.SchemaFirst.Sample1 - Schema-first approach
- GraphQL.Federation.CodeFirst.Sample3 - Code-first approach
- GraphQL.Federation.TypeFirst.Sample4 - Type-first approach

## Adding a Custom Validation Rule

To add a custom validation rule, create a class that inherits from `ValidationRuleBase` and implements the `INodeVisitor` interface:

```csharp
using GraphQL.Language.AST;
using GraphQL.Validation;
using GraphQL.Validation.Rules;

public class MyCustomValidationRule : ValidationRuleBase, INodeVisitor
{
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(this);

    public ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
    {
        // Add your validation logic here
        return default;
    }

    public ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context)
        => default;
}
```

Then register the rule in your DI configuration:

```csharp
services.AddGraphQL(builder => builder
    .AddSchema<MySchema>()
    .AddValidationRule<MyCustomValidationRule>());
```

For more details on custom validation rules, see the [Query Validation](getting-started/query-validation) documentation.
