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
    Field<DroidType>("hero")
      .Resolve(context =>
      {
        var userContext = context.UserContext as MyGraphQLUserContext;
        ...
      });
  }
}
```

If you need to access the User from the http request and populate this in validation rules you may need to add a User property to your custom `UserContext` class and modify your Startup configuration as follows:
```csharp

  public class MyGraphQLUserContext : Dictionary<string, object?>
  {
      public ClaimsPrincipal User { get; set; }

      public MyGraphQLUserContext(ClaimsPrincipal user)
      {
          User = user;
      }
  }

  services.AddGraphQL()
          .AddUserContextBuilder(httpContext => new MyGraphQLUserContext(httpContext.User));
```
Please note that the `AddUserContextBuilder` method above comes from the [`GraphQL.Server`](https://github.com/graphql-dotnet/server) project.
