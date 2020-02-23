# Field Middleware

Field Middleware is a component connected to the schema, which is embedded into the process of
calculating the field value. You can write middleware for fields to provide additional behaviors
during field resolution. After connecting the middleware to the schema, it is applied to all
fields of all schema types. You can connect several middlewares to the schema. In this case,
they will be called sequentially along the chain where the previous middleware decides to call
the next one. This process is very similar to how middlewares work in the ASP.NET Core HTTP request
pipeline.

The following example is how Metrics are captured. You register Field Middleware in `ExecutionOptions`.

```csharp
await schema.ExecuteAsync(_ =>
{
  _.Query = "...";
  _.FieldMiddleware.Use<InstrumentFieldsMiddleware>();
});
```

You can write a class that has a `Resolve` method:

```csharp
public class InstrumentFieldsMiddleware
{
  public async Task<object> Resolve(
    IResolveFieldContext context,
    FieldMiddlewareDelegate next)
  {
    var metadata = new Dictionary<string, object>
    {
      {"typeName", context.ParentType.Name},
      {"fieldName", context.FieldName}
    };

    using (context.Metrics.Subject("field", context.FieldName, metadata))
    {
      return await next(context);
    }
  }
}
```

Or you can register a middleware delegate directly:

```csharp
_.FieldMiddleware.Use(next =>
{
  return context =>
  {
    return next(context);
  };
});
```

Also you can implement `IFieldMiddleware` interface in your Field Middleware:

```csharp
public interface IFieldMiddleware
{
  Task<object> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next);
}
```

It doesn’t have to be implemented on your middleware. Then a search will be made for such a method
with a suitable signature.

Nevertheless, **to improve performance, it is recommended to implement this interface.**

The middleware delegate is defined as:

```csharp
public delegate Task<object> FieldMiddlewareDelegate(IResolveFieldContext context);
```

## Field Middleware and Dependency Injection

First, you are advised to read the article about [Dependency Injection](Dependency-Injection).

If you use `IFieldMiddlewareBuilder.Use` overloads which accept type parameter (that is,
those that do not accept a `IFieldMiddleware` instance or a middleware delegate) then your
middleware creation will be delegated to DI-container. Thus, you can pass any dependencies to
the Field Middleware constructor, provided that you have registered them correctly in DI.

Also, the middleware itself should be registered in DI:

```csharp
services.AddSingleton<InstrumentFieldsMiddleware>();
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

  public Task<object> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next)
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

If we consider Field Middleware as a way to globally affect the method of calculating all fields
of all types in the Schema, then the directive can be considered as a way to locally affect only
specific fields. The mechanism of their work is similar. For more information about directives
see [Directives](Directives).
