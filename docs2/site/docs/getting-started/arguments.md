# Arguments

You can provide arguments to a field.  You can use `GetArgument` on `ResolveFieldContext` to retrieve argument values.  `GetArgument` will attempt to coerce the argument values to the generic type it is given, including primitive values, objects, and enumerations.  You can gain access to the value directly through the `Arguments` dictionary on `ResolveFieldContext`.

```graphql
query {
  droid(id: "1") {
    id
    name
  }
}
```

```csharp
public class StarWarsQuery : ObjectGraphType
{
  public StarWarsQuery(IStarWarsData data)
  {
    Field<DroidType>(
      "droid",
      arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "id" }),
      resolve: context =>
      {
        var id = context.GetArgument<string>("id");
        return data.GetDroidByIdAsync(id);
      }
    );
  }
}
```
