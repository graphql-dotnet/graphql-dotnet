# User Context

You can pass a `UserContext` to provide access to your specific data.  The `UserContext` is accessible in field resolvers and validation rules.

```csharp
public class MyGraphQLUserContext
{
}

schema.Execute(_ =>
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
