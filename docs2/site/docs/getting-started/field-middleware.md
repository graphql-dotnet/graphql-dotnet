# Field Middleware

Field Middleware is a component connected to the schema, which is embedded into the process of
calculating the field value. You can write middleware for fields to provide additional behaviors
during field resolution. After connecting the middleware to the schema, it is applied to all
fields of all schema types. You can connect several middlewares to the schema. In this case,
they will be called sequentially along the chain where the previous middleware decides to call
the next one. This process is very similar to how middlewares work in the ASP.NET Core HTTP request
pipeline.

The following example is how Metrics are captured. You write a class that implements `IFieldMiddleware`:

```csharp
public class InstrumentFieldsMiddleware : IFieldMiddleware
{
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
    {
        return context.Metrics.Enabled
            ? ResolveWhenMetricsEnabledAsync(context, next)
            : next(context);
    }

    private async ValueTask<object?> ResolveWhenMetricsEnabledAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
    {
        var name = context.FieldAst.Name.StringValue;

        var metadata = new Dictionary<string, object?>
        {
            { "typeName", context.ParentType.Name },
            { "fieldName", name },
            { "returnTypeName", context.FieldDefinition.ResolvedType!.ToString() },
            { "path", context.Path },
        };

        using (context.Metrics.Subject("field", name, metadata))
            return await next(context).ConfigureAwait(false);
    }
}
```

Then register your Field Middleware on the schema.

```csharp
var schema = new Schema();
schema.Query = new MyQuery();
schema.FieldMiddleware.Use(new InstrumentFieldsMiddleware());
```

Or, you can register a middleware delegate directly:

```csharp
schema.FieldMiddleware.Use(next =>
{
  return context =>
  {
    // your code here
    var result = next(context);
    // your code here
    return result;
  };
});
```

The middleware interface is defined as:

```csharp
public interface IFieldMiddleware
{
  ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next);
}
```

The middleware delegate is defined as:

```csharp
public delegate ValueTask<object?> FieldMiddlewareDelegate(IResolveFieldContext context);
```

## Field Middleware and Dependency Injection

First, you are advised to read the article about [Dependency Injection](Dependency-Injection).

Typically you will want to set the middleware within the schema constructor.

```csharp
public MySchema : Schema
{
  public MySchema(
    IServiceProvider services,
    MyQuery query,
    InstrumentFieldsMiddleware middleware)
    : base(services)
  {
    Query = query;
    FieldMiddleware.Use(middleware);
  }
}
```

Then your middleware creation will be delegated to DI-container. Thus, you can pass any dependencies to
the Field Middleware constructor, provided that you have registered them correctly in DI.

Also, the middleware itself should be registered in DI:

```csharp
services.AddSingleton<InstrumentFieldsMiddleware>();
```

Alternatively, you can use an enumerable in your constructor to add all DI-registered middlewares:

```csharp
public MySchema : Schema
{
  public MySchema(
    IServiceProvider services,
    MyQuery query,
    IEnumerable<IFieldMiddleware> middlewares)
    : base(services)
  {
    Query = query;
    foreach (var middleware in middlewares)
      FieldMiddleware.Use(middleware);
  }
}

// within Startup.cs
services.AddSingleton<ISchema, MySchema>();
services.AddSingleton<IFieldMiddleware, InstrumentFieldsMiddleware>();
services.AddSingleton<IFieldMiddleware, MyMiddleware>();
...
```

## Known issues

Perhaps the most important thing with Field Middlewares that you should be aware of is that the
default `DocumentExecuter` applies middlewares to the schema only once while the schema is being
initialized. After this, calling any `IFieldMiddlewareBuilder.Use` methods has no effect.

Field Middleware, when applying to the schema, **modifies** the resolver of each field. Therefore,
you should be careful when using different lifetimes (singleton, scoped, transient) for your
GraphTypes, Schema and Field Middleware. You **can** use any of lifetime, but for example in
case of using singleton lifetime for some GraphType and scoped lifetime for Field Middleware
and Schema this will cause the middleware to be applied to the same fields multiple times.
In the case of ASP.NET Core app the resolvers of these fields will be wrapped again on each
HTTP request to the server.

General recommendations for lifetimes are:

| Schema    | Graph Type | Middleware | Rating | 
|-----------|------------|------------|--------|
| singleton | singleton  | singleton  | the safest and the most performant option recommended by default |
| scoped    | scoped     | singleton  | much less performant option |
| scoped    | scoped     | scoped     | the least performant option |
| scoped    | singleton  | scoped     | DO NOT DO THAT! Explanation above. |
| singleton | singleton  | scoped     | DO NOT DO THAT! InvalidOperationException: Cannot resolve scoped service from root provider |

If your Field Middleware has scoped dependencies but your Schema and Graph Types are singletons
(which is recommended for them) you can make Field Middleware singleton too and obtain the necessary
dependencies right in the `Resolve` method. Here is an example of such an approach:

```csharp
public class MyFieldMiddleware : IFieldMiddleware
{
  private readonly IHttpContextAccessor _accessor;
  private readonly IMySingletonService _service;

  public MyFieldMiddleware(IHttpContextAccessor accessor, IMySingletonService service)
  {
    _accessor = accessor;
    _service = service;
  }

  public ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
  {
    var scopedDependency1 = accessor.HttpContext.RequestServices.GetRequiredService<IMyService1>();
    var scopedDependency2 = accessor.HttpContext.RequestServices.GetRequiredService<IMyService2>();
    ...
    return next(context);
  }
}
```

Options are also possible using transient lifetime, but are not given here (not recommended).

## Field Middleware vs Directive

You can think of a Field Middleware as something global that controls how all fields of all types
in the schema are resolved. A directive, at the same time, would only affect specific schema elements
and only those elements. Moreover, a directive is not limited to field resolvers like middleware is.
For more information about directives see [Directives](https://graphql-dotnet.github.io/docs/getting-started/directives).
