# Authorization

> See the [Authorization](https://github.com/graphql-dotnet/authorization) project for a more in depth implementation of the following idea.

You can write validation rules that will run before a query is executed.  You can use this pattern to check that the user is authenticated or has permissions for a specific field.  This example uses the `Metadata` dictionary available on Fields to set permissons per field.

```csharp
public class MyGraphType : ObjectGraphType
{
  public MyGraphType()
  {
    this.RequirePermission("READ_ONLY");
    Field(x => x.Secret).RequirePermission("Admin");
  }
}
```

## Validation Rule

```csharp
public class RequiresAuthValidationRule : IValidationRule
{
  public INodeVisitor Validate(ValidationContext context)
  {
    var userContext = context.UserContext as GraphQLUserContext;
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

  public static void RequirePermission(this IProvideMetadata type, string permission)
  {
    var permissions = type.GetMetadata<List<string>>(PermissionsKey);

    if (permissions == null)
    {
      permissions = new List<string>();
      type.Metadata[PermissionsKey] = permissions;
    }

    permissions.Add(permission);
  }

  public static FieldBuilder<TSourceType, TReturnType> RequirePermission<TSourceType, TReturnType>(
      this FieldBuilder<TSourceType, TReturnType> builder, string permission)
  {
    builder.FieldType.RequirePermission(permission);
    return builder;
  }
}
```
