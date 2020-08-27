# Migrating from v2.x to v3.x

## New Features

### Support for additional scalar types and conversions built-in

These .Net types now are automatically mapped to corresponding built-in custom scalar types:
* Byte
* SByte
* Short
* UShort
* UInt
* ULong
* Guid (maps to `IdGraphType` by default; also supports `GuidGraphType`)
* BigInt

There is also support for converting byte arrays to base-64 encoded strings and vice versa. See
the `ValueConverter` class for more details.

See [Schema Types](https://graphql-dotnet.github.io/docs/getting-started/schema-types) for more details.

### Other new features

* Based on .Net Standard 2.0, supporting .Net Core applications
* Supports scoped services (see below under [Dependency Injection](#dependency-injection))
* Supports the `System.Text.Json` library for JSON serialization, in addition to the Newtonsoft.Json library
* Data loaders work with serial execution strategies and can be chained together
* Name converters can be configured to use a different function for field names versus argument names
* Field builders can take an optional configuration action parameter
* Support for auto-registering input object graph types via `AutoRegisteringInputObjectGraphType`
* Added codes to `ExecutionError`s
* Document processing exceptions can be logged or modified (see below under [Exception Handling](#exception-handling))
* Enhanced validation of graphs built-in
* Supports filtering of schema introspection requests - see details [here](https://github.com/graphql-dotnet/graphql-dotnet/pull/1179)
* Supports federated schemas - see details [here](https://github.com/graphql-dotnet/graphql-dotnet/pull/1233)
* Supports schema 'description' property - see details [here](https://github.com/graphql-dotnet/graphql-dotnet/pull/1613)
* Supports comment nodes - see details [here](https://github.com/graphql-dotnet/graphql-dotnet/pull/1617)
* Supports result 'extensions' - see details [here](https://github.com/graphql-dotnet/graphql-dotnet/pull/1611)
* Ability to limit maximum number of asynchronous field resolvers executing simultaneously - see details [here](https://github.com/graphql-dotnet/graphql-dotnet/issues/1239)

## Breaking Changes

### .Net compatibility

This project now requires the .Net Standard 2.0 framework, and breaks compatibility with applications based on
.Net Framework 4.6 and earlier.  See https://docs.microsoft.com/en-us/dotnet/standard/net-standard for a list of
frameworks that support .Net Standard 2.0.

### Dependency Injection

The previous `IDependencyResolver` interface and `FuncDependencyResolver` class have been replaced by the
.Net Standard `IServiceProvider` interface and the new `FuncServiceProvider` class.

```csharp
//public Schema(IDependencyResolver dependencyResolver)
public Schema(IServiceProvider serviceProvider)
{
  ...
}
```

Also, the `Schema.DependencyResolver` property has been removed and not replaced. If you need to access the service provider
from your graphs, you can include `IServiceProvider` in the constructor of the graph type. Your DI container will pass
a reference to the service provider.  For singleton schemas, this will be the root service provider, from which you can
obtain other singleton or transient services. For scoped schemas with scoped graph types, this will be the service provider
for the current executing scope. Casting `Schema` to `IServiceProvider` is also possible, but not recommended, and will
yield similar results.

If you wish to access a scoped service from within a resolver and want to use a singleton schema (as is recommended), you
can pass a scoped service provider to `ExecutionOptions.RequestServices`, which can then be used to resolve scoped
services. For Asp.Net Core projects, you can set this to equal `HttpContext.RequestServices`. Be aware that if you
are using a parallel execution strategy (default for 'query' requests), using scoped services within field resolvers can
introduce thread safety issues; you may need to use a serial execution strategy or manually create a scope within each
field resolver.

See the [Dependency Injection documentation](https://graphql-dotnet.github.io/docs/getting-started/dependency-injection) for
more details, including service lifetime guidelines and restrictions when registering your schema and graph types.

### Json parsing and serialization

Version 2.x relied on the Newtonsoft.Json library for parsing of input variables and serialization of response data.
As the Newtonsoft.Json dependency has now been removed, a third-party library will be required within your application
to parse and serialize json data. The [GraphQL.SystemTextJson](https://www.nuget.org/packages/GraphQL.SystemTextJson/) and
[GraphQL.NewtonsoftJson](https://www.nuget.org/packages/GraphQL.NewtonsoftJson/) nuget packages include the necessary
components to assist in this regard. Below are examples of the changes required:

Version 2.x sample, using the Newtonsoft.Json converter:
```csharp
using Newtonsoft.Json;

private static async Task ExecuteAsync(HttpContext context, ISchema schema)
{
    GraphQLRequest request;
    using (var reader = new StreamReader(context.Request.Body))
    using (var jsonReader = new JsonTextReader(reader))
    {
        var ser = new JsonSerializer();
        request = ser.Deserialize<GraphQLRequest>(jsonReader);
    }

    var executer = new DocumentExecuter();
    var result = await executer.ExecuteAsync(options =>
    {
        options.Schema = schema;
        options.Query = request.Query;
        options.OperationName = request.OperationName;
        options.Inputs = request.Variables.ToInputs();
    });

    context.Response.ContentType = "application/json";
    context.Response.StatusCode = result.Errors?.Any() == true ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.OK;

    var writer = new GraphQL.Http.DocumentWriter();
    await writer.WriteAsync(context.Response.Body, result);
}

public class GraphQLRequest
{
    public string OperationName { get; set; }
    public string Query { get; set; }
    public Newtonsoft.Json.Linq.JObject Variables { get; set; }
}
```

Version 3.0 sample, using the System.Text.Json converter:
```csharp
using System.Text.Json;

private static async Task ExecuteAsync(HttpContext context, ISchema schema)
{
    var request = await JsonSerializer.DeserializeAsync<GraphQLRequest>
    (
        context.Request.Body,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    var executer = new DocumentExecuter();
    var result = await executer.ExecuteAsync(options =>
    {
        options.Schema = schema;
        options.Query = request.Query;
        options.OperationName = request.OperationName;
        options.Inputs = request.Variables.ToInputs();
    });

    context.Response.ContentType = "application/json";
    context.Response.StatusCode = 200; // OK

    var writer = new GraphQL.SystemTextJson.DocumentWriter();
    await writer.WriteAsync(context.Response.Body, result);
}

public class GraphQLRequest
{
    public string OperationName { get; set; }

    public string Query { get; set; }

    [JsonConverter(typeof(GraphQL.SystemTextJson.ObjectDictionaryConverter))]
    public Dictionary<string, object> Variables { get; set; }
}
```

This is just a simplified example of the changes necessary. Note that typically the `DocumentExecuter` and `DocumentWriter`
are registered as singletons within the dependency injection container, as they can safely be shared between requests.

If you continue to use the `Newtonsoft.Json` converter, please note that Asp.Net Core 3.0 disallows synchronous IO by default,
which is required by the converter.  You will need to make a change in the `ConfigureServices` section of `Startup.cs` as
follows:

```csharp
// kestrel
services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

// IIS
services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});
```

Please note that if you use a `NameConverter` other than the default `CamelCaseNameConverter`, you may need to configure
your json serializer also to not convert object properties to camel case. For example, with the `System.Text.Json` converter,
you need to set `JsonSerializerOptions.PropertyNamingPolicy = null;` as follows:

```csharp
var writer = new GraphQL.SystemTextJson.DocumentWriter(options => {
    options.PropertyNamingPolicy = null;
});
```

### UserContext

The definition of the `UserContext` object throughout the library has changed to a `IDictionary<string, object>`.
Custom user context classes will need to inherit from `Dictionary<string, object>` or otherwise support the interface.

```csharp
//class MyContext
class MyContext : Dictionary<string, object>
{
    public DbContext MyDbContext { get; set; }
}
```

### Document Listeners

The `DocumentExecutionListenerBase<T>` class and `IDocumentExecutionListener<T>` interface have been removed;
please implement the `IDocumentExecutionListener` interface when creating a custom document listener. You
can also inherit from the `DocumentExecutionListenerBase` class to provide default implementations of events.

The methods definitions have also changed from passing `userContext` and `cancellationToken` parameters to a
single parameter `context` of type `IExecutionContext`.  The context has properties for accessing the user context,
cancellation token, metrics, execution errors, and other information about the executing request.

```csharp
//class MyListener : DocumentExecutionListenerBase<MyContext>
class MyListener : DocumentExecutionListenerBase
{
    //public virtual Task AfterValidationAsync(MyContext userContext, IValidationResult validationResult, CancellationToken token)
    public Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult)
    {
        var myContext = (MyContext)context.userContext;

        // log validation error

        return Task.CompletedTask;
    }

    ...
}
```

### IResolveFieldContext and IResolveConnectionContext

Field resolver methods now are passed a reference to an `IResolveFieldContext` interface, rather than a
`ResolveFieldContext` class.  Inline lambda functions are typically unaffected, but if you define your
resolvers separately, you will need to change the function signature.

```csharp
class MyGraphType : ObjectGraphType
{
    public MyGraphType()
    {
        Field("Name", resolve: x => "John Doe");
        Field("Children", resolve: GetChildren);
    }

    //public IEnumerable<string> GetChildren(ResolveFieldContext context)
    public IEnumerable<string> GetChildren(IResolveFieldContext context)
    {
        return new [] { "Jack", "Jill" };
    }
}
```

Also please note that all `IResolveFieldContext` and similar interfaces and classes have moved from
the `GraphQL.Types` namespace to the `GraphQL` namespace. You may need to add a `using GraphQL;`
statement to some of your files.

### Connection Builders

The connection builders have changed slightly. Please see https://graphql-dotnet.github.io/docs/getting-started/relay for current implementation details.

### Field Middleware

Field Middleware must be registered in the dependency injection container in order to be instantiated.
You also need to implement the `IFieldMiddleware` interface on your custom middleware classes, and change
the signature for the `Resolve` method to accept `IResolveFieldContext`.

```csharp
//class MyMiddleware
class MyMiddleware : IFieldMiddleware
{
    //public async Task<object> Resolve(ResolveFieldContext context, FieldMiddlewareDelegate next)
    public async Task<object> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next)
    {
        // your code here
        var ret = await next(context);
        // your code here
        return ret;
    }
}
```

Please note that the delegate definition for `FieldMiddlewareDelegate` has changed as follows:

```csharp
// version 2.4.0
public delegate Task<object> FieldMiddlewareDelegate(ResolveFieldContext context);
// version 3.0
public delegate Task<object> FieldMiddlewareDelegate(IResolveFieldContext context);
```

You also must ensure that your schema implements `IServiceProvider`. This is handled
automatically if you inherit from `Schema`.

See [Field Middleware](https://graphql-dotnet.github.io/docs/getting-started/field-middleware) for more details,
including guidelines and restrictions on service lifetimes of middleware registered through your DI framework.

### Data Loaders

Data loaders now return an `IDataLoaderResult<T>` rather than a `Task<T>`.  Field resolver signatures may need to change
as a result.  Lambda functions passed to field builders' `ResolveAsync` method should not need to change.

```csharp
public class OrderType : ObjectGraphType<Order>
{
    private readonly IDataLoaderContextAccessor _accessor;
    private readonly IUsersStore _users;

    // Inject the IDataLoaderContextAccessor to access the current DataLoaderContext
    public OrderType(IDataLoaderContextAccessor accessor, IUsersStore users)
    {
        _accessor = accessor;
        _users = users;

        ...

        Field<UserType, User>()
            .Name("User")
            .ResolveAsync(ResolveUser);
    }

    //public Task<User> ResolveUser(IResolveFieldContext context)
    public IDataLoaderResult<User> ResolveUser(IResolveFieldContext context)
    {
        // Get or add a batch loader with the key "GetUsersById"
        // The loader will call GetUsersByIdAsync for each batch of keys
        var loader = _accessor.Context.GetOrAddBatchLoader<int, User>("GetUsersById", users.GetUsersByIdAsync);

        // Add this UserId to the pending keys to fetch
        // The task will complete once the GetUsersByIdAsync() returns with the batched results
        return loader.LoadAsync(context.Source.UserId);
    }
}
```

If you need to process the data loader result before it is returned, additional refactoring will need to be done.
The data loader also now supports chained data loaders, and asynchronous code prior to queuing the data loader. See
[Data loader documentation](https://graphql-dotnet.github.io/docs/getting-started/dataloader) for more details.

### ExecutionStrategy changes

If you utilize data loaders along with a custom implementation of `IExecutionStrategy` (typically inheriting
from `ExecutionStrategy`), you must change the implementation to monitor for `IDataLoaderResult` returned
values, and execute `GetResultAsync` at the appropriate time to retrieve the actual value asynchronously.
If the field resolver returns a `Task<IDataLoaderResult>`, the execution strategy should start the task as
usual, only queuing the data loader once the `IDataLoaderResult` has been returned. Note that
`await IDataLoaderResult.GetResultAsync()` may return another `IDataLoaderResult` which must again
be queued to execute at the proper time. Please refer to the reference implementation of `ParallelExecutionStrategy`
for an example.

### Exception Handling

Exceptions have been split into three categories: schema errors, input errors, and processing errors.  For instance, if an invalid
query was passed to the `DocumentExecuter`, it would be considered an input error, and a `SyntaxError` would be
thrown.  Or if an invalid enum string was passed as a variable to a query, an `InvalidValueError` would be thrown.
All validation rules that fail their respective tests are treated as input errors.

All input errors will inherit from `DocumentError`.  Below is a list of error messages and their respective error
classes and codes:

Description | Error class | Code
-|-|-
Empty query document | NoOperationError | NO_OPERATION
Query parsing error | SyntaxError | SYNTAX_ERROR
Attempting a mutation or subscription when none are defined | InvalidOperationError | INVAILD_OPERATION
Invalid variable values | InvalidVariableError | INVALID_VALUE

Schema errors throw an exception during the process of defining or building the schema.  For instance, adding
a two fields of the same name to a GraphQL type would result in an `ArgumentOutOfRangeException` while
attempting to add the second field.  Another example would be if a schema defined an invalid union; an error
would be thrown while the schema was being initialized within `DocumentExecuter` and caught as an unhandled exception.

Processing errors should only occur if an exception is thrown from within a field resolver.  For instance, if
an error in your code causes an NullReferenceException to be thrown.  Or if you execute `.Single()` on an empty
array, causing an `InvalidOperationException` to be thrown.  Processing errors also include calling
`context.GetArgument<>` with a type that does not match the argument type, and the system cannot perform
the conversion.  For instance, calling `context.GetArgument<Guid>("arg")` on an argument of type `IntGraphType`.

Field resolvers can manually trigger an input error by throwing an exception of the `ExecutionError` type, or
a type that inherits from it.  Any other thrown error is treated as a processing error.  Below is an example of
typical validation within a query or mutation that returns an input error:

```csharp
Field<NonNullGraphType<OrderGraph>>("order",
    arguments: new QueryArguments(
        new QueryArgument<NonNullGraphType<IntGraphType>> { Name = "id" }),
    resolve: ctx =>
    {
        var orderService = ctx.RequestServices.GetRequiredService<OrderService>();
        var order = orderService.GetById(ctx.GetArgument<int>("id"));
        if (order == null)
            throw new ExecutionError("Invalid order id");
    });
```

Input errors are returned back from the `DocumentExecuter` pursuant to GraphQL specifications.  You can test for
input errors by checking the `Errors` property of the `ExecutionResult` object returned from `DocumentExecuter.ExecuteAsync`.

Processing errors can be thrown back to the caller of `DocumentExecuter.ExecuteAsync` by setting the
`ExecutionOptions.ThrowOnUnhandledExceptions` property to `true`.  When this property is set to `false`, the
default setting, unhandled exceptions are wrapped in an `UnhandledError` and added with a generic error
message to the `ExecutionResult.Errors` property.  Error codes are dynamically generated from the inner exceptions
of the wrapped exception and also returned.

You can also handle these processing exceptions by setting a delegate within the `ExecutionOptions.UnhandledExceptionDelegate`
property.  Within the delegate you can log the error message and stack trace for debugging needs.  You can also
override the generic error message with a more specific message, wrap the exception in your own ExecutionError
class, and/or set the codes as necessary.

Here is a sample of a typical unhandled exception delegate:

```csharp
var executer = new DocumentExecuter();
var result = executer.ExecuteAsync(options => {

    ...

    options.UnhandledExecutionDelegate = ctx =>
    {
        try
        {
            using var db = new MyDatabase();
            db.ErrorLogs.Add(new ErrorLog {
                DateStamp = DateTime.UtcNow,
                Message = ctx.Exception.Message,
                Details = ctx.Exception.ToString()
            });
            db.SaveChanges();
        }
        catch { }
    };
});
```

### SubscriptionExecuter removal

The `SubscriptionExecuter` class, previously marked as obsolete, has been removed. Use the `DocumentExecuter`
in its place.

### NameConverter

The `ExecutionOptions.FieldNameConverter` property has been replaced by the `NameConverter` property, and the
corresponding class names have changed as well. Static instance members have been added to the included
name converters. Below is a sample of the required change when using `PascalCaseFieldNameConverter`:

```csharp
var executer = new DocumentExecuter();
var result = executer.ExecuteAsync(options => {

	...

  //options.FieldNameConverter = new PascalCaseFieldNameConverter();
  options.NameConverter = PascalCaseNameConverter.Instance;
});
```

If you have written a custom name converter, you must now implement `INameConverter` rather than
`IFieldNameConverter`, which has two methods, `NameForField` and `NameForArgument`, rather than
only `NameFor`. There is also no need to check for introspection types, as the framework will
handle this automatically. Below is a sample of the changes required:

```csharp
// version 2.4.x
public class MyConverter : IFieldNameConverter
{
    private static readonly Type[] IntrospectionTypes = { typeof(SchemaIntrospection) };

    public string NameFor(string field, Type parentType) => isIntrospectionType(parentType) ? field.ToCamelCase() : field.ToPascalCase();

    private bool isIntrospectionType(Type type) => IntrospectionTypes.Contains(type);
}

// version 3.0
public class MyConverter : INameConverter
{
    public string NameForField(string fieldName, IComplexGraphType graphType) => fieldName.ToPascalCase();

    public string NameForArgument(string argumentName, IComplexGraphType graphType, FieldType field) => argumentName.ToPascalCase();
}
```

### Global references to the three introspection fields are now properties on `ISchema`

The three introspection field definitions for `__schema`, `__type`, and `__typename` have moved from static properties on the `SchemaIntrospection` class
to properties of the `ISchema` interface, typically provided by the `Schema` class. Custom implementations of `ISchema` must implement three new properties:
`SchemaMetaFieldType`, `TypeMetaFieldType`, and `TypeNameMetaFieldType`. These can be provided by the `GraphTypesLookup` class.
