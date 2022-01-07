# User Context

You can pass a `UserContext` (any `IDictionary<string, object?>`) to provide access to
your specific data. The `UserContext` is accessible in field resolvers and validation rules.

```csharp
public class MyGraphQLUserContext : Dictionary<string, object?>
{
}

await schema.ExecuteAsync(_ =>
{
  _.Query = "...";
  _.UserContext = new MyGraphQLUserContext();
});

public class Query : ObjectGraphType
{
  public Query()
  {
    Field<DroidType>(
      "hero",
      resolve: context =>
      {
        var userContext = context.UserContext as MyGraphQLUserContext;
        ...
      });
  }
}
```
