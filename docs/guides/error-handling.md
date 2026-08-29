
# Custom Error Handling in GraphQL .NET

This guide explains how to implement custom error handling in GraphQL .NET using `ExecutionErrors` and custom error filters.

## Understanding ExecutionErrors

In GraphQL .NET, errors during query execution are represented by `ExecutionError` objects. These errors can occur due to various reasons like:
- Invalid queries
- Missing fields
- Type mismatches
- Business logic validation failures

The framework automatically collects these errors and returns them to the client.

## Custom Error Filtering

You can create custom error filters to transform or handle errors in a specific way. This is useful for:
- Adding custom error messages
- Filtering out sensitive information
- Standardizing error formats
- Adding logging

## Example: Custom Error Filter

Here's an example of how to implement a custom error filter:

```csharp
using GraphQL;
using GraphQL.Types;
using GraphQL.Validation;

public class CustomErrorFilter : IErrorFilter
{
    public IErrorFilter Next { get; set; }

    public IErrorFilterResult Filter(IErrorFilterContext context)
    {
        // Get the next error filter in the chain
        var nextResult = Next?.Filter(context);

        // If there are errors, process them
        if (nextResult?.Errors != null)
        {
            var filteredErrors = new List<IError>();

            foreach (var error in nextResult.Errors)
            {
                // Example: Add custom error message for validation errors
                if (error.Message.Contains("Validation failed"))
                {
                    var customError = new Error
                    {
                        Message = "Validation error occurred. Please check your input.",
                        Path = error.Path,
                        Locations = error.Locations,
                        Extensions = new
                        {
                            error.Extensions,
                            Code = "VALIDATION_ERROR"
                        }
                    };
                    filteredErrors.Add(customError);
                }
                else
                {
                    // For other errors, keep them as-is
                    filteredErrors.Add(error);
                }
            }

            return new ErrorFilterResult(filteredErrors);
        }

        return nextResult;
    }
}
```

## Registering the Error Filter

To use your custom error filter, you need to register it with the GraphQL schema:

```csharp
var schema = new Schema
{
    Query = new QueryType { /* ... */ },
    Mutation = new MutationType { /* ... */ },
    ErrorFilter = new CustomErrorFilter()
};
```

## Best Practices

1. **Security**: Never expose sensitive information in error messages
2. **Consistency**: Maintain consistent error formats across your API
3. **Logging**: Consider logging errors for debugging purposes
4. **Testing**: Test your error handling with various error scenarios

## Common Error Scenarios

Here are some common error scenarios you might want to handle:

1. **Validation Errors**: When input validation fails
2. **Authentication Errors**: When users are not authenticated
3. **Authorization Errors**: When users lack permissions
4. **Business Logic Errors**: When business rules are violated
5. **Database Errors**: When database operations fail

## Complete Example

Here's a more complete example that handles multiple error types:

```csharp
public class ComprehensiveErrorFilter : IErrorFilter
{
    public IErrorFilter Next { get; set; }

    public IErrorFilterResult Filter(IErrorFilterContext context)
    {
        var nextResult = Next?.Filter(context);

        if (nextResult?.Errors == null)
            return nextResult;

        var filteredErrors = new List<IError>();

        foreach (var error in nextResult.Errors)
        {
            var customError = new Error
            {
                Message = error.Message,
                Path = error.Path,
                Locations = error.Locations,
                Extensions = new
                {
                    error.Extensions,
                    Timestamp = DateTime.UtcNow,
                    ErrorCode = GetErrorCode(error.Message)
                }
            };

            // Add custom handling for specific error types
            if (error.Message.Contains("Authentication"))
            {
                customError.Extensions["Type"] = "AUTHENTICATION_ERROR";
            }
            else if (error.Message.Contains("Authorization"))
            {
                customError.Extensions["Type"] = "AUTHORIZATION_ERROR";
            }
            else if (error.Message.Contains("Validation"))
            {
                customError.Extensions["Type"] = "VALIDATION_ERROR";
            }

            filteredErrors.Add(customError);
        }

        return new ErrorFilterResult(filteredErrors);
    }

    private string GetErrorCode(string message)
    {
        // Implement logic to determine error codes
        return "UNKNOWN_ERROR";
    }
}
```

## Summary

This guide provided an overview of custom error handling in GraphQL .NET. By implementing custom error filters, you can:
- Provide more user-friendly error messages
- Standardize error formats across your API
- Add additional context to errors
- Implement specific error handling logic

The example code demonstrates how to create and register a custom error filter, and shows how to handle different types of errors in a comprehensive way.

**Completed error handling guide for [graphql-dotnet/graphql-dotnet]**
