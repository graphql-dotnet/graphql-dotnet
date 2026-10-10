
# Custom Validation Rules in GraphQL.NET

This guide demonstrates how to implement custom validation rules in GraphQL.NET by creating an `IValidationRule` implementation.

## Creating a Custom Validation Rule

To create a custom validation rule, implement the `IValidationRule` interface and register it with your GraphQL schema.

### Example: Custom Validation Rule for Minimum Length

Here's a C# example that implements a custom validation rule to ensure a string field has a minimum length:

```csharp
using GraphQL.Validation;
using GraphQL.Language;

public class MinimumLengthRule : IValidationRule
{
    private readonly int _minimumLength;

    public MinimumLengthRule(int minimumLength)
    {
        _minimumLength = minimumLength;
    }

    public IReadOnlyList<ValidationError> Validate(
        (DocumentNode Document, IReadOnlyList<ValidationRuleContext> Contexts) arguments)
    {
        var errors = new List<ValidationError>();

        foreach (var context in arguments.Contexts)
        {
            if (context.Field.SelectionSet?.Selections.Count > 0)
            {
                // Check if the field is a string type
                if (context.Field.Type is NamedType stringType &&
                    stringType.Name == "String")
                {
                    // In a real implementation, you would check the actual value
                    // This is a simplified example showing the validation rule structure
                    errors.Add(new ValidationError(
                        $"Field '{context.Field.Name}' must have a minimum length of {_minimumLength} characters.",
                        context.Field));
                }
            }
        }

        return errors;
    }
}
```

### Registering the Validation Rule

Register your custom validation rule with the schema:

```csharp
var schema = new GraphQLSchema
{
    Query = new Query(),
    Mutation = new Mutation(),
    // Add your custom validation rule
    ValidationRules = new List<IValidationRule>
    {
        new MinimumLengthRule(5) // Enforce minimum length of 5 characters
    }
};
```

### Using the Validation Rule

When you execute a query that violates the validation rule, GraphQL.NET will return an error:

```graphql
query {
  user(id: 1) {
    name  # This will fail if name has less than 5 characters
  }
}
```

## Best Practices

1. **Type Safety**: Always check the field type before applying validation
2. **Error Messages**: Provide clear, actionable error messages
3. **Performance**: Keep validation rules efficient, especially for large schemas
4. **Testing**: Test your validation rules with various edge cases

## Complete Example

Here's a more complete example with a custom validation rule for email format:

```csharp
public class EmailFormatRule : IValidationRule
{
    public IReadOnlyList<ValidationError> Validate(
        (DocumentNode Document, IReadOnlyList<ValidationRuleContext> Contexts) arguments)
    {
        var errors = new List<ValidationError>();

        foreach (var context in arguments.Contexts)
        {
            if (context.Field.SelectionSet?.Selections.Count > 0 &&
                context.Field.Type is NamedType stringType &&
                stringType.Name == "String")
            {
                // In a real implementation, you would check the actual value
                // against a regex pattern for email format
                errors.Add(new ValidationError(
                    $"Field '{context.Field.Name}' must be a valid email address.",
                    context.Field));
            }
        }

        return errors;
    }
}
```

