# Field Middleware

Field Middleware provides additional behaviors during field resolution. GraphQL.NET supports two types:

1. **Global Middleware** - Applied to all fields across the entire schema
2. **Field-Specific Middleware** - Applied to individual fields (v8.7.0+)

Both types work similarly to ASP.NET Core HTTP middleware, executing in a chain where each middleware can perform actions before and after the next middleware or resolver.

## Creating Middleware

Middleware is created by implementing the `IFieldMiddleware` interface:

```csharp
public class LoggingMiddleware : IFieldMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
    {
        _logger.LogInformation("Resolving {Field}", context.FieldName);
        var result = await next(context);
        _logger.LogInformation("Resolved {Field}", context.FieldName);
        return result;
    }
}
```

The same middleware class can be used as either global or field-specific middleware depending on how you register it.

## Global Middleware

Global middleware applies to all fields in your schema. This is useful for cross-cutting concerns like logging, metrics, or authorization.

### Using UseMiddleware

The recommended approach is using `UseMiddleware<T>()` on the GraphQL builder:

```csharp
services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .UseMiddleware<InstrumentFieldsMiddleware>()
    .UseMiddleware<LoggingMiddleware>());
```

This automatically registers the middleware in DI as a singleton and applies it to the schema.

### Manual Registration

You can also register middleware directly on the schema:

```csharp
public class MySchema : Schema
{
    public MySchema(IServiceProvider services, MyQuery query, LoggingMiddleware middleware)
        : base(services)
    {
        Query = query;
        FieldMiddleware.Use(middleware);
    }
}
```

Or use a lambda:

```csharp
schema.FieldMiddleware.Use(next => async context =>
{
    // Code before resolver
    var result = await next(context);
    // Code after resolver
    return result;
});
```

## Field-Specific Middleware

Field-specific middleware applies only to designated fields, offering better performance and clearer intent than global middleware for field-level concerns. There are three ways to apply middleware to a field:

```csharp
// Register middleware in DI first if applicable
services.AddSingleton<LoggingMiddleware>();

public class MyGraphType : ObjectGraphType
{
    public MyGraphType()
    {
        Field<StringGraphType>("field1")
            .Resolve(context => "Data")
            // 1. Using a lambda
            .ApplyMiddleware(next => async context =>
            {
                // Custom logic here
                var result = await next(context);
                return result;
            });

        Field<StringGraphType>("field2")
            .Resolve(context => "Data")
            // 2. Using a middleware instance
            .ApplyMiddleware(new LoggingMiddleware(logger));

        Field<StringGraphType>("field3")
            .Resolve(context => "Data")
            // 3. Using a type resolved from DI (recommended)
            .ApplyMiddleware<LoggingMiddleware>();
    }
}
```

## Execution Order

When both global and field-specific middleware are present:

1. Global middleware (in registration order)
2. Field-specific middleware (in application order)
3. Field resolver

Example:

```csharp
// Global middleware
services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .UseMiddleware<GlobalMiddleware1>()
    .UseMiddleware<GlobalMiddleware2>());

// Field-specific middleware
public class MyGraphType : ObjectGraphType
{
    public MyGraphType()
    {
        Field<StringGraphType>("myField")
            .Resolve(context => "Result")
            .ApplyMiddleware<FieldMiddleware1>()
            .ApplyMiddleware<FieldMiddleware2>();
    }
}

// Execution order:
// 1. GlobalMiddleware1 (before)
// 2. GlobalMiddleware2 (before)
// 3. FieldMiddleware1 (before)
// 4. FieldMiddleware2 (before)
// 5. Field Resolver executes
// 6. FieldMiddleware2 (after)
// 7. FieldMiddleware1 (after)
// 8. GlobalMiddleware2 (after)
// 9. GlobalMiddleware1 (after)
```

## Dependency Injection

### Using DI with Global Middleware

When using `UseMiddleware<T>()`, the middleware is automatically registered as a singleton. For manual registration:

```csharp
services.AddSingleton<LoggingMiddleware>();

public class MySchema : Schema
{
    public MySchema(IServiceProvider services, MyQuery query, LoggingMiddleware middleware)
        : base(services)
    {
        Query = query;
        FieldMiddleware.Use(middleware);
    }
}
```

### Using DI with Field-Specific Middleware

When using `ApplyMiddleware<T>()`, the middleware must be registered in the DI container:

```csharp
// Register middleware in DI
services.AddSingleton<AuthorizationMiddleware>();

// Apply to field - middleware is resolved from DI during schema initialization
Field<StringGraphType>("protectedField")
    .Resolve(context => "Protected data")
    .ApplyMiddleware<AuthorizationMiddleware>();
```

**Note**: The middleware is resolved from DI during schema initialization, not during each field resolution.

### Scoped Dependencies

For scoped dependencies, use a singleton middleware and resolve dependencies in `ResolveAsync`:

```csharp
public class MyMiddleware : IFieldMiddleware
{
    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
    {
        var scopedService = context.RequestServices!
            .GetRequiredService<IMyScopedService>();
        
        // Use scoped service
        return await next(context);
    }
}
```

## Lifetime Considerations

Recommended lifetimes for optimal performance:

| Schema    | Graph Type | Middleware | Recommendation |
|-----------|------------|------------|----------------|
| singleton | singleton  | singleton  | ✅ Recommended |
| scoped    | scoped     | singleton  | ⚠️ Less performant |
| scoped    | scoped     | scoped     | ⚠️ Least performant |
| scoped    | singleton  | scoped     | ❌ Avoid - causes duplicate middleware application |
| singleton | singleton  | scoped     | ❌ Avoid - throws InvalidOperationException |

**Important**: Middleware is applied during schema initialization. Using incompatible lifetimes can cause middleware to be applied multiple times or fail to resolve.

## Field Middleware vs Directives

**Use Field Middleware when:**

- You need programmatic control over field behavior
- The behavior is implementation-specific (not part of the schema contract)
- You want to apply logic to specific fields without schema changes

**Use Directives when:**

- The behavior should be visible in schema introspection
- You want schema-first configuration
- The behavior applies to multiple schema elements (types, fields, arguments)

For more information, see [Directives](../directives).

## Interface Reference

```csharp
public interface IFieldMiddleware
{
    ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next);
}

public delegate ValueTask<object?> FieldMiddlewareDelegate(IResolveFieldContext context);
