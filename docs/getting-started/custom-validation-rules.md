# Custom Validation Rules

GraphQL.NET provides a validation framework that allows you to inspect and validate incoming GraphQL requests before execution. This guide covers how to create custom validation rules for your application.

## Built-in Validation Rules

GraphQL.NET includes several built-in validation rules that enforce the GraphQL specification:

| Rule | Description |
|------|-------------|
| `UniqueOperationNamesRule` | Ensures all operations have unique names |
| `UniqueFragmentNamesRule` | Ensures all fragments have unique names |
| `KnownTypeNamesRule` | Verifies all types referenced exist in the schema |
| `KnownFragmentNamesRule` | Verifies all fragment spreads reference defined fragments |
| `FieldsOnCorrectTypeRule` | Ensures fields are only queried on types that define them |
| `NoUndefinedVariablesRule` | Ensures all used variables are defined |
| `NoUnusedVariablesRule` | Ensures all defined variables are used |
| `UniqueVariableNamesRule` | Ensures variable names are unique per operation |
| `OverlappingFieldsRule` | Detects conflicting field selections |
| `PossibleFragmentSpreadsRule` | Validates fragment spread type compatibility |
| `ProvidedNonNullArgumentsRule` | Ensures required arguments are provided |
| `UniqueArgumentsRule` | Ensures argument names are unique per field |
| `UniqueDirectivesRule` | Ensures directives are not duplicated |
| `KnownDirectiveNamesRule` | Verifies all directives are defined |
| `DirectivesInValidLocationsRule` | Ensures directives are used in valid positions |
| `VariableTypesRule` | Validates variable type compatibility |

### Custom Built-in Rules

These additional rules are available but not enabled by default:

| Rule | Description |
|------|-------------|
| `NoIntrospectionValidationRule` | Blocks all introspection queries |
| `ComplexityValidationRule` | Limits query complexity |
| `DeprecatedElementsValidationRule` | Warns when deprecated fields are used |

## Creating Custom Validation Rules

All validation rules implement the `IValidationRule` interface. In most cases, you should inherit from `ValidationRuleBase` and override one of the visitor methods.

### Interface Overview

```csharp
public interface IValidationRule
{
    ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context);
    ValueTask<IVariableVisitor?> GetVariableVisitorAsync(ValidationContext context);
    ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context);
}
```

- **Pre-node visitor**: Runs before the standard validation rules. Use for early checks.
- **Variable visitor**: Inspects variable definitions and values.
- **Post-node visitor**: Runs after the standard validation rules. Use for cross-node checks.

### Example 1: Query Depth Limit

Limit how deeply nested a query can be to prevent overly complex queries:

```csharp
using GraphQL.Validation;
using GraphQLParser.AST;

public class QueryDepthValidationRule : ValidationRuleBase
{
    private readonly int _maxDepth;

    public QueryDepthValidationRule(int maxDepth)
    {
        _maxDepth = maxDepth;
    }

    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(new DepthVisitor(_maxDepth));

    private class DepthVisitor : MatchingNodeVisitor<GraphQLField>
    {
        private readonly int _maxDepth;

        public DepthVisitor(int maxDepth) : base((field, context) =>
        {
            int depth = CalculateDepth(field);
            if (depth > maxDepth)
            {
                context.ReportError(
                    new ValidationError(
                        $"Query depth {depth} exceeds maximum allowed depth of {maxDepth}.",
                        field));
            }
        })
        {
            _maxDepth = maxDepth;
        }

        private static int CalculateDepth(GraphQLField field)
        {
            if (field.SelectionSet == null)
                return 1;

            int maxChildDepth = 0;
            foreach (var selection in field.SelectionSet.Selections)
            {
                if (selection is GraphQLField childField)
                {
                    maxChildDepth = Math.Max(maxChildDepth, CalculateDepth(childField));
                }
            }
            return 1 + maxChildDepth;
        }
    }
}
```

### Example 2: Field Authorization Rule

Restrict access to specific fields based on user context:

```csharp
using GraphQL.Validation;
using GraphQLParser.AST;

public class FieldAuthorizationRule : ValidationRuleBase
{
    private readonly HashSet<string> _restrictedFields;
    private readonly Func<string?> _getCurrentUser;

    public FieldAuthorizationRule(
        IEnumerable<string> restrictedFields,
        Func<string?> getCurrentUser)
    {
        _restrictedFields = new HashSet<string>(restrictedFields);
        _getCurrentUser = getCurrentUser;
    }

    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(new FieldAuthVisitor(_restrictedFields, _getCurrentUser));

    private class FieldAuthVisitor : MatchingNodeVisitor<GraphQLField>
    {
        private readonly HashSet<string> _restrictedFields;
        private readonly Func<string?> _getCurrentUser;

        public FieldAuthVisitor(
            HashSet<string> restrictedFields,
            Func<string?> getCurrentUser) : base((field, context) =>
        {
            if (_restrictedFields.Contains(field.Name.Value) && _getCurrentUser() == null)
            {
                context.ReportError(
                    new ValidationError(
                        $"Access denied for field '{field.Name.Value}'. Authentication required.",
                        field));
            }
        })
        {
            _restrictedFields = restrictedFields;
            _getCurrentUser = getCurrentUser;
        }
    }
}
```

### Example 3: Query Complexity Analysis

Estimate and limit the total complexity of a query:

```csharp
using GraphQL.Validation;
using GraphQLParser.AST;

public class QueryComplexityRule : ValidationRuleBase
{
    private readonly int _maxComplexity;
    private readonly Dictionary<string, int> _fieldComplexity;

    public QueryComplexityRule(int maxComplexity, Dictionary<string, int>? fieldComplexity = null)
    {
        _maxComplexity = maxComplexity;
        _fieldComplexity = fieldComplexity ?? new();
    }

    public override ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context)
    {
        int totalComplexity = 0;

        return new(new MatchingNodeVisitor<GraphQLField>((field, context) =>
        {
            totalComplexity += _fieldComplexity.TryGetValue(field.Name.Value, out var cost)
                ? cost
                : 1;

            if (totalComplexity > _maxComplexity)
            {
                context.ReportError(
                    new ValidationError(
                        $"Query complexity {totalComplexity} exceeds maximum of {_maxComplexity}.",
                        field));
            }
        }));
    }
}
```

## Registering Custom Validation Rules

Register your custom rules when building the schema or configuring the service:

### With Dependency Injection

```csharp
// In Startup.cs or Program.cs
builder.Services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .AddValidationRule<QueryDepthValidationRule>()
    .AddValidationRule<FieldAuthorizationRule>());
```

### With Manual Configuration

```csharp
var schema = new MySchema();

var result = await schema.ExecuteAsync(options =>
{
    options.Query = query;
    options.ValidationRules = new IValidationRule[]
    {
        QueryDepthValidationRule.Instance,
        new FieldAuthorizationRule(
            new[] { "ssn", "creditCard", "salary" },
            () => httpContext.User.Identity?.Name),
    };
});
```

## Field-Level Validation: Parser and Validator Methods

In addition to document-level validation rules, GraphQL.NET supports field-level validation through `Parser` and `Validator` methods on fields. These allow you to validate individual field arguments and input values.

### Parser Methods

Parser methods transform and validate input values at the field level:

```csharp
public class MyQueryType : ObjectGraphType
{
    public MyQueryType()
    {
        Field<StringGraphType>("greet")
            .Argument<StringGraphType>("name")
            .ParseValue(context =>
            {
                var name = context.GetArgument<string>("name");
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Name cannot be empty.");
                return name.Trim();
            })
            .Resolve(context =>
            {
                var name = context.GetArgument<string>("name");
                return $"Hello, {name}!";
            });
    }
}
```

### Validator Methods

Validator methods check field arguments without transforming them:

```csharp
Field<StringGraphType>("search")
    .Argument<StringGraphType>("query")
    .ValidateArgument("query", (value) =>
    {
        var query = value as string;
        if (query != null && query.Length > 500)
            throw new ArgumentException("Search query must be 500 characters or fewer.");
    })
    .Resolve(context => /* search logic */);
```

### Using Attributes (Source-Generated)

With GraphQL.NET v8, you can use attributes for field-level validation in source-generated schemas:

```csharp
public class MyQuery
{
    [Name("search")]
    public static string Search(
        [MaxLength(500)] string query,
        [Range(1, 100)] int limit = 10)
    {
        // query is validated to be max 500 characters
        // limit is validated to be between 1 and 100
        return SearchInternal(query, limit);
    }
}
```

Available validation attributes include:
- `[Required]` - Field/argument must be provided
- `[MaxLength(int)]` / `[MinLength(int)]` - String length constraints
- `[Range(min, max)]` - Numeric range constraints
- `[RegularExpression(string)]` - Regex pattern matching
- `[Emailaddress]` - Email format validation
- `[Url]` - URL format validation

## Best Practices

1. **Keep rules focused**: Each rule should do one thing well.
2. **Use static instances for stateless rules**: If your rule doesn't need configuration, use a singleton pattern like `NoIntrospectionValidationRule.Instance`.
3. **Report errors early**: Use pre-node visitors for fail-fast validation.
4. **Provide clear error messages**: Include the field name, expected value, and what went wrong.
5. **Consider performance**: Validation runs on every request. Keep your rules efficient.
6. **Combine with authorization**: Use validation rules for query structure checks, and field-level authorization for access control.

## See Also

- [Validation Rules Reference](https://graphql-dotnet.github.io/docs/getting-started/validation-rules)
- [Complexity Validation](https://graphql-dotnet.github.io/docs/getting-started/complexity-validation)
- [Dependency Injection](https://graphql-dotnet.github.io/docs/getting-started/dependency-injection)
