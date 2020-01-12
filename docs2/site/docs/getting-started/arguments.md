# Arguments

You can provide arguments to a field.  You can use `GetArgument` on `IResolveFieldContext` to retrieve argument values.  `GetArgument` will attempt to coerce the argument values to the generic type it is given, including primitive values, objects, and enumerations.  You can gain access to the value directly through the `Arguments` dictionary on `IResolveFieldContext`.

```graphql
query {
  droid(id: "123") {
    id
    name
  }
}
```

## Schema First

```csharp
public class Droid
{
  public string Id { get; set; }
  public string Name { get; set; }
}

public class Query
{
  private List<Droid> _droids = new List<Droid>
  {
    new Droid { Id = "123", Name = "R2-D2" }
  };

  [GraphQLMetadata("droid")]
  public Droid GetDroid(string id)
  {
    return _droids.FirstOrDefault(x => x.Id == id);
  }
}

var schema = Schema.For(@"
  type Droid {
    id: ID!
    name: String
  }

  type Query {
    droid(id: ID!): Droid
  }
", _ => {
    _.Types.Include<Query>();
});

var json = await schema.ExecuteAsync(_ =>
{
  _.Query = $"{{ droid(id: \"123\") {{ id name }} }}";
});
```

## GraphType First

```csharp
public class Droid
{
  public string Id { get; set; }
  public string Name { get; set; }
}

public class DroidType : ObjectGraphType
{
  public DroidType()
  {
    Field<NonNullGraphType<IdGraphType>>("id");
    Field<StringGraphType>("name");
  }
}

public class StarWarsQuery : ObjectGraphType
{
  private List<Droid> _droids = new List<Droid>
  {
    new Droid { Id = "123", Name = "R2-D2" }
  };

  public StarWarsQuery()
  {
    Field<DroidType>(
      "droid",
      arguments: new QueryArguments(
        new QueryArgument<IdGraphType> { Name = "id" }
      ),
      resolve: context =>
      {
        var id = context.GetArgument<string>("id");
        return _droids.FirstOrDefault(x => x.Id == id);
      }
    );
  }
}

var schema = new Schema { Query = new StarWarsQuery() };
var json = await schema.ExecuteAsync(_ =>
{
  _.Query = $"{{ droid(id: \"123\") {{ id name }} }}";
})
```
