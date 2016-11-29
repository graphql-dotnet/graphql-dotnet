# Error Handling

TODO

# User Context

TODO

# Dependency Injection

TODO

# Object/Field Metadata

`GraphType` and `FieldType` implement the `IProvideMetadata` interface.  This allows you to add arbitrary information to a field or graph type.  This can be useful in combination with a validation rule or filed middleware.

```csharp
public interface IProvideMetadata
{
    IDictionary<string, object> Metadata { get; }
    TType GetMetadata<TType>(string key, TType defaultValue = default(TType));
    bool HasMetadata(string key);
}
```

# Field Middleware

You can write middleware for fields to provide additional behaviors during field resolution.  The following example is how Metrics are captured.  You register Field Middleware in the `ExecutionOptions`.

```csharp
var result = await _executer.ExecuteAsync(_ =>
{
    _.Schema = _schema;
    _.Query = queryToExecute;

    _.FieldMiddleware.Use<InstrumentFieldsMiddleware>();

}).ConfigureAwait(false);
```

You can write a class that has a `Resolve` method or you can register a middleware delegate directly.

```csharp
public class InstrumentFieldsMiddleware
{
    public Task<object> Resolve(
        ResolveFieldContext context,
        FieldMiddlewareDelegate next)
    {
        var metadata = new Dictionary<string, object>
        {
            {"typeName", context.ParentType.Name},
            {"fieldName", context.FieldName}
        };

        using (context.Metrics.Subject("field", context.FieldName, metadata))
        {
            return next(context);
        }
    }
}
```

The middleware delegate is defined as:

``` csharp
public delegate Task<object> FieldMiddlewareDelegate(ResolveFieldContext context);
```

```csharp
_.FieldMiddleware.Use(next =>
{
    return context =>
    {
        return next(context);
    };
});
```

# Authentication / Authorization

You can write validation rules that will run before the query is executed.  You can use this pattern to check that the user is authenticated or has permissions for a specific field.  This example uses the `Metadata` dictionary available on Fields to set permissons per field.

```csharp
public class RequiresAuthValidationRule : IValidationRule
{
    public INodeVisitor Validate(ValidationContext context)
    {
        var userContext = context.UserContext.As<GraphQLUserContext>();
        var authenticated = userContext.User?.IsAuthenticated() ?? false;

        return new EnterLeaveListener(_ =>
        {
            _.Match<Operation>(op =>
            {
                if (op.OperationType == OperationType.Mutation && !authenticated)
                {
                    context.ReportError(new ValidationError(
                        context.OriginalQuery,
                        "auth-required",
                        $"Authorization is required to access {op.Name}.",
                        op));
                }
            });

            // this could leak info about hidden fields in error messages
            // it would be better to implement a filter on the schema so it
            // acts as if they just don't exist vs. an auth denied error
            // - filtering the schema is not currently supported
            _.Match<Field>(fieldAst =>
            {
                var fieldDef = context.TypeInfo.GetFieldDef();
                if (fieldDef.RequiresPermissions() &&
                    (!authenticated || !fieldDef.CanAccess(userContext.User.Claims)))
                {
                    context.ReportError(new ValidationError(
                        context.OriginalQuery,
                        "auth-required",
                        $"You are not authorized to run this query.",
                        fieldAst));
                }
            });
        });
    }
}
```

## Permission Extension Methods

```csharp
Field(x => x.Name).AddPermission("Some permission");
```

```csharp
public static class GraphQLExtensions
{
    public static readonly string PermissionsKey = "Permissions";

    public static bool RequiresPermissions(this IProvideMetadata type)
    {
        var permissions = type.GetMetadata<IEnumerable<string>>(PermissionsKey, new List<string>());
        return permissions.Any();
    }

    public static bool CanAccess(this IProvideMetadata type, IEnumerable<string> claims)
    {
        var permissions = type.GetMetadata<IEnumerable<string>>(PermissionsKey, new List<string>());
        return permissions.All(x => claims?.Contains(x) ?? false);
    }

    public static bool HasPermission(this IProvideMetadata type, string permission)
    {
        var permissions = type.GetMetadata<IEnumerable<string>>(PermissionsKey, new List<string>());
        return permissions.Any(x => string.Equals(x, permission));
    }

    public static void AddPermission(this IProvideMetadata type, string permission)
    {
        var permissions = type.GetMetadata<List<string>>(PermissionsKey);

        if (permissions == null)
        {
            permissions = new List<string>();
            type.Metadata[PermissionsKey] = permissions;
        }

        permissions.Fill(permission);
    }

    public static FieldBuilder<TSourceType, TReturnType> AddPermission<TSourceType, TReturnType>(
        this FieldBuilder<TSourceType, TReturnType> builder, string permission)
    {
        builder.FieldType.AddPermission(permission);
        return builder;
    }
}
```

# Protection Against Malicious Queries

TODO

# Query Batching

Query batching allows you to make a single request to your data store instead of multiple requests.  This can also often be referred to as the ["N+1"](http://stackoverflow.com/questions/97197/what-is-the-n1-selects-issue) problem.  One technique of accomplishing this is to have all of your resolvers return a `Task`, then resolve those tasks when the batch is complete.  Some projects provide features like [Marten Batched Queries](http://jasperfx.github.io/marten/documentation/documents/querying/batched_queries/) that support this pattern.

The trick is knowing when to execute the batched query.  GraphQL .NET provides the ability to add listeners in the execution pipline.  Combined with a custom `UserContext` this makes executing the batch trivial.

```csharp
public class GraphQLUserContext
{
    // a Marten batched query
    public IBatchedQuery Batch { get; set; }
}

var result = await executer.ExecuteAsync(_ =>
{
    ...
    _.UserContext = userContext;
    _.Listeners.Add(new ExecuteBatchListener());
});

public class ExecuteBatchListener : DocumentExecutionListenerBase<GraphQLUserContext>
{
    public override async Task BeforeExecutionAwaitedAsync(
        GraphQLUserContext userContext,
        CancellationToken token)
    {
        await userContext.Batch.Execute(token);
    }
}

// using the Batched Query in the field resolver
Field<ListGraphType<DinnerType>>(
    "popularDinners",
    resolve: context =>
    {
        var userContext = context.UserContext.As<GraphQLUserContext>();
        return userContext.Batch.Query(new FindPopularDinners());
    });
```

## Projects attempting to solve N+1:

* [Marten](http://jasperfx.github.io/marten/documentation/documents/querying/batched_queries/) - by Jeremy Miller, PostgreSQL
* [GraphQL .NET DataLoader](https://github.com/dlukez/graphql-dotnet-dataloader) by [Daniel Zimmermann](https://github.com/dlukez)

# Metrics

Metrics are captured during execution.  This can help you determine performance issues within a resolver or validation.  Field metrics are captured using Field Middleware and the results are returned as a `PerfRecord` array on the `ExecutionResult`.  You can then generate a report from those records using `StatsReport`.

```csharp
var start = DateTime.UtcNow;

var result = await _executer.ExecuteAsync( _ =>
    _.FieldMiddleware.Use<InstrumentFieldsMiddleware>();
);

var report = StatsReport.From(schema, result.Operation, result.Perf, start);
```

# Relay

TODO
