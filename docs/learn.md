# Error Handling

The `ExecutionResult` provides an `Errors` property which includes any errors encountered during exectution.  Errors are returned [according to the spec](http://facebook.github.io/graphql/#sec-Errors), which means stack traces are excluded.  The `ExecutionResult` is transformed to what the spec requires using JSON.NET.  You can change what information is provided by overriding the JSON Converter.

You can provide additional error handling or logging for fields by adding Field Middleware.

# User Context

You can provide a `UserContext` to provide access to your specific data.  The `UserContext` is accessible in field resolvers and validation rules.

```csharp
public class GraphQLUserContext
{
}

var result = await _executer.ExecuteAsync(_ =>
{
    _.UserContext = new GraphQLUserContext();
}).ConfigureAwait(false);

Field<ListGraphType<DinnerType>>(
    "popularDinners",
    resolve: context =>
    {
        var userContext = context.UserContext.As<GraphQLUserContext>();
    });
```

# Dependency Injection

GraphQL .NET supports dependency injection through a simple resolve function on the Schema class.  Internally when trying to resolve a type the library will call this resolve function.



The default implementation uses `Activator.CreateInstance`.

```csharp
type => (GraphType) Activator.CreateInstance(type)
```

How you integrate this into your system will depend on the dependency injection framework you are using.  Registering your schema with a resolve function that accesses your container may look something like this:

```csharp
// Nancy TinyIoCContainer
container.Register((c, overloads) =>
{
    return new NerdDinnerSchema(type => c.Resolve(type) as IGraphType);
});

// SimpleContainer
var container = new SimpleContainer();
container.Singleton(new StarWarsSchema(type => container.Get(type) as IGraphType));
```

[The GraphiQL sample application uses Dependency Injection.](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL.GraphiQL/Bootstrapper.cs)

```csharp
public class NerdDinnerSchema : GraphQL.Types.Schema
{
    public NerdDinnerSchema(Func<Type, IGraphType> resolve)
        : base(resolve)
    {
        Query = (IObjectGraphType)resolve(typeof(Query));
        Mutation = (IObjectGraphType)resolve(typeof(Mutation));
    }
}
```

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
GraphQL allows the client to bundle and nest many queries into a single request. While this is quite convenient it also makes GraphQL endpoints susceptible to Denial of Service attacks.

To mitigate this graphql-dotnet provides a few options that can be tweaked to set the upper bound of nesting and complexity of incoming queries so that the endpoint would only try to resolve queries that meet the set criteria and discard any overly complex and possibly malicious query that you don't expect your clients to make thus protecting your server resources against depletion by a denial of service attacks.

These options are passed to the ``` DocumentExecutor.ExecuteAsync(...)``` via an instance of ```GraphQL.Validation.Complexity.ComplexityConfiguration``` <sub><sup>[*(click here for an example)*](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL.GraphiQL/Controllers/GraphQLController.cs#L62)</sup></sub>. You can leave any of the options null to go with the default value and disable that specific test. The available options are the following:
```csharp
public int? MaxDepth { get; set; }
public int? MaxComplexity { get; set; }
public double? FieldImpact { get; set; }
```
```MaxDepth``` will enforce the total maximum nesting across all queries in a request. For example the following query will have a query depth of 2. Note that fragments are taken into consideration when making these calculations.
```graphql
{
  Product {  # This query has a depth of 2 which loosely translates to two distinct queries
  			 # to the datasource, first one to return the list of products and second
             # one (which will be executed once for each returned product) to grab
             # the product's first 3 locations.     
    Title
    ...X  # The depth of this fragment is calculated first and added to the total.             
  }
}

fragment X on Product { # This fragment has a depth of only 1.
  Location(first: 3) {
    lat
    long
  }
}
```
The query depth setting is a good estimation of complexity for most use cases and it loosely translates to the number of unique queries sent to the datastore (however it does not look at how many times each query might get executed). Keep in mind that the calculation of complexity needs to be FAST otherwise it can impose a significant overhead.

One step further would be specifying ```MaxComplexity``` and ```FieldImpact``` to look at the estimated number of entities (or cells in a database) that are expected to be returned by each query. Obviously this depends on the size of your database (i.e. number of records per entity) so you will need to find the average number of records per database entity and input that into ```FieldImpact```. For example if I have 3 tables with 100, 120 and 98 rows and I know I will be querying the first table twice as much then a good estimation for ```avgImpact``` would be 105.

Note: I highly recommend setting a higher bound on the number of returned entities by each resolve function in your code. if you use this approach already in your code then you can input that upper bound (which would be the maximum possible items returned per entity) as your avgImpact.
It is also possilbe to use a theorical value for this (for example 2.0) to asses the query's impact on a theorical database hence decoupling this calculation from your actual database.

Imagine if we had a simple test database for the query in the previous example and we assume an average impact of 2.0 (each entity will return ~2 results) then we can calculate the complexity as following:

```math
2 Products returned + 2 * (1 * Title per Product) + 2 * [ (3 * Locations) + (3 * lat entries) + (3 * long entries) ] = **22**
```

Or simply put on average we will have **2x Products** each will have 1 Title for a total of **2x Titles** plus per each Product entry we will have 3 locations overriden by ```first``` argument (we follow relay's spec for ```first```,```last``` and ```id``` arguments) and each of these 3 locations have a lat and a long totalling **6x Locations** having **6x lat**s and **6x longs**.

Now if we set the ```avgImpact``` to 2.0 and set the ```MaxComplexity``` to 23 (or higher) the query will execute correctly. If we change the ```MaxComplexity``` to something like 20 the DocumentExecutor will fail right after parsing the AST tree and will not attempt to resolve any of the fields (or talk to the database).

# Query Batching

Query batching allows you to make a single request to your data store instead of multiple requests.  This can also often be referred to as the ["N+1"](http://stackoverflow.com/questions/97197/what-is-the-n1-selects-issue) problem.  One technique of accomplishing this is to have all of your resolvers return a `Task`, then resolve those tasks when the batch is complete.  Some projects provide features like [Marten Batched Queries](http://jasperfx.github.io/marten/documentation/documents/querying/batched_queries/) that support this pattern.

The trick is knowing when to execute the batched query.  GraphQL .NET provides the ability to add listeners in the execution pipeline.  Combined with a custom `UserContext` this makes executing the batch trivial.

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

The core project provides a few classes to help with Relay.  You can find more types and helpers [here](https://github.com/graphql-dotnet/relay).

(Example needed)
