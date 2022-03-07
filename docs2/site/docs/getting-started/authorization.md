# Authorization

The GraphQL operation authorization problem is out of scope of GraphQL.NET project but we
provide some [extension methods](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/AuthorizationExtensions.cs)
to configure authorization requirements to fields, types and other shema elements.

You can write validation rules that will run before a GraphQL document is executed. You
can use this pattern to check that the user is authenticated or has permissions for a
specific field or type.

See the [Authorization](https://github.com/graphql-dotnet/authorization) project for an
implementation of authorization framework on top of GraphQL.NET. Keep in mind that alongside
this project there is a similar [Authorization.AspNetCore](https://github.com/graphql-dotnet/server/tree/develop/src/Authorization.AspNetCore)
project designed specifically for ASP.NET Core apps.

This example shows how to mark type and field with the specified authorization policy.

```csharp
public class MyGraphType : ObjectGraphType
{
  public MyGraphType()
  {
    this.AuthorizeWith("ReadOnlyPolicy");
    Field(x => x.Secret).AuthorizeWith("Admin");
  }
}
```

Authorization policy may consist from various authorization requirements and is configured
outside your GraphType and schema. `AuthorizeWith` method just marks the type or field that
this schema element should be checked against the specified policy before executing GraphQL
document. Both frameworks mentioned above support the concept of policies and authorization
requirements. See the examples in the corresponding repositories.

## Validation Rule

Authorization is controlled by a special validation rule, which should be added to other
(standard) validation rules provided by this project.

1. Validation rule from [Authorization](https://github.com/graphql-dotnet/authorization/blob/master/src/GraphQL.Authorization/AuthorizationValidationRule.cs) project. 
2. Validation rule from [Authorization.AspNetCore](https://github.com/graphql-dotnet/server/blob/master/src/Authorization.AspNetCore/AuthorizationValidationRule.cs) project.
