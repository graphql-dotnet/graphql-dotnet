# Field Middleware

You can write middleware for fields to provide additional behaviors during field resolution.  The following example is how Metrics are captured.  You register Field Middleware in the `ExecutionOptions`.

```csharp
schema.Execute(_ =>
{
  _.Query = "...";
  _.FieldMiddleware.Use<InstrumentFieldsMiddleware>();
});
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
